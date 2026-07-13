using WordFreq.Core;

namespace WordFreq.Cli;

internal sealed record CliArgs(string Path, int Top, WordFreqOptions Options)
{
	public static CliArgs? Parse(string[] args)
	{
		if (args.Length == 0)
			return Fail("usage: wordfreq <file> [--top N] [--min-length M] [--ignore-case]");

		string path = args[0];
		int top = 10;
		int minLength = 1;
		bool ignoreCase = false;

		for (int i = 1; i < args.Length; i++)
		{
			switch (args[i])
			{
				case "--top":
					if (!TryReadInt(args, ref i, out top) || top <= 0)
						return Fail("--top requires a positive integer");
					break;
				case "--min-length":
					if (!TryReadInt(args, ref i, out minLength) || minLength < 1)
						return Fail("--min-length requires a positive integer");
					break;
				case "--ignore-case":
					ignoreCase = true;
					break;
				default:
					return Fail($"unknown option: {args[i]}");
			}
		}

		return new CliArgs(path, top, new WordFreqOptions(minLength, ignoreCase));
	}

	private static CliArgs? Fail(string message)
	{
		Console.Error.WriteLine(message);
		return null;
	}

	private static bool TryReadInt(string[] args, ref int i, out int value)
	{
		value = 0;
		if (i + 1 >= args.Length) return false;
		return int.TryParse(args[++i], out value);
	}
}