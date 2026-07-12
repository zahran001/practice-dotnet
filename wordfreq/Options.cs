using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wordfreq
{
	internal record Options(
		string Path,
		int Top,
		int MinLength, 
		bool IgnoreCase)
	{

		// Returns null on failure and writes to stderr
		public static Options? Parse(string[] args)
		{
			if (args.Length == 0)
				return Fail("usage: wordfreq <file> [--top N] [--min-length M] [--ignore-case]");

			string path = args[0];
			int top = 10;
			int minLength = 1;
			bool ignoreCase = false;

			for(int i=1; i<args.Length; i++)
			{
				switch(args[i])
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

			return new Options(path, top, minLength, ignoreCase);
		
		}

		private static Options? Fail(string message)
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
}
