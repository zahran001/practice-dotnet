using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WordFreq.Core
{
	public interface IWordCounter
	{
		CountResult Count(IEnumerable<string> lines);
	}
}
