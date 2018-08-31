using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace ParallelizedTasks.Partitioner
{
    class Program
    {
        static async Task<(IReadOnlyCollection<int>, TimeSpan)> RunComputationsAsync(int maxDegreeOfParallelism, int messageCount)
        {
            var input = System.Linq.Enumerable.Range(0, messageCount);

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            var tasks = System.Collections.Concurrent.Partitioner.Create(input)
                .GetPartitions(maxDegreeOfParallelism)
                .Select(partition => Task.Run(async () =>
                {
                    using (partition)
                    {
                        List<int> results = new List<int>();
                        while (partition.MoveNext())
                        {
                            int item = partition.Current;
                            await Task.Delay(item == 3 ? 10000 : 1000);
                            results.Add(item * 2);
                        }

                        return results;
                    }
                })).ToArray();


            await Task.WhenAll(tasks);

            stopwatch.Stop();
            return (tasks.SelectMany(t => t.Result).ToImmutableList(), stopwatch.Elapsed);
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
