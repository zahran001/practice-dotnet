using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace WordFreq.Core
{
	public record CountResult(int TotalWords, Dictionary<string, int> Counts)
	{
		public int UniqueWords => Counts.Count;

		public IEnumerable<KeyValuePair<string, int>> Top(int n) =>
			Counts.OrderByDescending(kv => kv.Value)
			.ThenBy(kv => kv.Key, StringComparer.Ordinal)
			.Take(n);
	}
}
