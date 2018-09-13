using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.ChangeFeedProcessor.DataAccess;
using Microsoft.Azure.Documents.Client;

namespace DocumentDB.ChangeFeedProcessor.ConsoleApp
{
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