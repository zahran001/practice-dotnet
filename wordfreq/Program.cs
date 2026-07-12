using wordfreq;

// Parse user's command line arguments
Options? opts = Options.Parse(args);

if (opts is null) return;

try
{
	// open the file
	// File.ReadLines is a generator
	// It reads one line at a time
	var lines = File.ReadLines(opts.Path);

	// create the word counter engine
	var counter = new WordCounter(opts.IgnoreCase);

	// run the counting logic and get the results
	var result = counter.Count(lines, opts.MinLength);

	// print the results to the console
	Console.WriteLine($"Total words: {result.TotalWords}");
	Console.WriteLine($"Unique words: {result.UniqueWords}");
	Console.WriteLine($"Top {opts.Top} words:");
	Console.WriteLine("-------------------");


	// Loop through the formatted top words
	foreach(var kvp in result.Top(opts.Top))
	{
		Console.WriteLine($"{kvp.Key}: {kvp.Value}");
	}
} catch (Exception ex)
{
	Console.Error.WriteLine($"An unexpected error occurred: {ex.Message}");
}