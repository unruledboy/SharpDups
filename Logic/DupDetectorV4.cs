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
	public class ProgressiveDupDetectorWithGpu : IDupDetector
	{
		private const int DefaultBufferSize = 64 * 1024;
		private const int DefaulfQuickHashSize = 3 * 20;
		private int _workers;

		public (List<Duplicate> duplicates, IList<string> failedToProcessFiles) Find(IEnumerable<string> files, int workers, int quickHashSize, int bufferSize)
		{
			_workers = workers;
			if (_workers <= 0)
				_workers = 5;

			if (bufferSize <= 3)
				bufferSize = DefaultBufferSize;

			if (quickHashSize <= 0)
				quickHashSize = DefaulfQuickHashSize;

			var result = new List<Duplicate>();
			var failedToProcessFiles = new List<string>();

			//groups with same file size
			var sameSizeGroups = files.Select(f =>
			{
				return GetDupFileItem(f);
			}).GroupBy(f => f.Size).Where(g => g.Count() > 1);

			var mappedSameSizeGroupList = new ConcurrentBag<IGrouping<string, DupItem>>();

			gpu.ForEach(MapFileSizeGroups(sameSizeGroups), mappedSameSizeGroups =>
			{
				foreach (var group in mappedSameSizeGroups)
				{
					foreach (var file in group)
					{
						try
						{
							//fast random bytes checking
							QuickHashFile(file, quickHashSize);
						}
						catch (Exception)
						{
							file.Status = CompareStatus.Failed;
							failedToProcessFiles.Add(file.FileName);
						}
					}

					//groups with same quick hash value
					var sameQuickHashGroups = group.Where(f => f.Status != CompareStatus.Failed).GroupBy(f => f.QuickHash).Where(g => g.Count() > 1);
					foreach (var sameQuickHashGroup in sameQuickHashGroups)
					{
						mappedSameSizeGroupList.Add(sameQuickHashGroup);
					}
				}
			});

			Parallel.ForEach(MapFileHashGroups(mappedSameSizeGroupList), mappedSameSizehGroups =>
			{
				foreach (var quickHashGroup in mappedSameSizehGroups)
				{
					ProgressiveHash(quickHashGroup, bufferSize);
					failedToProcessFiles.AddRange(quickHashGroup.Where(f => f.Status == CompareStatus.Failed).Select(f => f.FileName));

					//phew, finally.....
					//group by same file hash
					var sameFullHashGroups = quickHashGroup.Where(g => g.Status != CompareStatus.Failed).GroupBy(g => g.FullHash).Where(g => g.Count() > 1);
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

		public static DupItem ProgressiveHashFile(string file, int quickHashSize, int bufferSize)
		{
			var dupFileItem = GetDupFileItem(file);
			QuickHashFile(dupFileItem, quickHashSize);
			var length = dupFileItem.Size / bufferSize;
			if (length == 0)
				length = 1;
			var position = 0L;
			for (var i = 0; i < length; i++)
			{
				ProgressiveHashSection(position, dupFileItem, bufferSize);
				position += bufferSize;
			}
			return dupFileItem;
		}

		public static bool ProgressiveCompareFile(DupItem sourceDupItem, string targetFile, int quickHashSize, int bufferSize)
		{
			var targetDupFileItem = GetDupFileItem(targetFile);
			if (targetDupFileItem.Size != sourceDupItem.Size)
				return false;

			QuickHashFile(targetDupFileItem, quickHashSize);
			if (targetDupFileItem.QuickHash != sourceDupItem.QuickHash)
				return false;

			var length = targetDupFileItem.Size / bufferSize;
			if (length == 0)
				length = 1;
			var position = 0L;
			for (var i = 0; i < length; i++)
			{
				ProgressiveHashSection(position, targetDupFileItem, bufferSize);
				if (sourceDupItem.HashSections.Count < i + 1 || targetDupFileItem.HashSections[i] != sourceDupItem.HashSections[i])
					return false;
				position += bufferSize;
			}
			return true;
		}

		private static void QuickHashFile(DupItem file, int quickHashSize)
		{
			var hashSize = (int)Math.Min(file.Size, quickHashSize);
			using (var stream = File.Open(file.FileName, FileMode.Open, FileAccess.Read, FileShare.Read))
			{
				file.Tags = new byte[hashSize];
				for (var i = 0; i < 3; i++)
				{
					var sectionSize = hashSize / 3;
					long position;
					if (i == 0)
						position = 0;
					else if (i == 1)
						position = file.Size / 2 - sectionSize / 2;
					else
						position = file.Size - sectionSize;
					stream.Seek(position, SeekOrigin.Begin);
					stream.Read(file.Tags, i * sectionSize, sectionSize);
				}
				file.QuickHash = HashTool.HashBytesText(file.Tags);
				if (file.Size <= hashSize)
					file.Status = CompareStatus.Matched;
			}
		}

		private static void ProgressiveHash(IGrouping<string, DupItem> quickHashGroup, int bufferSize)
		{
			var groups = quickHashGroup.ToArray();
			var first = groups.First();
			var length = first.Size / bufferSize;
			if (length == 0)
				length = 1;
			var position = 0L;
			for (var i = 0; i < length; i++)
			{
				foreach (var group in groups.Where(g => g.Status == CompareStatus.None).GroupBy(g => i == 0 ? string.Empty : g.HashSections[i - 1]))
				{
					var hashCount = 0;
					foreach (var groupFile in group)
					{
						try
						{
							ProgressiveHashSection(position, groupFile, bufferSize);
						}
						catch (Exception)
						{
							groupFile.Status = CompareStatus.Failed;
						}
						hashCount = groupFile.HashSections.Count;
					}

					foreach (var incrementalGroupWithSameHashSection in group.Where(g => g.Status != CompareStatus.Failed).GroupBy(g => g.HashSections[hashCount - 1]))
					{
						if (incrementalGroupWithSameHashSection.Count() == 1)
						{
							foreach (var item in incrementalGroupWithSameHashSection)
							{
								item.Status = CompareStatus.Different;
							}
						}
					}
				}
				position += bufferSize;
			}

			foreach (var groupFile in groups.Where(g => g.Status != CompareStatus.Different))
			{
				if (groupFile.Status != CompareStatus.Matched)
					groupFile.FullHash = string.Join(string.Empty, groupFile.HashSections);
			}
		}

		private static void ProgressiveHashSection(long position, DupItem dupItem, int bufferSize)
		{
			if (dupItem.HashSections == null)
				dupItem.HashSections = new List<string>();
			dupItem.HashSections.Add(HashTool.HashFile(dupItem.FileName, position, bufferSize, bufferSize));
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
