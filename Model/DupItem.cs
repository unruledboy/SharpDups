namespace Xnlab.SharpDups.Model
{
    public class DupItem : FileItem
    {
        public byte[] Tags { get; set; }
        public string QuickHash { get; set; }
        public string FullHash { get; set; }
    }
}
