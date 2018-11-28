using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.ChangeFeedProcessor;
using Microsoft.Azure.Documents.ChangeFeedProcessor.DataAccess;
using Microsoft.Azure.Documents.ChangeFeedProcessor.PartitionManagement;
using Microsoft.Azure.Documents.Client;
using IChangeFeedObserver = Microsoft.Azure.Documents.ChangeFeedProcessor.FeedProcessing.IChangeFeedObserver;
using IChangeFeedObserverFactory = Microsoft.Azure.Documents.ChangeFeedProcessor.FeedProcessing.IChangeFeedObserverFactory;

namespace DocumentDB.ChangeFeedProcessor.ConsoleApp
{
    class Program
    {
        private const string DbName = "DB";

        static async Task Main(string[] args)
        {
            var dbUri = "https://localhost:8081/";
            var key = "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";
            var collectionName = "Input";

            await SetupEnvironmentAsync(dbUri, key, collectionName);

            CancellationTokenSource cts = new CancellationTokenSource();

            Task feedingTask = StartFeedingDataAsync(dbUri, key, collectionName, cts.Token);
            Task monitoringTask = StartMonitoringAsync(dbUri, key, collectionName, cts.Token);

            IChangeFeedProcessor processor = await RunChangeFeedProcessorAsync(dbUri, key, collectionName);

            Console.WriteLine("Running...[Press ENTER to stop]");
            Console.ReadLine();

            Console.WriteLine("Stopping...");
            cts.Cancel();
            await feedingTask.ConfigureAwait(false);
            await processor.StopAsync().ConfigureAwait(false);
            await monitoringTask.ConfigureAwait(false);
            Console.WriteLine("Stopped");
            Console.ReadLine();
        }

        private static async Task StartMonitoringAsync(string uri, string key, string collection, CancellationToken ctsToken)
        {
            IRemainingWorkEstimator estimator = await CreateEstimatorAsync(uri, key, collection);

            StringBuilder builder = new StringBuilder();
            while (!ctsToken.IsCancellationRequested)
            {
                builder.Clear();

                IReadOnlyList<RemainingPartitionWork> remainingWork = await estimator.GetEstimatedRemainingWorkPerPartitionAsync();

                for (int i = 0; i < remainingWork.Count; i++)
                {
                    var work = remainingWork[i];
                    if (i != 0) builder.Append(",");
                    builder.AppendFormat(work.PartitionKeyRangeId + ":" + work.RemainingWork);
                }
                Console.WriteLine($"### Estimated work: {builder}");

                try
                {
                    await Task.Delay(5000, ctsToken);
                }
                catch (TaskCanceledException) { }
            }
        }

        private static async Task<IRemainingWorkEstimator> CreateEstimatorAsync(string uri, string key, string collection)
        {
            IChangeFeedDocumentClient dbClient = new ChangeFeedDocumentClient(new DocumentClient(new Uri(uri), key));

            var builder = new ChangeFeedProcessorBuilder()
                .WithHostName("console_app_host")
                .WithFeedCollection(new DocumentCollectionInfo()
                {
                    Uri = new Uri(uri),
                    MasterKey = key,
                    CollectionName = collection,
                    DatabaseName = DbName
                })
                .WithLeaseCollection(new DocumentCollectionInfo()
                {
                    CollectionName = $"{collection}.Lease.ConsoleApp",
                    DatabaseName = DbName,
                    Uri = new Uri(uri),
                    MasterKey = key
                })
                .WithFeedDocumentClient(dbClient)
                .WithLeaseDocumentClient(dbClient);

            return await builder.BuildEstimatorAsync();
        }

        private static async Task SetupEnvironmentAsync(string dbUri, string key, string collectionName)
        {
            var client = new DocumentClient(new Uri(dbUri), key);
            var database = new Database() { Id = DbName };
            await client.CreateDatabaseIfNotExistsAsync(database);
            await client.CreateDocumentCollectionIfNotExistsAsync(UriFactory.CreateDatabaseUri(DbName), new DocumentCollection() { Id = collectionName });
            await client.CreateDocumentCollectionIfNotExistsAsync(UriFactory.CreateDatabaseUri(DbName), new DocumentCollection() { Id = $"{collectionName}.Lease.ConsoleApp" });
        }

        private static Task StartFeedingDataAsync(string dbUri, string key, string collectionName, CancellationToken ctsToken)
        {
            var client = new DocumentClient(new Uri(dbUri), key);
            Uri collectionUri = UriFactory.CreateDocumentCollectionUri(DbName, collectionName);

            async Task FeedDocumentsAsync()
            {
                while (!ctsToken.IsCancellationRequested)
                {
                    await client.CreateDocumentAsync(collectionUri, new {
                        type = "event",
                        id = $"{DateTime.UtcNow.Ticks}.{Guid.NewGuid().ToString()}" });
                }
            }

            return Task.Run(FeedDocumentsAsync, ctsToken);
        }

        private static async Task<IChangeFeedProcessor> RunChangeFeedProcessorAsync(string uri, string key, string collection)
        {
            IChangeFeedDocumentClient dbClient = new ChangeFeedDocumentClient(new DocumentClient(new Uri(uri), key));

            var builder = new ChangeFeedProcessorBuilder()
                .WithProcessorOptions(new ChangeFeedProcessorOptions()
                {
                    CheckpointFrequency = new CheckpointFrequency()
                    {
                        ExplicitCheckpoint = true
                    }
                })
                .WithObserverFactory(new ConsoleObserverFactory(TimeSpan.FromSeconds(10)))
                .WithHostName("console_app_host")
                .WithFeedCollection(new DocumentCollectionInfo()
                {
                    Uri = new Uri(uri),
                    MasterKey = key,
                    CollectionName = collection,
                    DatabaseName = DbName
                })
                .WithLeaseCollection(new DocumentCollectionInfo()
                {
                    CollectionName = $"{collection}.Lease.ConsoleApp",
                    DatabaseName = DbName,
                    Uri = new Uri(uri),
                    MasterKey = key
                })
                .WithFeedDocumentClient(dbClient)
                .WithLeaseDocumentClient(dbClient);

            var processor = await builder.BuildAsync();

            await processor.StartAsync().ConfigureAwait(false);
            return processor;
        }
    }

    public class ConsoleObserverFactory : IChangeFeedObserverFactory
    {
        private readonly TimeSpan _checkpointInterval;

        public ConsoleObserverFactory(TimeSpan checkpointInterval)
        {
            _checkpointInterval = checkpointInterval;
        }

        public IChangeFeedObserver CreateObserver()
        {
            return new ManualCheckpointObserver(new ManualCheckpointer(_checkpointInterval));
        }
    }
}
