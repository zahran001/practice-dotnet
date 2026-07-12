using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace wordfreq
{
	internal class WordCounter (bool ignoreCase)
	{
		public static IEnumerable<string> Tokenize(string line)
		{
			int start = -1;

			for(int i=0; i<=line.Length; i++)
			{
				bool isLetter = i < line.Length && char.IsLetter(line[i]);

				if (isLetter)
				{
					if (start < 0) start = i;

				}
				else if(start >= 0)
				{
					yield return line.Substring(start, i - start);
					start = -1;
				}

			}

		}

		public CountResult Count(IEnumerable<string> lines, int minLength)
		{
			var counts = new Dictionary<string, int>(
				ignoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);

			int total = 0;

			foreach (string line in lines)
			{
				foreach (string word in Tokenize(line))
				{
					if (word.Length < minLength) continue;

					total++;
					ref int slot = ref CollectionsMarshal.GetValueRefOrAddDefault(counts, word, out _);
					slot++;
				}
			}

			return new CountResult(total, counts);
		}

	}
}
