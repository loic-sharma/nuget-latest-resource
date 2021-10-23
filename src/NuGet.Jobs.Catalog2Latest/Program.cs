using Azure.Identity;
using BaGet.Protocol;
using BaGet.Protocol.Catalog;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace NuGet.Jobs.Catalog2Latest
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            // TODO: NuGetClientFactory should accept a function to create the httpclient
            // TODO: NuGetClientFactory should have an interface.
            ThreadPool.SetMinThreads(workerThreads: 32, completionPortThreads: 4);
            ServicePointManager.DefaultConnectionLimit = 32;
            ServicePointManager.MaxServicePointIdleTime = 10000;

            var hostBuilder = Host.CreateDefaultBuilder(args);

            try
            {
                await hostBuilder
                    .ConfigureServices(ConfigureService)
                    .RunConsoleAsync();
            }
            catch (OperationCanceledException)
            {
            }
        }

        private static void ConfigureService(HostBuilderContext ctx, IServiceCollection services)
        {
            services.Configure<Catalog2LatestOptions>(ctx.Configuration.GetSection("Catalog2Latest"));

            services
                .AddHttpClient("NuGet")
                .ConfigurePrimaryHttpMessageHandler(handler =>
                {
                    return new HttpClientHandler
                    {
                        AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip
                    };
                });

            services.AddSingleton(provider =>
            {
                var factory = provider.GetRequiredService<IHttpClientFactory>();
                var httpClient = factory.CreateClient("NuGet");

                var serviceIndex = "https://api.nuget.org/v3/index.json";

                return new NuGetClientFactory(httpClient, serviceIndex);
            });


            services.AddAzureClients(builder =>
            {
                builder.AddBlobServiceClient(ctx.Configuration.GetSection("Storage"));

                builder.UseCredential(new DefaultAzureCredential());
            });

            services.AddSingleton<ICursor, BlobCursor>();
            services.AddSingleton<CatalogLeafItemProducer>();
            services.AddSingleton<PackageMetadataWorker>();
            services.AddHttpClient<PackageMetadataCursor>();

            services.AddHostedService<EnsureBlobContainer>();
            services.AddHostedService<Catalog2LatestJob>();
        }
    }
}
