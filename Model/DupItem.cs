using System.Collections.Generic;

namespace Xnlab.SharpDups.Model
{
	public class DupItem : FileItem
	{
		public byte[] Tags { get; set; }
		public string QuickHash { get; set; }
		public string FullHash { get; set; }
		public List<string> HashSections { get; set; }
		public CompareStatus Status { get; set; }
	}

	public enum CompareStatus
	{
		None = 0,
		Matched = 1,
		Different = 2,
		Failed = 3
	}
}
