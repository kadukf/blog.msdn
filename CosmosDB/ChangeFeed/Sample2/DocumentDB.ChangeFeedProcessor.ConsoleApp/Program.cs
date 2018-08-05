using System;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using log4net.Core;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.ChangeFeedProcessor;
using Microsoft.Azure.Documents.ChangeFeedProcessor.Logging;
using Microsoft.Azure.Documents.ChangeFeedProcessor.PartitionManagement;
using Microsoft.Azure.Documents.Client;

namespace DocumentDB.ChangeFeedProcessor.ConsoleApp
{
    class Program
    {
        private const string DbName = "DB";

        static async Task Main(string[] args)
        {
            log4net.Config.XmlConfigurator.Configure();

            // for using System.Diagnostics.TraceSource logging uncomment the line bellow
            // LogProvider.SetCurrentLogProvider(new TraceLogProvider());

            var dbUri = "https://localhost:8081/";
            var key = "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";
            var collectionName = "Input";

            await SetupEnvironmentAsync(dbUri, key, collectionName);

            CancellationTokenSource cts = new CancellationTokenSource();

            Task feedingTask = StartFeedingDataAsync(dbUri, key, collectionName, cts.Token);

            IChangeFeedProcessor processor = await RunChangeFeedProcessorAsync(dbUri, key, collectionName);

            Console.WriteLine("Running...[Press ENTER to stop]");
            Console.ReadLine();

            Console.WriteLine("Stopping...");
            cts.Cancel();
            await feedingTask.ConfigureAwait(false);
            await processor.StopAsync().ConfigureAwait(false);
            Console.WriteLine("Stopped");
            Console.ReadLine();
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
                    await client.CreateDocumentAsync(collectionUri, new { type = "event", id = Guid.NewGuid().ToString() });
                }
            }

            return Task.Run(FeedDocumentsAsync, ctsToken);
        }

        private static async Task<IChangeFeedProcessor> RunChangeFeedProcessorAsync(string uri, string key, string collection)
        {
            var processor = await new ChangeFeedProcessorBuilder()
                .WithObserver<ConsoleObserver>()
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
                .BuildAsync();

            await processor.StartAsync().ConfigureAwait(false);
            return processor;
        }
    }
}
