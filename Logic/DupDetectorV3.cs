using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xnlab.SharpDups.Infrastructure;
using Xnlab.SharpDups.Model;

namespace Xnlab.SharpDups.Logic
{
    public class DupDetectorV3 : IDupDetector
    {
        private const int BufferSize = 4096;
        private int _workers;

        public List<Duplicate> Find(IEnumerable<string> files, int workers)
        {
            _workers = workers;

            if (_workers <= 0)
                _workers = 5;

            var result = new List<Duplicate>();

            //groups with same file size
            var sameSizeGroups = files.Select(f =>
            {
                var info = new FileInfo(f);
                return new DupItem { FileName = f, ModifiedTime = info.LastWriteTime, Size = info.Length };
            }).GroupBy(f => f.Size).Where(g => g.Count() > 1);

            var mappedSameSizeGroupList = new ConcurrentBag<IGrouping<string, DupItem>>();

            Parallel.ForEach(MapFileSizeGroups(sameSizeGroups), mappedSameSizeGroups =>
            {
                foreach (var group in mappedSameSizeGroups)
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
                    foreach (var sameQuickHashGroup in sameQuickHashGroups)
                    {
                        mappedSameSizeGroupList.Add(sameQuickHashGroup);
                    }
                }
            });

            Parallel.ForEach(MapFileHashGroups(mappedSameSizeGroupList), mappedSameHashGroups =>
            {
                foreach (var quickHashGroup in mappedSameHashGroups)
                {
                    IncrementalHash(quickHashGroup);

                    //phew, finally.....
                    //group by same file hash
                    var sameFullHashGroups = quickHashGroup.GroupBy(g => g.FullHash).Where(g => g.Count() > 1);
                    result.AddRange(sameFullHashGroups.Select(fullHashGroup => new Duplicate { Items = fullHashGroup.Select(f => new FileItem { FileName = f.FileName, ModifiedTime = f.ModifiedTime, Size = f.Size }) }));
                }
            });

            return result;
        }

        private void IncrementalHash(IGrouping<string, DupItem> quickHashGroup)
        {
            var groups = quickHashGroup.ToArray();
            var first = groups.First();
            var length = first.Size / BufferSize;
            if (length == 0)
                length = 1;
            var position = 0;
            for (var i = 0; i < length; i++)
            {
                position += BufferSize;
                foreach (var group in groups.GroupBy(g => i == 0 ? string.Empty : g.HashSections[i - 1]))
                {
                    foreach (var groupFile in group)
                    {
                        if (groupFile.HashSections == null)
                            groupFile.HashSections = new List<string>();
                        groupFile.HashSections.Add(HashTool.HashFile(groupFile.FileName, position, BufferSize));
                    }
                }
            }

            foreach (var groupFile in groups)
            {
                groupFile.FullHash = string.Join(string.Empty, groupFile.HashSections);
            }
        }

        private IEnumerable<IEnumerable<IGrouping<long, DupItem>>> MapFileSizeGroups(IEnumerable<IGrouping<long, DupItem>> source)
        {
            var it = source.ToArray();
            var size = it.Length / _workers;
            return it.Section(size);
        }

        private IEnumerable<IEnumerable<IGrouping<string, DupItem>>> MapFileHashGroups(IEnumerable<IGrouping<string, DupItem>> source)
        {
            var it = source.ToArray();
            var size = it.Length / _workers;
            return it.Section(size);
        }

    }
}
