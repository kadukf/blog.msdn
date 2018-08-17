using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using log4net.Core;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.ChangeFeedProcessor;
using Microsoft.Azure.Documents.ChangeFeedProcessor.DataAccess;
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
            IChangeFeedDocumentClient dbClient = new ChangeFeedDocumentClient(new DocumentClient(new Uri(uri), key));
            dbClient = new QoSMeteringChangeFeedDocumentClient(dbClient, new QoSMeteringReporter());


            var builder = new ChangeFeedProcessorBuilder()
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
                .WithFeedDocumentClient(dbClient)
                .WithLeaseDocumentClient(dbClient);

            var processor = await builder.BuildAsync();
            var estimator = await builder.BuildEstimatorAsync();

            await processor.StartAsync().ConfigureAwait(false);
            return processor;
        }
    }

    public interface IQoSMeteringReporter
    {
        Task<T> MeasureAsync<T>(string name, string partitionId, Func<Task<T>> func);
    }

    public class QoSMeteringReporter : IQoSMeteringReporter
    {
        public async Task<T> MeasureAsync<T>(string name, string partitionId, Func<Task<T>> func)
        {
            var startTime = Stopwatch.GetTimestamp();
            T obj = default(T);
            try
            {
                T ret = await func();
                obj = ret;
                int duration = TimeSpan.FromTicks(Stopwatch.GetTimestamp() - startTime).Milliseconds;
                Console.WriteLine($"QoS: {name} on range {partitionId} took {duration}ms");
                return obj;
            }
            catch (Exception ex)
            {
                int duration = TimeSpan.FromTicks(Stopwatch.GetTimestamp() - startTime).Milliseconds;
                Console.WriteLine($"QoS: {name} on range {partitionId} took {duration}ms failed! {ex.Message}");
                throw;
            }
        }
    }

    public class QoSMeteringChangeFeedDocumentQuery : IChangeFeedDocumentQuery<Document>
    {
        private readonly IChangeFeedDocumentQuery<Document> _inner;
        private readonly string _partitionRangeId;
        private readonly IQoSMeteringReporter _meter;

        public QoSMeteringChangeFeedDocumentQuery(IChangeFeedDocumentQuery<Document> inner, string partitionRangeId, IQoSMeteringReporter meter)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _partitionRangeId = partitionRangeId ?? throw new ArgumentNullException(nameof(partitionRangeId));
            _meter = meter ?? throw new ArgumentNullException(nameof(meter));
        }

        public void Dispose()
        {
            _inner.Dispose();
        }

        public Task<IFeedResponse<TResult>> ExecuteNextAsync<TResult>(CancellationToken token = new CancellationToken())
        {
            return _meter.MeasureAsync(
                "ExecuteNextAsync",
                _partitionRangeId,
                async () => await _inner.ExecuteNextAsync<TResult>(token)
            );
        }

        public bool HasMoreResults => _inner.HasMoreResults;
    }

    public class QoSMeteringChangeFeedDocumentClient : IChangeFeedDocumentClient
    {
        private readonly IChangeFeedDocumentClient _inner;
        private readonly IQoSMeteringReporter _meter;

        public QoSMeteringChangeFeedDocumentClient(IChangeFeedDocumentClient changeFeedDocumentClientImplementation, IQoSMeteringReporter meter)
        {
            _inner = changeFeedDocumentClientImplementation ?? throw new ArgumentNullException(nameof(changeFeedDocumentClientImplementation));
            _meter = meter ?? throw new ArgumentNullException(nameof(meter));
        }

        public Task<IFeedResponse<PartitionKeyRange>> ReadPartitionKeyRangeFeedAsync(string partitionKeyRangesOrCollectionLink, FeedOptions options)
        {
            return _meter.MeasureAsync(
                "ReadPartitionKeyRangeFeedAsync",
                options.PartitionKeyRangeId,
                async () => await _inner.ReadPartitionKeyRangeFeedAsync(partitionKeyRangesOrCollectionLink, options));
        }

        public IChangeFeedDocumentQuery<Document> CreateDocumentChangeFeedQuery(string collectionLink, ChangeFeedOptions feedOptions)
        {
            return new QoSMeteringChangeFeedDocumentQuery(_inner.CreateDocumentChangeFeedQuery(collectionLink, feedOptions), feedOptions.PartitionKeyRangeId, _meter);
        }

        public async Task<IResourceResponse<Database>> ReadDatabaseAsync(Uri databaseUri, RequestOptions options)
        {
            return await _inner.ReadDatabaseAsync(databaseUri, options);
        }

        public async Task<IResourceResponse<DocumentCollection>> ReadDocumentCollectionAsync(Uri documentCollectionUri, RequestOptions options)
        {
            return await _inner.ReadDocumentCollectionAsync(documentCollectionUri, options);
        }

        public async Task<IResourceResponse<Document>> CreateDocumentAsync(string collectionLink, object document)
        {
            return await _inner.CreateDocumentAsync(collectionLink, document);
        }

        public async Task<IResourceResponse<Document>> DeleteDocumentAsync(Uri documentUri)
        {
            return await _inner.DeleteDocumentAsync(documentUri);
        }

        public async Task<IResourceResponse<Document>> ReplaceDocumentAsync(Uri documentUri, object document, RequestOptions options)
        {
            return await _inner.ReplaceDocumentAsync(documentUri, document, options);
        }

        public async Task<IResourceResponse<Document>> ReadDocumentAsync(Uri documentUri)
        {
            return await _inner.ReadDocumentAsync(documentUri);
        }

        public IQueryable<T> CreateDocumentQuery<T>(string documentCollectionUri, SqlQuerySpec querySpec)
        {
            return _inner.CreateDocumentQuery<T>(documentCollectionUri, querySpec);
        }
    }
}
