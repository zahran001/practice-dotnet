using WordFreq.Core;

namespace WordFreq.Cli;

internal static class Program
{
	private static int Main(string[] args)
	{
		var cli = CliArgs.Parse(args);
		if (cli is null) return 1;

		var counter = new WordCounter(cli.Options);
		var result = counter.Count(File.ReadLines(cli.Path));

		foreach (var (word, count) in result.Counts
					 .OrderByDescending(kv => kv.Value)
					 .Take(cli.Top))                    // <- Top lives here, not in Core
		{
			Console.WriteLine($"{count,6}  {word}");
		}

		return 0;
	}
}