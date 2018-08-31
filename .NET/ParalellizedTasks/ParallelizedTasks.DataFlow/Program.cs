using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace ParallelizedTasks.DataFlow
{
    public class Program
    {
        static async Task<(IReadOnlyCollection<int>, TimeSpan)> RunComputationsAsync(int maxDegreeOfParallelism, int messageCount)
        {
            ConcurrentBag<int> results = new ConcurrentBag<int>();
            var workerBlock = new ActionBlock<int>(
               async item =>
               {
                   await Task.Delay(item == 3 ? 10000 : 1000);
                   results.Add(item * 2);
               },
               // Specify a maximum degree of parallelism.
               new ExecutionDataflowBlockOptions
               {
                   MaxDegreeOfParallelism = maxDegreeOfParallelism
               });

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            for (int i = 0; i < messageCount; i++)
            {
                workerBlock.Post(i);
            }
            workerBlock.Complete();

            workerBlock.Completion.Wait();

            stopwatch.Stop();
            return (results, stopwatch.Elapsed);
        }
        public static async Task Main(string[] args)
        {
            (IReadOnlyCollection<int> data, TimeSpan elapsed) result = await RunComputationsAsync(1, 10);
            Console.WriteLine($"Degree of parallelism = 1; message count = 10; elapsed time = {result.elapsed.TotalMilliseconds}ms. Results = {string.Join(",", result.data)}");

            result = await RunComputationsAsync(4, 10);
            Console.WriteLine($"Degree of parallelism = 4; message count = 10; elapsed time = {result.elapsed.TotalMilliseconds}ms. Results = {string.Join(",", result.data)}");

            Console.ReadKey();
        }
    }
}
