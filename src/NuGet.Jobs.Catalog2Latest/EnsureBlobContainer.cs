using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Threading.Tasks;

namespace NuGet.Jobs.Catalog2Latest
{
    class EnsureBlobContainer : IHostedService
    {
        private readonly BlobServiceClient _blobs;

        public EnsureBlobContainer(BlobServiceClient blobs)
        {
            _blobs = blobs;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await _blobs
                .GetBlobContainerClient("latest")
                .CreateIfNotExistsAsync(PublicAccessType.Blob, cancellationToken: cancellationToken);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
