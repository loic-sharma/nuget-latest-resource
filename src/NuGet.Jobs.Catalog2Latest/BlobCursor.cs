using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using BaGet.Protocol.Catalog;
using Microsoft.Extensions.Logging;
using System;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace NuGet.Jobs.Catalog2Latest
{
    public class BlobCursor : ICursor
    {
        private readonly BlobContainerClient _blobContainer;
        private readonly ILogger<BlobCursor> _logger;

        public BlobCursor(BlobServiceClient blobs, ILogger<BlobCursor> logger)
        {
            _blobContainer = blobs.GetBlobContainerClient("latest");
            _logger = logger;
        }

        public async Task<DateTimeOffset?> GetAsync(CancellationToken cancellationToken)
        {
            try
            {
                var response = await _blobContainer.GetBlobClient("cursor.json").DownloadContentAsync(cancellationToken);

                var data = response.Value.Content.ToObjectFromJson<Data>();

                _logger.LogDebug("Read cursor value {cursor:O}", data.Value);

                return data.Value;
            }
            catch (RequestFailedException e) when (e.ErrorCode == BlobErrorCode.BlobNotFound)
            {
                return null;
            }
        }

        public async Task SetAsync(DateTimeOffset value, CancellationToken cancellationToken)
        {
            var data = BinaryData.FromObjectAsJson(new Data { Value = value });

            await _blobContainer
                .GetBlobClient("cursor.json")
                .UploadAsync(data, overwrite: true, cancellationToken);

            _logger.LogDebug("Wrote cursor value {cursor:O}", value);

        }

        private class Data
        {
            [JsonPropertyName("value")]
            public DateTimeOffset Value { get; set; }
        }
    }
}
