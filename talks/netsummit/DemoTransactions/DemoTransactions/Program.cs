using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Newtonsoft.Json;

namespace DemoTransactions
{
    class Program
    {

        static async Task Main(string[] args)
        {
            var serverUri = "https://localhost:8081/";
            var client = new DocumentClient(new Uri(serverUri), "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==");
            var dataCollectionName = "data";
            var dbName = "DB";

            // Step1: setting up the database for inserting the data
            var database = new Database { Id = dbName };
            var dbUri = UriFactory.CreateDatabaseUri(dbName);
            await SetupDatabaseAsync(client, dbUri, database, dataCollectionName);

            // Step2: add sproc
            Uri dataCollectionUri = UriFactory.CreateDocumentCollectionUri(dbName, dataCollectionName);
            string sprocName = "StoreItems";
            await AddStoredProcedureAsync(client, dataCollectionUri, sprocName);

            // Step3: add document manually
            await AddDocumentAsync(client, dataCollectionUri);

            // Step4: add documents in transaction
            Uri sprocUri = UriFactory.CreateStoredProcedureUri(dbName, dataCollectionName, sprocName);
            await StoreDocumentsViaStoredProcedureAsync(client, sprocUri);
        }

        private static async Task SetupDatabaseAsync(DocumentClient client, Uri dbUri, Database database, string dataCollectionName)
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
            var documents = new[] {
                new {id = "2", user = "B", version = 0},
                new {id = "1", user = "A", version = 0},
                new {id = "3", user = "C", version = 0},
            };
            var args = new[] {documents};
            Console.WriteLine("Storing documents over stored procedure: " + JsonConvert.SerializeObject(documents));

            StoredProcedureResponse<string> response = await client.ExecuteStoredProcedureAsync<string>(sprocUri, args);
            Console.WriteLine("Session token:" + response.SessionToken);
            Console.ReadLine();
        }

        private static async Task AddDocumentAsync(DocumentClient client, Uri collectionUri)
        {
            var document = new { id = "manual_0", version = 0 };
            Console.WriteLine("Creating document: " + JsonConvert.SerializeObject(document));
            ResourceResponse<Document> response = await client.CreateDocumentAsync(collectionUri, document);
            Console.WriteLine("Session token:" + response.SessionToken);
            Console.ReadLine();

            document = new { id = "manual_1", version = 0 };
            Console.WriteLine("Creating document: " + JsonConvert.SerializeObject(document));
            response = await client.CreateDocumentAsync(collectionUri, document);
            Console.WriteLine("Session token:" + response.SessionToken);
            Console.ReadLine();
        }

    }
}
