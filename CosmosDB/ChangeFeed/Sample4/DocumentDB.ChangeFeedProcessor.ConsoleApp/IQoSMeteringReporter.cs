using System;
using System.Threading.Tasks;

namespace DocumentDB.ChangeFeedProcessor.ConsoleApp
{
    public interface IQoSMeteringReporter
    {
        Task<T> MeasureAsync<T>(string name, string partitionId, Func<Task<T>> func);
    }
}