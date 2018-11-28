using System;
using System.Threading.Tasks;
using Microsoft.Azure.Documents.ChangeFeedProcessor.FeedProcessing;

namespace DocumentDB.ChangeFeedProcessor.ConsoleApp
{
    public class BatchRegistry
    {
        private readonly Task _batch;
        private readonly IChangeFeedObserverContext _context;

        public bool IsCompleted => _batch.IsCompleted;

        public BatchRegistry(Task batch, IChangeFeedObserverContext context)
        {
            _batch = batch ?? throw new ArgumentNullException(nameof(batch));
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task CheckpointAsync()
        {
            await _context.CheckpointAsync();
        }
    }
}