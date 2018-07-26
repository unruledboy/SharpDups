using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Xnlab.SharpDups.Logic;
using Xnlab.SharpDups.Model;

namespace Xnlab.SharpDups.Runner
{
	class Program
	{
		static void Main(string[] args)
		{
			AppDomain.MonitoringIsEnabled = true;
			var input = string.Empty;
			var exitCommand = "q";
			while (!input.Equals(exitCommand, StringComparison.OrdinalIgnoreCase))
			{
				Console.WriteLine("Please specify the folder to find dup files:");
				var folder = Console.ReadLine();
				if (Directory.Exists(folder))
				{
					var workers = 5;

					Console.WriteLine("Please choose from the following options(press the number):");
					Console.WriteLine("1. Find");
					Console.WriteLine("2. Compare");
					Console.WriteLine("3. Performance Testing");
					Console.WriteLine("Q. Quit");

					var choice = Console.ReadKey();
					Console.WriteLine();
					Console.WriteLine("Started.");
					switch (choice.Key)
					{
						case ConsoleKey.D1:
							var detector = new ProgressiveDupDetector();
							Run(detector, workers, folder);
							break;
						case ConsoleKey.D2:
							RunAll(workers, folder);
							break;
						case ConsoleKey.D3:
							PerfAll(workers, folder);
							break;
						case ConsoleKey.Q:
							input = exitCommand;
							break;
					}

					Console.WriteLine($"Took: {AppDomain.CurrentDomain.MonitoringTotalProcessorTime.TotalMilliseconds:#,###} ms");
					Console.WriteLine($"Allocated: {AppDomain.CurrentDomain.MonitoringTotalAllocatedMemorySize / 1024:#,#} kb");
					Console.WriteLine($"Peak Working Set: {Process.GetCurrentProcess().PeakWorkingSet64 / 1024:#,#} kb");

					for (var index = 0; index <= GC.MaxGeneration; index++)
					{
						Console.WriteLine($"Gen {index} collections: {GC.CollectionCount(index)}");
					}
				}
				else
					Console.WriteLine("Please make sure folder {0} exist", folder);
				Console.WriteLine();
			}
		}

		private static void PerfAll(int workers, string folder)
		{
			foreach (var detector in new[] { (IDupDetector)new ProgressiveDupDetector(), new DupDetectorV2(), new DupDetector() })
			{
				Perf(detector, workers, folder, 2);
			}
		}

		private static void Perf(IDupDetector dupDetector, int workers, string folder, int times)
		{
			var files = Directory.GetFiles(folder, "*.*", SearchOption.AllDirectories);
			var timer = new Stopwatch();
			DupResult result = default;

			if (times <= 0)
				times = 10;

			timer.Start();

			for (var i = 0; i < times; i++)
			{
				result = dupDetector.Find(files, workers);
			}

			timer.Stop();

			Log(string.Format("dup method: {0}, workers: {1}, groups: {2}, times: {3}, avg elapse: {4}", dupDetector, workers, result.Duplicates.Count, times, TimeSpan.FromMilliseconds(timer.ElapsedMilliseconds / times)));
		}

		private static void RunAll(int workers, string folder)
		{
			foreach (var detector in new[] { (IDupDetector)new ProgressiveDupDetector(), new DupDetectorV2(), new DupDetector() })
			{
				Run(detector, workers, folder);
			}
		}

		private static void Run(IDupDetector dupDetector, int workers, string folder)
		{
			var files = Directory.GetFiles(folder, "*.*", SearchOption.AllDirectories);
			var result = dupDetector.Find(files, workers);
			Log($"total files: {result.TotalFiles}");
			Log($"total file bytes: {result.TotalFileBytes}");
			Log($"total read bytes: {result.TotalReadBytes}");
			Log($"dup groups: {result.Duplicates.Count}");
			foreach (var dup in result.Duplicates)
			{
				var dupItems = dup.Items.OrderByDescending(f => f.ModifiedTime);
				var latestItem = dupItems.First();
				Log("\tlatest one:");
				Log(string.Format("\t\t{0}", latestItem.FileName));
				var remainingItems = dupItems.Skip(1).ToArray();
				Log(string.Format("\tdup items:{0}", remainingItems.Length));
				foreach (var item in remainingItems)
				{
					Log(string.Format("\t\t{0}", item.FileName));
				}
				Log(string.Empty);
			}

			Log(string.Empty);
			Log($"Failed to process: {result.FailedToProcessFiles.Count}");
			foreach (var item in result.FailedToProcessFiles)
			{
				Log($"\t{item}");
			}
		}

		private static void Log(string text)
		{
			Console.WriteLine(text);
			File.AppendAllText("log.txt", text + "\r\n");
		}
	}
}
