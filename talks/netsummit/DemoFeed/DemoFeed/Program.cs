using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.ChangeFeedProcessor;
using Microsoft.Azure.Documents.ChangeFeedProcessor.PartitionManagement;
using Microsoft.Azure.Documents.Client;
using Newtonsoft.Json;

namespace DemoFeed
{
    class Program
    {

        static async Task Main(string[] args)
        {
            var serverUri = "https://localhost:8081/";
            var client = new DocumentClient(new Uri(serverUri), "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==");
            var dataCollectionName = "data";
            var dbName = "DB";

            // setting up the database for inserting the data
            var database = new Database { Id = dbName };
            var dbUri = UriFactory.CreateDatabaseUri(dbName);
            await SetupDatabaseAsync(client, dbUri, database, dataCollectionName);

            // add documents
            Uri dataCollectionUri = UriFactory.CreateDocumentCollectionUri(dbName, dataCollectionName);
            await AddDocumentsAsync(client, dataCollectionUri);

            // prepate change feed processor bookeeping collection
            // setup and start the change feed processor
            Console.WriteLine("Starting change feed processor...");
            Console.ReadLine();
            var processor = await StartChangeFeedProcessorAsync(dataCollectionName, client, dbName, serverUri);

            Console.WriteLine("Running...[Press ENTER to stop]");
            Console.ReadLine();

            await processor.StopAsync().ConfigureAwait(false);
            Console.WriteLine("Stopped...[Press ENTER to stop]");
            Console.ReadLine();
        }

        private static async Task SetupDatabaseAsync(DocumentClient client, Uri dbUri, Database database,
            string dataCollectionName)
        {
            try
            {
                await client.DeleteDatabaseAsync(dbUri);
            }
            catch (DocumentClientException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                Console.WriteLine("No database to be deleted.");
            }

            await client.CreateDatabaseIfNotExistsAsync(database);
            await client.CreateDocumentCollectionIfNotExistsAsync(dbUri, new DocumentCollection { Id = dataCollectionName });
        }

        private static async Task<IChangeFeedProcessor> StartChangeFeedProcessorAsync(
            string dataCollectionName,
            DocumentClient client,
            string dbName,
            string serverUri)
        {
            var leasesCollectionName = dataCollectionName + ".leases";
            await client.CreateDocumentCollectionIfNotExistsAsync(UriFactory.CreateDatabaseUri(dbName), new DocumentCollection { Id = leasesCollectionName });

            IChangeFeedProcessor processor = await new ChangeFeedProcessorBuilder()
                .WithObserver<ConsoleObserver>()
                .WithHostName("console_app_host")
                .WithFeedDocumentClient(client)
                .WithFeedCollection(new DocumentCollectionInfo
                {
                    CollectionName = dataCollectionName,
                    DatabaseName = dbName,
                    Uri = new Uri(serverUri)
                })
                .WithLeaseDocumentClient(client)
                .WithLeaseCollection(new DocumentCollectionInfo
                {
                    CollectionName = leasesCollectionName,
                    DatabaseName = dbName
                })
                .WithProcessorOptions(new ChangeFeedProcessorOptions()
                {
                    StartFromBeginning = true
                })
                .BuildAsync();

            await processor.StartAsync().ConfigureAwait(false);
            return processor;
        }

        private static async Task AddDocumentsAsync(DocumentClient client, Uri collectionUri)
        {
            var document = new { id = "1", user = "A", version = 0 };
            var response = await client.CreateDocumentAsync(collectionUri, document);
            Console.WriteLine("Inserted: " + JsonConvert.SerializeObject(document));
            Console.WriteLine("Session token:" + response.SessionToken);
            Console.ReadLine();

            document = new { id = "2", user = "B", version = 0 };
            response = await client.CreateDocumentAsync(collectionUri, document);
            Console.WriteLine("Inserted: " + JsonConvert.SerializeObject(document));
            Console.WriteLine("Session token:" + response.SessionToken);
            Console.ReadLine();

            document = new { id = "3", user = "C", version = 0 };
            response = await client.CreateDocumentAsync(collectionUri, document);
            Console.WriteLine("Inserted: " + JsonConvert.SerializeObject(document));
            Console.WriteLine("Session token:" + response.SessionToken);
            Console.ReadLine();

            document = new { id = "1", user = "A", version = 1 };
            response = await client.UpsertDocumentAsync(collectionUri, document);
            Console.WriteLine("Upserted: " + JsonConvert.SerializeObject(document));
            Console.WriteLine("Session token:" + response.SessionToken);
            Console.ReadLine();

            document = new { id = "1", user = "A", version = 2 };
            response = await client.UpsertDocumentAsync(collectionUri, document);
            Console.WriteLine("Upserted: " + JsonConvert.SerializeObject(document));
            Console.WriteLine("Session token:" + response.SessionToken);
            Console.ReadLine();
        }
    }
}
