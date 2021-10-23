using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using BaGet.Protocol;
using BaGet.Protocol.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace NuGet.Jobs.Catalog2Latest
{
    public class PackageMetadataWorker
    {
        private readonly NuGetClientFactory _factory;
        private readonly BlobContainerClient _blobContainer;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly IOptionsSnapshot<Catalog2LatestOptions> _options;
        private readonly ILogger<PackageMetadataWorker> _logger;

        public PackageMetadataWorker(
            NuGetClientFactory factory,
            BlobServiceClient blobs,
            IOptionsSnapshot<Catalog2LatestOptions> options,
            ILogger<PackageMetadataWorker> logger)
        {
            _jsonOptions = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            };

            _factory = factory;
            _blobContainer = blobs.GetBlobContainerClient("latest");
            _options = options;
            _logger = logger;
        }

        public async Task WorkAsync(
            ChannelReader<string> packageIdReader,
            CancellationToken cancellationToken)
        {
            var client = _factory.CreatePackageMetadataClient();

            _logger.LogInformation(
                "Indexing package metadata to using {ConsumerWorkers} workers...",
                _options.Value.ConsumerWorkers);

            // TODO: Upgrade to .NET 6 -> Parallel.ForEachAsync
            var tasks = Enumerable
                .Repeat(0, _options.Value.ConsumerWorkers)
                .Select(async _ =>
                {
                    await Task.Yield();

                    while (await packageIdReader.WaitToReadAsync(cancellationToken))
                    {
                        while (packageIdReader.TryRead(out var packageId))
                        {
                            var done = false;
                            while (!done)
                            {
                                try
                                {
                                    _logger.LogDebug("Processing package {PackageId}...", packageId);

                                    var index = await GetInlinedRegistrationIndexOrNullAsync(client, packageId, cancellationToken);
                                    if (index != null)
                                    {
                                        var latestResponses = BuildLatestResponses(index);
                                        var path = packageId.ToLowerInvariant();

                                        await WriteJsonAsync(path, "latest", latestResponses.Latest, cancellationToken);
                                        await WriteJsonAsync(path, "latest-stable", latestResponses.LatestStable, cancellationToken);
                                        await WriteJsonAsync(path, "latest-prerelease", latestResponses.LatestPrerelease, cancellationToken);
                                    }

                                    done = true;

                                    _logger.LogDebug("Processed package {PackageId}", packageId);
                                }
                                catch (Exception e) when (!cancellationToken.IsCancellationRequested)
                                {
                                    _logger.LogError(e, "Retrying package {PackageId} in 5 seconds...", packageId);
                                    await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
                                }
                            }
                        }
                    }
                });

            await Task.WhenAll(tasks);
        }

        private async Task<RegistrationIndexResponse> GetInlinedRegistrationIndexOrNullAsync(
            IPackageMetadataClient client,
            string packageId,
            CancellationToken cancellationToken)
        {
            // Return the index directly if it is not or if all the pages are inlined.
            var index = await client.GetRegistrationIndexOrNullAsync(packageId, cancellationToken);
            if (index == null || index.Pages.All(p => p.ItemsOrNull != null))
            {
                return index;
            }

            // Create a new registration index response with inlined pages.
            var pages = new List<RegistrationIndexPage>();

            foreach (var pageItem in index.Pages)
            {
                if (pageItem.ItemsOrNull == null)
                {
                    var page = await client.GetRegistrationPageAsync(pageItem.RegistrationPageUrl, cancellationToken);

                    pages.Add(page);
                }
            }

            return new RegistrationIndexResponse
            {
                RegistrationIndexUrl = index.RegistrationIndexUrl,
                Type = index.Type,
                Count = index.Count,
                Pages = pages
            };
        }

        private LatestResponses BuildLatestResponses(RegistrationIndexResponse index)
        {
            var packages = index
                .Pages
                .SelectMany(p => p.ItemsOrNull)
                .Select(i => i.PackageMetadata)
                .Where(p => p.IsListed())
                .Select(p => new { PackageMetadata = p, Version = p.ParseVersion() })
                .OrderByDescending(p => p.Version)
                .ToList();

            var stable = packages
                .Where(p => !p.Version.IsPrerelease)
                .Select(p => p.PackageMetadata)
                .FirstOrDefault();
            var prerelease = packages
                .Select(p => p.PackageMetadata)
                .FirstOrDefault();

            return new(
                Latest: new() { Stable = stable, Prerelease = prerelease },
                LatestStable: new() { Stable = stable, Prerelease = null },
                LatestPrerelease: new() { Stable = null, Prerelease = prerelease });
        }
        
        private async Task WriteJsonAsync(
            string directory,
            string fileName,
            LatestResponse content,
            CancellationToken cancellationToken)
        {
            var name = Path.Combine(directory, fileName + ".json");
            var blob = _blobContainer.GetBlobClient(name);
            var headers = new BlobHttpHeaders
            {
                ContentType = "application/json",
                CacheControl = "no-store",
                ContentEncoding = "gzip",
            };

            using var stream = new MemoryStream();
            using (var gzip = new GZipStream(stream, CompressionLevel.Optimal, leaveOpen: true))
            {
                await JsonSerializer.SerializeAsync(gzip, content, _jsonOptions, cancellationToken);
                await gzip.FlushAsync(cancellationToken);
            }

            stream.Position = 0;
            await blob.UploadAsync(stream, headers, cancellationToken: cancellationToken);
        }

        private record LatestResponses(
            LatestResponse Latest,
            LatestResponse LatestStable,
            LatestResponse LatestPrerelease);
    }
}
