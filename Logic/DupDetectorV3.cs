using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xnlab.SharpDups.Infrastructure;
using Xnlab.SharpDups.Model;

namespace Xnlab.SharpDups.Logic
{
	public class ProgressiveDupDetector : IDupDetector
	{
		private const int BufferSize = 4096;
		private int _workers;

		public (List<Duplicate> duplicates, IList<string> failedToProcessFiles) Find(IEnumerable<string> files, int workers)
		{
			_workers = workers;

			if (_workers <= 0)
				_workers = 5;

			var result = new List<Duplicate>();
			var failedToProcessFiles = new List<string>();

			//groups with same file size
			var sameSizeGroups = files.Select(f =>
			{
				return GetDupFileItem(f);
			}).GroupBy(f => f.Size).Where(g => g.Count() > 1);

			var mappedSameSizeGroupList = new ConcurrentBag<IGrouping<string, DupItem>>();

			Parallel.ForEach(MapFileSizeGroups(sameSizeGroups), mappedSameSizeGroups =>
			{
				foreach (var group in mappedSameSizeGroups)
				{
					foreach (var file in group)
					{
						try
						{
							//fast random byte checking
							QuickHashFile(file);
						}
						catch (Exception)
						{
							failedToProcessFiles.Add(file.FileName);
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
					ProgressiveHash(quickHashGroup);

					//phew, finally.....
					//group by same file hash
					var sameFullHashGroups = quickHashGroup.Where(g => !g.IsDifferent).GroupBy(g => g.FullHash).Where(g => g.Count() > 1);
					result.AddRange(sameFullHashGroups.Select(fullHashGroup => new Duplicate { Items = fullHashGroup.Select(f => new FileItem { FileName = f.FileName, ModifiedTime = f.ModifiedTime, Size = f.Size }) }));
				}
			});

			return (result, failedToProcessFiles);
		}

		private static DupItem GetDupFileItem(string f)
		{
			var info = new FileInfo(f);
			return new DupItem { FileName = f, ModifiedTime = info.LastWriteTime, Size = info.Length };
		}

		public static DupItem ProgressiveHashFile(string file)
		{
			var dupFileItem = GetDupFileItem(file);
			QuickHashFile(dupFileItem);
			var length = dupFileItem.Size / BufferSize;
			if (length == 0)
				length = 1;
			var position = 0;
			for (var i = 0; i < length; i++)
			{
				ProgressiveHashSection(position, dupFileItem);
				position += BufferSize;
			}
			return dupFileItem;
		}

		public static bool ProgressiveCompareFile(DupItem sourceDupItem, string targetFile)
		{
			var targetDupFileItem = GetDupFileItem(targetFile);
			if (targetDupFileItem.Size != sourceDupItem.Size)
				return false;
			QuickHashFile(targetDupFileItem);
			var length = targetDupFileItem.Size / BufferSize;
			if (length == 0)
				length = 1;
			var position = 0;
			for (var i = 0; i < length; i++)
			{
				ProgressiveHashSection(position, targetDupFileItem);
				if (sourceDupItem.HashSections.Count < i + 1 || targetDupFileItem.HashSections[i] != sourceDupItem.HashSections[i])
					return false;
				position += BufferSize;
			}
			return true;
		}

		private static void QuickHashFile(DupItem file)
		{
			if (file.Size > 0)
			{
				using (var stream = File.Open(file.FileName, FileMode.Open, FileAccess.Read, FileShare.Read))
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

		private void ProgressiveHash(IGrouping<string, DupItem> quickHashGroup)
		{
			var groups = quickHashGroup.ToArray();
			var first = groups.First();
			var length = first.Size / BufferSize;
			if (length == 0)
				length = 1;
			var position = 0;
			for (var i = 0; i < length; i++)
			{
				foreach (var group in groups.Where(g => !g.IsDifferent).GroupBy(g => i == 0 ? string.Empty : g.HashSections[i - 1]))
				{
					var hashCount = 0;
					foreach (var groupFile in group)
					{
						ProgressiveHashSection(position, groupFile);
						hashCount = groupFile.HashSections.Count;
					}

					foreach (var incrementalGroupWithSameHashSection in group.GroupBy(g => g.HashSections[hashCount - 1]))
					{
						if (incrementalGroupWithSameHashSection.Count() == 1)
						{
							foreach (var item in incrementalGroupWithSameHashSection)
							{
								item.IsDifferent = true;
							}
						}
					}
				}
				position += BufferSize;
			}

			foreach (var groupFile in groups.Where(g => !g.IsDifferent))
			{
				groupFile.FullHash = string.Join(string.Empty, groupFile.HashSections);
			}
		}

		private static void ProgressiveHashSection(int position, DupItem dupItem)
		{
			if (dupItem.HashSections == null)
				dupItem.HashSections = new List<string>();
			dupItem.HashSections.Add(HashTool.HashFile(dupItem.FileName, position, BufferSize));
		}

		private IEnumerable<IEnumerable<IGrouping<long, DupItem>>> MapFileSizeGroups(IEnumerable<IGrouping<long, DupItem>> source) => Slice(source);

		private IEnumerable<IEnumerable<IGrouping<T, DupItem>>> Slice<T>(IEnumerable<IGrouping<T, DupItem>> source)
		{
			var it = source.ToArray();
			var size = it.Length / _workers;
			if (size == 0)
				size = 1;
			return it.Section(size);
		}

		private IEnumerable<IEnumerable<IGrouping<string, DupItem>>> MapFileHashGroups(IEnumerable<IGrouping<string, DupItem>> source) => Slice(source);
	}
}
