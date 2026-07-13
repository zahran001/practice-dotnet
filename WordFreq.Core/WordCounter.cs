using System.Runtime.InteropServices;

namespace WordFreq.Core;

public sealed class WordCounter(WordFreqOptions options) : IWordCounter
{
	private readonly StringComparer _comparer = options.IgnoreCase
		? StringComparer.OrdinalIgnoreCase
		: StringComparer.Ordinal;
	private readonly int _minLength = options.MinLength;

	public static IEnumerable<string> Tokenize(string line)
	{
		int start = -1;
		for (int i = 0; i <= line.Length; i++)
		{
			bool isLetter = i < line.Length && char.IsLetter(line[i]);
			if (isLetter)
			{
				if (start < 0) start = i;
			}
			else if (start >= 0)
			{
				yield return line.Substring(start, i - start);
				start = -1;
			}
		}
	}

	public CountResult Count(IEnumerable<string> lines)
	{
		var counts = new Dictionary<string, int>(_comparer);
		int total = 0;

		foreach (string line in lines)
		{
			foreach (string word in Tokenize(line))
			{
				if (word.Length < _minLength) continue;
				total++;
				ref int slot = ref CollectionsMarshal.GetValueRefOrAddDefault(counts, word, out _);
				slot++;
			}
		}

		return new CountResult(total, counts);
	}
}