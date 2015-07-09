using System.Collections.Generic;
using Xnlab.SharpDups.Model;

namespace Xnlab.SharpDups.Logic
{
    public interface IDupDetector
    {
        List<Duplicate> Find(IEnumerable<string> files, int workers);
    }
}
