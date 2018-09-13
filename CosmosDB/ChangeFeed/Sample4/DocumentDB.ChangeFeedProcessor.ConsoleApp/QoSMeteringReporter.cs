using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace DocumentDB.ChangeFeedProcessor.ConsoleApp
{
    public class QoSMeteringReporter : IQoSMeteringReporter
    {
        public async Task<T> MeasureAsync<T>(string name, string partitionId, Func<Task<T>> func)
        {
            var startTime = Stopwatch.GetTimestamp();
            T obj = default(T);
            try
            {
                T ret = await func();
                obj = ret;
                int duration = TimeSpan.FromTicks(Stopwatch.GetTimestamp() - startTime).Milliseconds;
                Console.WriteLine($"QoS: {name} on range {partitionId} took {duration}ms");
                return obj;
            }
            catch (Exception ex)
            {
                int duration = TimeSpan.FromTicks(Stopwatch.GetTimestamp() - startTime).Milliseconds;
                Console.WriteLine($"QoS: {name} on range {partitionId} took {duration}ms failed! {ex.Message}");
                throw;
            }
        }
    }
}