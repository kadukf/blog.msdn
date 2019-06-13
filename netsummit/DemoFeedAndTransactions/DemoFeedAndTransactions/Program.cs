using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.ChangeFeedProcessor;
using Microsoft.Azure.Documents.ChangeFeedProcessor.PartitionManagement;
using Microsoft.Azure.Documents.Client;
using Newtonsoft.Json;

namespace DemoFeedAndTransactions
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

            // add sproc
            Uri dataCollectionUri = UriFactory.CreateDocumentCollectionUri(dbName, dataCollectionName);
            string sprocName = "StoreItems";
            await AddStoredProcedureAsync(client, dataCollectionUri, sprocName);

            // add document manually
            await AddDocumentsAsync(client, dataCollectionUri);

            // add documents in transaction
            Uri sprocUri = UriFactory.CreateStoredProcedureUri(dbName, dataCollectionName, sprocName);
            await StoreDocumentsViaStoredProcedureAsync(client, sprocUri);

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

        private static async Task AddStoredProcedureAsync(DocumentClient client, Uri dataCollectionUri, string sprocName)
        {
            var sprocBody = File.ReadAllText("StoreItems.js");
            await client.UpsertStoredProcedureAsync(dataCollectionUri,
                new StoredProcedure
                {
                    Body = sprocBody,
                    Id = sprocName
                });
        }

        private static async Task StoreDocumentsViaStoredProcedureAsync(DocumentClient client, Uri sprocUri)
        {
            dynamic[] documents = {
                new {id = "sproc_2", user = "B", version = 0},
                new {id = "sproc_1", user = "A", version = 0},
                new {id = "sproc_3", user = "C", version = 0},
            };
            dynamic[] args = { documents };
            Console.WriteLine("Inserting: " + JsonConvert.SerializeObject(documents));

            StoredProcedureResponse<string> response = await client.ExecuteStoredProcedureAsync<string>(sprocUri, args);
            Console.WriteLine("Session token:" + response.SessionToken);
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
            var document = new { id = "manual_1", user = "A", version = 0 };
            var response = await client.CreateDocumentAsync(collectionUri, document);
            ReportAndWait("Insert", document, response);

            document = new { id = "manual_2", user = "B", version = 0 };
            response = await client.CreateDocumentAsync(collectionUri, document);
            ReportAndWait("Insert", document, response);

            document = new { id = "manual_3", user = "C", version = 0 };
            response = await client.CreateDocumentAsync(collectionUri, document);
            ReportAndWait("Insert", document, response);

            document = new { id = "manual_1", user = "A", version = 1 };
            response = await client.UpsertDocumentAsync(collectionUri, document);
            ReportAndWait("Upsert", document, response);

            document = new { id = "manual_1", user = "A", version = 2 };
            response = await client.UpsertDocumentAsync(collectionUri, document);
            ReportAndWait("Upsert", document, response);
        }

        private static void ReportAndWait(string step, object document, ResourceResponse<Document> response)
        {
            Console.WriteLine(step + ": " + JsonConvert.SerializeObject(document));
            Console.WriteLine("Session token:" + response.SessionToken);
            Console.ReadLine();
        }
    }
}
