using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.ChangeFeedProcessor.DataAccess;
using Microsoft.Azure.Documents.Client;

namespace DocumentDB.ChangeFeedProcessor.ConsoleApp
{
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
}