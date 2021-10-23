# Catalog2Latest job

This job updates the [NuGet V3 latest resource](../../README.md#proposed-latest-api).

# Running

You can run this job using:

```cs
dotnet run
```

## Resources

You will need an Azure Storage account with a `latest` container. Optionally, you can also create an Azure CDN resource with your storage account as its origin.

## Settings

You can configure the job by modifying the `appsettings.json` file:

* `Storage:ConnectionString` - The connection string to your Azure Blob Storage account containing the `latest` container.
* `Catalog2Latest:MaxPages` - The maximum number of [catalog pages](https://docs.microsoft.com/en-us/nuget/api/catalog-resource#catalog-page) to process in a single batch. Higher values increase throughput but result in less incremental progress. If you are creating a new latest resource, consider increasing this value to `20000` to index the entire catalog in a single batch.
* `Catalog2Latest.ProducerWorkers` - The number of workers reading [catalog pages](https://docs.microsoft.com/en-us/nuget/api/catalog-resource#catalog-page). If you are creating a new latest resource, consider increasing this value to `32`.
* `Catalog2Latest.ConsumerWorkers` - The number of workers creating latest resources.

# Algorithm

Here's how Catalog2Latest works at a high-level:

1. Load the Catalog2Latest's catalog cursor from Azure Blob Storage
1. Load the Catalog2Registration's catalog cursor
1. Fetch catalog pages that are newer than Catalog2Latest's catalog cursor, but older than Catalog2Registration's catalog cursor
1. For each package ID in the catalog pages:
    1. Fetch the package metadata [index](https://docs.microsoft.com/nuget/api/registration-base-url-resource#registration-index) and all [pages](https://docs.microsoft.com/nuget/api/registration-base-url-resource#registration-page) for the package ID
    1. Save an "inlined" registration index plus latest resources to Azure Blob Storage
1. Save Catalog2Latest's catalog cursor to Azure Blob Storage