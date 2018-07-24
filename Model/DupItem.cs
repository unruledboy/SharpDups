using System.Collections.Generic;

namespace Xnlab.SharpDups.Model
{
	public class DupItem : FileItem
	{
		public byte[] Tags { get; set; }
		public string QuickHash { get; set; }
		public string FullHash { get; set; }
		public List<string> HashSections { get; set; }
		public bool IsDifferent { get; set; }
		public bool IsFailed { get; set; }
	}
}
