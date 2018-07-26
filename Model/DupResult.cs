using System.Collections.Generic;

namespace Xnlab.SharpDups.Model
{
	public class DupResult
	{
		public long TotalComparedFiles { get; set; }
		public long TotalBytesInComparedFiles { get; set; }
		public long TotalReadBytes { get; set; }
		public List<Duplicate> Duplicates { get; set; }
		public List<string> FailedToProcessFiles { get; set; }
		public long TotalFiles { get; set; }
	}
}
