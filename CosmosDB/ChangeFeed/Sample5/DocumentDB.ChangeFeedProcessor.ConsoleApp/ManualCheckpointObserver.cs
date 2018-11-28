using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.ChangeFeedProcessor.FeedProcessing;

namespace DocumentDB.ChangeFeedProcessor.ConsoleApp
{
    public class ManualCheckpointObserver : IChangeFeedObserver
    {
        private readonly ManualCheckpointer _checkpointer;
        private CancellationTokenSource _ctsClose;
        private Task _checkpointTask;

        public ManualCheckpointObserver(ManualCheckpointer checkpointer)
        {
            _checkpointer = checkpointer ?? throw new ArgumentNullException(nameof(checkpointer));
        }

        public Task OpenAsync(IChangeFeedObserverContext context)
        {
            Console.WriteLine("Open:" + context.PartitionKeyRangeId);
            _ctsClose = new CancellationTokenSource();
            _checkpointTask = _checkpointer.RunAsync(_ctsClose.Token);
            return Task.CompletedTask;
        }

        public async Task CloseAsync(IChangeFeedObserverContext context, ChangeFeedObserverCloseReason reason)
        {
            _ctsClose.Cancel();
            await _checkpointTask;
            Console.WriteLine("Closed:" + context.PartitionKeyRangeId + " due to " + reason);
        }

        public async Task ProcessChangesAsync(IChangeFeedObserverContext context, IReadOnlyList<Document> docs, CancellationToken cancellationToken)
        {
            if (docs.Count > 0)
            {
                Console.WriteLine("Age of the documents: " + (DateTime.UtcNow - docs[0].Timestamp).TotalSeconds + " seconds");
            }

            Task[] tasks = new Task[docs.Count];
            for (int i = 0; i < docs.Count; i++)
            {
                tasks[i] = HandleDocumentAsync(docs[i]);
            }

            _checkpointer.AddBatch(tasks, context);
        }

        private Task HandleDocumentAsync(Document document)
        {
            return Task.Delay(TimeSpan.FromMilliseconds(100));
        }
    }
}