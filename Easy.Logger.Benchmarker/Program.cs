﻿namespace Easy.Logger.Benchmarker
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using Legimi.Easy.Logger.Interfaces;
    using Legimi.Easy.Logger;

    internal class Program
    {
        private readonly TimeSpan _duration;
        private readonly IEasyLogger _logger;

        private static void Main(string[] args)
        {
            var duration = TimeSpan.FromSeconds(10);

            if (args.Length > 0)
            {
                var seconds = int.Parse(args[0]);
                duration = TimeSpan.FromSeconds(seconds);
            }

            new Program(duration).Run();

            Console.WriteLine("[{0:HH:mm:ss.fff}] - [Program] - Disposing", DateTimeOffset.UtcNow);
            Log4NetService.Instance.Dispose();
            Console.WriteLine("[{0:HH:mm:ss.fff}] - [Program] - Disposed", DateTimeOffset.UtcNow);

            Console.WriteLine("Gen 0: {0}", GC.CollectionCount(0).ToString());
            Console.WriteLine("Gen 1: {0}", GC.CollectionCount(1).ToString());
            Console.WriteLine("Gen 2: {0}", GC.CollectionCount(2).ToString());
        }

        public Program(TimeSpan duration)
        {
            var configFile = new FileInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "the-log4net.config"));
            Log4NetService.Instance.Configure(configFile);
            
            _duration = duration;
            _logger = Log4NetService.Instance.GetLogger<Program>();
        }

        private void Run()
        {
            _logger.Info("Benchmarking starting");
            TestThroughput();
            //            TestMultiThreading();
            //            TestIdle();
            _logger.Warn("Benchmarking ended");
        }

        private void TestThroughput()
        {
            Console.WriteLine("------------Testing Throughput------------");

            var sw = Stopwatch.StartNew();
            long counter = 0;
            while (sw.Elapsed < _duration)
            {
                counter++;
                _logger.DebugFormat("Counter is: {0}", counter.ToString());
            }

            Console.WriteLine("Counter reached: {0:n0}, Time Taken: {1}", counter, sw.Elapsed.ToString());
        }

        private void TestMultiThreading()
        {
            Console.WriteLine("------------Testing MultiThreading------------");

            const int WorkerCount = 6;

            long totalCounter = 0;
            Func<int> action = () =>
            {
                var localCounter = 0;
                var sw = Stopwatch.StartNew();

                while (sw.Elapsed < _duration)
                {
                    _logger.DebugFormat("Counter is: {0}", (++localCounter).ToString());
                }

                return localCounter;
            };

            var totalSw = Stopwatch.StartNew();

            Parallel.For(
                0,
                WorkerCount,
                new ParallelOptions { MaxDegreeOfParallelism = WorkerCount },
                () => 0,
                (i, state, partial) => action(),
                partialCounter => Interlocked.Add(ref totalCounter, partialCounter));

            Console.WriteLine("Counter reached: {0:n0}, Time Taken: {1}", totalCounter, totalSw.Elapsed.ToString());
        }

        private void TestIdle()
        {
            Console.WriteLine("------------Testing Idle------------");

            var sw = Stopwatch.StartNew();
            long counter = 0;
            while (sw.Elapsed < _duration)
            {
                counter++;
                _logger.DebugFormat("Counter is: {0}", counter.ToString());
                Thread.Sleep(TimeSpan.FromSeconds(1));
            }

            Console.WriteLine("Counter reached: {0:n0}, Time Taken: {1}", counter, sw.Elapsed.ToString());
        }
    }
}
