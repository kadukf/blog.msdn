using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Documents.ChangeFeedProcessor.FeedProcessing;

namespace DocumentDB.ChangeFeedProcessor.ConsoleApp
{
    public class ManualCheckpointer
    {
        private readonly TimeSpan _checkpointInterval;
        private readonly ConcurrentQueue<BatchRegistry> _batches = new ConcurrentQueue<BatchRegistry>();

        public ManualCheckpointer(TimeSpan checkpointInterval)
        {
            _checkpointInterval = checkpointInterval;
        }

        public Task RunAsync(CancellationToken cancellationToken)
        {
            async Task TryCheckpointAsync()
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    await CheckpointLastCompletedBatchAsync();

                    try
                    {
                        await Task.Delay(_checkpointInterval, cancellationToken);
                    }
                    catch (TaskCanceledException) { }
                }

                await CheckpointLastCompletedBatchAsync();
            }

            return TryCheckpointAsync();
        }

        private async Task CheckpointLastCompletedBatchAsync()
        {
            try
            {
                BatchRegistry lastCompletedRegistry = null;

                while (_batches.TryPeek(out var batch))
                {
                    if (batch.IsCompleted)
                    {
                        _batches.TryDequeue(out batch);
                        lastCompletedRegistry = batch;
                    }
                    else
                    {
                        break;
                    }
                }

                if (lastCompletedRegistry != null)
                {
                    Console.WriteLine("Going to checkpoint!");
                    await lastCompletedRegistry.CheckpointAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error occured while executing checkpoint. {ex}");
            }
        }


        public void AddBatch(Task[] tasks, IChangeFeedObserverContext context)
        {
            _batches.Enqueue(new BatchRegistry(Task.WhenAll(tasks), context));
        }
    }
}