using System.Collections.Generic;
using Xnlab.SharpDups.Model;

namespace Xnlab.SharpDups.Logic
{
    public interface IDupDetector
    {
		(List<Duplicate> duplicates, IList<string> failedToProcessFiles) Find(IEnumerable<string> files, int workers, int bufferSize = 0);
    }
}
