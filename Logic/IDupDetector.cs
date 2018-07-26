using System.Collections.Generic;
using Xnlab.SharpDups.Model;

namespace Xnlab.SharpDups.Logic
{
	public interface IDupDetector
	{
		DupResult Find(IEnumerable<string> files, int workers, int quickHashSize = 3, int bufferSize = 0);
	}
}
