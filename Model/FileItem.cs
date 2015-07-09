using System;

namespace Xnlab.SharpDups.Model
{
    public class FileItem
    {
        public string FileName { get; set; }
        public DateTime ModifiedTime { get; set; }
        public long Size { get; set; }
    }

}
