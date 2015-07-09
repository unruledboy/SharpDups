using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xnlab.SharpDups.Infrastructure;
using Xnlab.SharpDups.Model;

namespace Xnlab.SharpDups.Logic
{
    public class DupDetector : IDupDetector
    {
        public List<Duplicate> Find(IEnumerable<string> files, int workers)
        {
            var result = new List<Duplicate>();

            //groups with same file size
            var sameSizeGroups = files.Select(f =>
            {
                var info = new FileInfo(f);
                return new DupItem { FileName = f, ModifiedTime = info.LastWriteTime, Size = info.Length };
            }).GroupBy(f => f.Size).Where(g => g.Count() > 1);

            foreach (var group in sameSizeGroups)
            {
                foreach (var file in group)
                {
                    if (file.Size > 0)
                    {
                        //fast random byte checking
                        using (var stream = File.OpenRead(file.FileName))
                        {
                            var length = stream.Length;
                            file.Tags = new byte[3];
                            //first byte
                            stream.Seek(0, SeekOrigin.Begin);
                            file.Tags[0] = (byte)stream.ReadByte();

                            //middle byte, we need it especially for xml like files
                            if (length > 1)
                            {
                                stream.Seek(stream.Length / 2, SeekOrigin.Begin);
                                file.Tags[1] = (byte)stream.ReadByte();
                            }

                            //last byte
                            if (length > 2)
                            {
                                stream.Seek(0, SeekOrigin.End);
                                file.Tags[2] = (byte)stream.ReadByte();
                            }

                            file.QuickHash = HashTool.GetHashText(file.Tags);
                        }
                    }
                }

                //groups with same quick hash value
                var sameQuickHashGroups = group.GroupBy(f => f.QuickHash).Where(g => g.Count() > 1);
                foreach (var quickHashGroup in sameQuickHashGroups)
                {
                    foreach (var groupFile in quickHashGroup)
                    {
                        groupFile.FullHash = HashTool.HashFile(groupFile.FileName);
                    }

                    //phew, finally.....
                    //group by same file hash
                    var sameFullHashGroups = quickHashGroup.GroupBy(g => g.FullHash).Where(g => g.Count() > 1);
                    result.AddRange(sameFullHashGroups.Select(fullHashGroup => new Duplicate {Items = fullHashGroup.Select(f => new FileItem {FileName = f.FileName, ModifiedTime = f.ModifiedTime, Size = f.Size})}));
                }
            }

            return result;
        }
    }
}
