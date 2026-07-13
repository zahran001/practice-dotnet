using Microsoft.Extensions.Options;   // IOptions<T> — needed only for the unwrap line below
using WordFreq.Api;                   // AnalyzeRequest, AnalyzeResponse, WordCount (wire contracts)
using WordFreq.Core;                  // IWordCounter, WordCounter, WordFreqOptions (domain)

// CreateBuilder does three things at once:
//   1. reads configuration sources, in precedence order:
//        appsettings.json  <  appsettings.{Environment}.json  <  env vars  <  command line
//      (later wins — so an env var overrides the JSON file)
//   2. sets up logging
//   3. creates an empty DI container (builder.Services) for you to fill
// It has NOT started a web server. Nothing runs yet. This is the *configuration* phase.
var builder = WebApplication.CreateBuilder(args);


// ─────────────── OPTIONS ───────────────
// Nothing here executes now. You are *describing* what should happen later.
builder.Services
	// Register WordFreqOptions with the options system. Returns an
	// OptionsBuilder<WordFreqOptions>, which is why the rest of this chains.
	.AddOptions<WordFreqOptions>()

	// GetSection("WordFreq") slices out that node of the config tree.
	// Bind() says: match JSON keys to property names, by reflection.
	//   "MinLength": 3   ->   o.MinLength = 3
	// This is why WordFreqOptions had to stop being a record: the binder needs a
	// parameterless ctor + public setters. It can't call a positional constructor.
	.Bind(builder.Configuration.GetSection(WordFreqOptions.SectionName))

	// Predicates that must hold. Stored, not run — they fire when the options
	// object is first materialized. The string is the error message on failure.
	.Validate(o => o.MinLength >= 1, "MinLength must be >= 1.")
	.Validate(o => o.MaxTextLength > 0, "MaxTextLength must be positive.")
	.Validate(o => o.DefaultTop is > 0 and <= 1000, "DefaultTop must be 1...1000.")

	// THIS is the load-bearing one. Without it, the validators run lazily — on the
	// first request that touches options. Which means a bad config boots green,
	// passes health checks, takes traffic, then dies.
	// With it, the host forces materialization during startup. Bad config = no boot.
	.ValidateOnStart();


// ─────────────── THE UNWRAP ───────────────
// AddOptions produced an IOptions<WordFreqOptions>. But WordCounter's constructor
// wants a bare WordFreqOptions — deliberately, so Core doesn't reference
// Microsoft.Extensions.Options. The domain shouldn't know a config system exists.
//
// The container has no registration for a bare WordFreqOptions. This adds one.
//
// `sp` is the IServiceProvider — the container itself. This is a *factory*
// registration: "when someone asks for WordFreqOptions, run this lambda."
// It resolves the wrapper and hands back what's inside.
//
// AddSingleton means the lambda runs once, on first request for the type, and the
// result is cached for the process lifetime.
builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<WordFreqOptions>>().Value);

// "When anyone asks for IWordCounter, give them a WordCounter, and make exactly one."
// The container inspects WordCounter's constructor, sees it needs WordFreqOptions,
// and resolves that via the line above. That chain is the whole point of DI —
// you never write `new WordCounter(...)` in the Api.
//
// Singleton is correct here *because WordCounter is stateless*: its two fields are
// readonly, set at construction, never mutated. Safe to share across concurrent
// requests. Had it held per-request state, this would be a data race.
builder.Services.AddSingleton<IWordCounter, WordCounter>();


// Configuration phase over. Build() freezes the container — no more registrations —
// and constructs the host. ValidateOnStart's checks fire around here.
var app = builder.Build();


// ─────────────── THE ENDPOINT ───────────────
// Also not executing. You're registering a route: "POST to /analyze runs this lambda."
// The lambda's *parameters* are the interesting part. Minimal APIs infer where each
// argument comes from, purely from its type:
//
//   AnalyzeRequest  -> complex type, not a registered service, POST has a body
//                      => deserialize it from the JSON request body
//   IWordCounter    -> registered in the container  => inject it
//   WordFreqOptions -> registered in the container  => inject it
//
// That inference (body vs. route vs. query vs. DI) is a favorite interview probe.
// Route params match by name; query strings bind simple types; everything else that
// the container knows about gets injected.
app.MapPost("/analyze", (
	AnalyzeRequest request,
	IWordCounter counter,
	WordFreqOptions options) =>
{
	// Hand-rolled validation. Returns a bare 400 with a string body — which is
	// *not* good HTTP. Phase 2 replaces both of these with ProblemDetails (RFC 7807)
	// and a 422 for the semantic failure. Left crude on purpose so you feel the gap.
	if (string.IsNullOrWhiteSpace(request.Text))
		return Results.BadRequest("Text is required.");

	// The guardrail. Without it, a 2GB request body OOMs the process.
	if (request.Text.Length > options.MaxTextLength)
		return Results.BadRequest($"Text exceeds {options.MaxTextLength} characters.");

	// Caller may specify Top; if not (null), fall back to configured default.
	// `??` is null-coalescing. Top is int? precisely so "absent" is distinguishable
	// from "zero" — a real API-design point.
	int top = request.Top ?? options.DefaultTop;

	// ── ACQUISITION. The Api's job. Parallel to File.ReadLines() in the Cli.
	// Core takes IEnumerable<string> — meaning, not mechanism — so both hosts fit.
	var lines = request.Text.Split('\n');

	// ── COMPUTATION. Core's job. Note the Api never says `new WordCounter`,
	// never mentions MinLength or IgnoreCase. It just calls the interface.
	var result = counter.Count(lines);

	// ── PRESENTATION. The Api's job. Core returned the *complete* CountResult;
	// slicing to top-N happens here. Parallel to Take(cli.Top) in the Cli.
	// ThenBy(kv.Key) breaks count ties alphabetically — without it, dictionary
	// iteration order is unspecified and your responses are nondeterministic.
	// That is a real bug and a good thing to have noticed.
	//
	// Note also the mapping from domain type (KeyValuePair) to wire type (WordCount).
	// Deliberate: the HTTP contract and the domain model are allowed to diverge, and
	// welding them together is a classic mistake — a rename in Core shouldn't
	// silently break every client.
	var response = new AnalyzeResponse(
		TotalWords: result.TotalWords,
		DistinctWords: result.Counts.Count,
		Top: result.Counts
			.OrderByDescending(kv => kv.Value)
			.ThenBy(kv => kv.Key)
			.Take(top)
			.Select(kv => new WordCount(kv.Key, kv.Value))
			.ToList());   // ToList forces evaluation now, inside the handler

	// 200 + JSON body. System.Text.Json serializes it, camelCasing the property
	// names by default: TotalWords -> "totalWords".
	return Results.Ok(response);
});


// NOW it runs. Binds the socket, starts listening, blocks until shutdown.
// Everything above was setup; this is the only line that does anything at runtime.
app.Run();