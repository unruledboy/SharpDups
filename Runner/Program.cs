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
            Console.WriteLine("Please specify the folder to find dup files:");
            //specify any files here
            var folder = Console.ReadLine();
            if (Directory.Exists(folder))
            {
                var workers = 5;

                Console.WriteLine("Please choose from the following options(press the number):");
                Console.WriteLine("1 Find");
                Console.WriteLine("2 Compare");
                Console.WriteLine("3 Performance Testing");

                var choice = Console.ReadKey();
                Console.WriteLine();
                Console.WriteLine("Started.");
                switch (choice.Key)
                {
                    case ConsoleKey.D1:
                        var v2 = new DupDetectorV2();
                        Run(v2, workers, folder);
                        break;
                    case ConsoleKey.D2:
                        RunAll(workers, folder);
                        break;
                    case ConsoleKey.D3:
                        PerfAll(workers, folder);
                        break;
                }
                Console.WriteLine("Done.");
            }
            else
                Console.WriteLine("Please make sure folder {0} exist", folder);
            Console.ReadLine();
        }

        private static void PerfAll(int workers, string folder)
        {
            var times = 2;

            //var v3 = new DupDetectorV3();
            //Perf(v3, workers, folder, times);

            var v2 = new DupDetectorV2();
            Perf(v2, workers, folder, times);

            var v1 = new DupDetector();
            Perf(v1, workers, folder, times);
        }

        private static void Perf(IDupDetector dupDetector, int workers, string folder, int times)
        {
            var files = Directory.GetFiles(folder, "*.*", SearchOption.AllDirectories);
            var timer = new Stopwatch();
            List<Duplicate> result = null;

            if (times <= 0)
                times = 10;

            timer.Start();

            for (var i = 0; i < times; i++)
            {
                result = dupDetector.Find(files, workers);
            }

            timer.Stop();

            Log(string.Format("dup method: {0}, workers: {1}, groups: {2}, times: {3}, avg elapse: {4}", dupDetector, workers, result.Count, times, TimeSpan.FromMilliseconds(timer.ElapsedMilliseconds / times)));
        }

        private static void RunAll(int workers, string folder)
        {
            //var v3 = new DupDetectorV3();
            //Run(v3, workers, folder);

            var v2 = new DupDetectorV2();
            Run(v2, workers, folder);

            var v1 = new DupDetector();
            Run(v1, workers, folder);
        }

        private static void Run(IDupDetector dupDetector, int workers, string folder)
        {
            var files = Directory.GetFiles(folder, "*.*", SearchOption.AllDirectories);
            var result = dupDetector.Find(files, workers);
            Log("dup groups:" + result.Count);
            foreach (var dup in result)
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
            File.AppendAllText("log.txt", text);
            File.AppendAllText("log.txt", "\r\n");
        }
    }

}
