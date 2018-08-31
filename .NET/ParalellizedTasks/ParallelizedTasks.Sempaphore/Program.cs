using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace ParallelizedTasks.Sempaphore
{
    public class Program
    {
        static async Task<(IReadOnlyCollection<int>, TimeSpan)> RunComputationsAsync(int maxDegreeOfParallelism, int messageCount)
        {
            SemaphoreSlim semaphore = new SemaphoreSlim(maxDegreeOfParallelism);
            List<Task<int>> tasks = new List<Task<int>>();

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            for (int i = 0; i < messageCount; i++)
            {
                var item = i;
                var task = Task.Run(async () =>
                {
                    try
                    {
                        await semaphore.WaitAsync();

                        await Task.Delay(item == 3 ? 10000 : 1000);
                        return item * 2;
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                });
                tasks.Add(task);
            }
            await Task.WhenAll(tasks);

            stopwatch.Stop();
            return (tasks.Select(t => t.Result).ToImmutableList(), stopwatch.Elapsed);
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
