using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.ChangeFeedProcessor.FeedProcessing;

namespace DemoFeedAndTransactions
{
    public class ConsoleObserver : IChangeFeedObserver
    {
        public Task OpenAsync(IChangeFeedObserverContext context)
        {
            Console.WriteLine("Opening partition processing:" + context.PartitionKeyRangeId);
            return Task.CompletedTask;
        }

        public Task CloseAsync(IChangeFeedObserverContext context, ChangeFeedObserverCloseReason reason)
        {
            Console.WriteLine("Closing partition processing:" + context.PartitionKeyRangeId);
            return Task.CompletedTask;
        }

        public Task ProcessChangesAsync(IChangeFeedObserverContext context, IReadOnlyList<Document> docs, CancellationToken cancellationToken)
        {
            Console.WriteLine("Partition: " + context.PartitionKeyRangeId + " received following documents:");
            foreach (var doc in docs)
            {
                Console.WriteLine(doc.ToString());
            }

            return Task.CompletedTask;
        }
    }
}