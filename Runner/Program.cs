using System;
using System.IO;
using System.Linq;
using Xnlab.SharpDups.Logic;

namespace Xnlab.SharpDups.Runner
{
    class Program
    {
        static void Main(string[] args)
        {
            var p = new DupDetector();
            //specify any files here
            var folder = @"C:\Temp\";
            if (Directory.Exists(folder))
            {
                var files = Directory.GetFiles(folder, "*.*", SearchOption.AllDirectories);
                var result = p.Find(files);
                Console.WriteLine("dup groups:" + result.Count);
                foreach (var dup in result)
                {
                    var dupItems = dup.Items.OrderByDescending(f => f.ModifiedTime);
                    var latestItem = dupItems.First();
                    Console.WriteLine("\tlatest one:");
                    Console.WriteLine("\t\t{0}", latestItem.FileName);
                    var remainingItems = dupItems.Skip(1).ToArray();
                    Console.WriteLine("\tdup items:{0}", remainingItems.Count());
                    foreach (var item in remainingItems)
                    {
                        Console.WriteLine("\t\t{0}", item.FileName);
                    }
                    Console.WriteLine();
                }
            }
            else
                Console.WriteLine("Please make sure folder {0} exist", folder);
            Console.Read();
        }
    }

}
