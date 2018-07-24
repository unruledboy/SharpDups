using System;
using System.Collections.Generic;
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
            Console.WriteLine("Please specify the folder to find dup files:");
            var folder = Console.ReadLine();
            if (Directory.Exists(folder))
            {
                var workers = 5;

                Console.WriteLine("Please choose from the following options(press the number):");
                Console.WriteLine("1. Find");
                Console.WriteLine("2. Compare");
                Console.WriteLine("3. Performance Testing");

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
                }

				Console.WriteLine($"Took: {AppDomain.CurrentDomain.MonitoringTotalProcessorTime.TotalMilliseconds:#,###} ms");
				Console.WriteLine($"Allocated: {AppDomain.CurrentDomain.MonitoringTotalAllocatedMemorySize / 1024:#,#} kb");
				Console.WriteLine($"Peak Working Set: {Process.GetCurrentProcess().PeakWorkingSet64 / 1024:#,#} kb");

				for (int index = 0; index <= GC.MaxGeneration; index++)
				{
					Console.WriteLine($"Gen {index} collections: {GC.CollectionCount(index)}");
				}
			}
			else
                Console.WriteLine("Please make sure folder {0} exist", folder);
            Console.ReadLine();
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
			(List<Duplicate> duplicates, IList<string> failedToProcessFiles) result = default;

            if (times <= 0)
                times = 10;

            timer.Start();

            for (var i = 0; i < times; i++)
            {
                result = dupDetector.Find(files, workers);
            }

            timer.Stop();

            Log(string.Format("dup method: {0}, workers: {1}, groups: {2}, times: {3}, avg elapse: {4}", dupDetector, workers, result.duplicates.Count, times, TimeSpan.FromMilliseconds(timer.ElapsedMilliseconds / times)));
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
            Log("dup groups:" + result.duplicates.Count);
            foreach (var dup in result.duplicates)
            {
                var dupItems = dup.Items.OrderByDescending(f => f.ModifiedTime);
                var latestItem = dupItems.First();
                Log("\tlatest one:");
                Log(string.Format("\t\t{0}", latestItem.FileName));
                var remainingItems = dupItems.Skip(1).ToArray();
                Log(string.Format("\tdup items:{0}", remainingItems.Count()));
                foreach (var item in remainingItems)
                {
                    Log(string.Format("\t\t{0}", item.FileName));
                }
                Log(string.Empty);
            }
        }

        private static void Log(string text)
        {
            Console.WriteLine(text);
            File.AppendAllText("log.txt", text + "\r\n");
        }
    }
}
