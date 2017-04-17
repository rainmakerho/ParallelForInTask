using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ParallelForInTask
{

    class Program
    {
        public static class ThreadSafeRandom
        {
            private static Random global = new Random();

            [ThreadStatic]
            private static Random local;

            public static int Next()
            {
                Random inst = local;
                if (inst == null)
                {
                    int seed;
                    lock (global)
                    {
                        seed = global.Next();
                    }

                    local = inst = new Random(seed);
                }

                var result = inst.Next(1, 100);
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"{Thread.CurrentThread.ManagedThreadId}) next value:{result} ... start to sleep  ");
                Thread.Sleep(result * 200);
                //值超過70故意給它錯
                if (result > 70) throw new Exception($"{Thread.CurrentThread.ManagedThreadId})grater then 70");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"{Thread.CurrentThread.ManagedThreadId}) next value:{result} ... after sleep");
                return result;
            }
        }

        static void Main(string[] args)
        {
            var Tasks = new List<Task>();
            int i = 0;
            var timer = new System.Timers.Timer(5000);
            timer.Elapsed += (sender, e) =>
            {
                timer.Stop();
                i++;
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"timer start... {DateTime.Now} *********");
                var task = Task.Factory.StartNew(() =>
                {
                    var stopwatch = new Stopwatch();
                    stopwatch.Start();
                    
                    Parallel.For(0, 10,
                        index =>
                        {
                            Console.ForegroundColor = ConsoleColor.White;
                            Console.WriteLine($"{i} - {index} Start");
                            try
                            {
                                ThreadSafeRandom.Next();
                            }
                            catch (Exception pex)
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine($"{i} - exception ... {pex.ToString()}");
                            }
                            Console.WriteLine($"{i} - {index} End");
                        });
                    stopwatch.Stop();
                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.WriteLine(
                        $"*{i}* loop time in milliseconds: {stopwatch.ElapsedMilliseconds} ***************** ");
                });
                Tasks.Add(task);
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"timer end... {DateTime.Now} *********");
                if (i < 5)
                {
                    timer.Start();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"timer stop ... *********");
                }
            };
            timer.Start();


            while (!Tasks.Any())
            {
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine("task not run ..press key ....");
                Console.ReadKey();
            }
            while (Tasks.Any())
            {
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                foreach (var t in Tasks)
                    Console.WriteLine($"TaskId:{t.Id}, Status:{t.Status}");
                if (Console.ReadKey().Key == ConsoleKey.Q) break;
            }
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine("... press key to  end ....");
            Console.ReadKey();
        }
    }
}
