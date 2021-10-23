# NuGet V3 latest resource

**Goal**: Speed up Visual Studio's Package Manager UI by prototyping new server APIs! 🚀

Kudos to [@nkolev92](https://github.com/nkolev92) for the idea!

## Background

You can use Visual Studio's Package Manager (PM) UI to browse and install packages. By default, the PM UI displays packages' latest version.

Today the PM UI is powered by [NuGet V3's package metadata API](https://docs.microsoft.com/nuget/api/registration-base-url-resource). This API has a few flaws:

1. It includes the metadata for _all_ versions of a package, not just the latest version.
1. Packages that with more than 128 versions are split across multiple "pages"
1. It includes [unlisted versions](https://docs.microsoft.com/en-us/nuget/nuget-org/policies/deleting-packages), which are hidden from the UI.

This makes it difficult for Visual Studio to quickly display packages' latest metadata.

### Data

5,510 unique package IDs (~3.4% of total downloads) are affected by registration paging. Here are the top 10 affected packages:

Package Id | Versions | Total downloads
-- | -- | --
xamarin.rgb | 8,442 | 1,004,138
lovecalculator | 8,186 | 877,666
momentum.pm.portalapi | 6,840 | 19,990,722
momentum.pm.api | 6,155 | 4,155,818
namegenerator | 5,415 | 507,700
perseuslibs | 2,985 | 1,674,791
paket | 2,480 | 3,892,159
hilma.common | 2,451 | 204,900
uno.ui | 2,466 | 1,096,205
paket.core | 2,261 | 3,315,729

Browsing these packages in Visual Studio's PM UI may result in freezes or crashes.

## Performance results

⚠ TODO

## Proposed latest API

### Versioning

The following [service index `@type`s](https://docs.microsoft.com/en-us/nuget/api/overview#resources-and-schema) are used:

@type value | Notes
-- | --
Latest/1.0.0 | The initial release

### Base URL

The base URL for the following APIs is the value of the `@id` property associated with the aforementioned resource `@type` value. In the following document, the placeholder base URL `{@id}` will be used.

### HTTP methods

All URLs found in the latest resource support the HTTP methods `GET` and `HEAD`.

### Latest versions

Fetch the metadata for a package's latest stable and pre-release versions, excluding unlisted versions.

```
GET {@id}/{LOWER_ID}/latest.json
```

#### Request parameters

Name | In | Type | Required | Notes
-- | -- | -- | -- | --
LOWER_ID | URL | string | yes | The package ID, lowercased

#### Response

If the package source has no versions of the provided package ID, a 404 status code is returned.

If the package source has one or more versions, including unlisted versions, a 200 status code is returned. The response body is a JSON object with the following properties:

Name | Type | Required | Notes
-- | -- | -- | --
stable | [`catalogEntry` object](https://docs.microsoft.com/nuget/api/registration-base-url-resource#catalog-entry) | No | The metadata for the package's latest stable version, excluding unlisted versions, or `null` if the package does not have any stable versions.
pre-release | [`catalogEntry` object](https://docs.microsoft.com/nuget/api/registration-base-url-resource#catalog-entry) | No | The metadata for the package's latest version, including pre-releases but excluding unlisted versions, or `null` if the package does not have any versions.

#### Sample request

```
GET https://loshar001.azureedge.net/latest/fake/latest.json
```

#### Sample response

```json
{
  "stable": {
    "id": "Newtonsoft.Json",
    "version": "13.0.1",
     ...
  },
  "prerelease": {
    "id": "Newtonsoft.Json",
    "version": "13.0.1-preview1",
     ...
  }
}
```

### Latest stable version

Fetch the metadata for a package's latest stable version, excluding pre-release and unlisted versions.

```
GET {@id}/{LOWER_ID}/latest-stable.json
```

#### Request parameters

Name | In | Type | Required | Notes
-- | -- | -- | -- | --
LOWER_ID | URL | string | yes | The package ID, lowercased

#### Response

If the package source has no versions of the provided package ID, a 404 status code is returned.

If the package source has one or more versions, including unlisted or pre-release versions, a 200 status code is returned. The response body is a JSON object with the following properties:

Name | Type | Required | Notes
-- | -- | -- | --
stable | [`catalogEntry` object](https://docs.microsoft.com/nuget/api/registration-base-url-resource#catalog-entry) | No | The metadata for the package's latest stable version, excluding pre-release and unlisted versions, or `null` if the package does not have any listed stable versions.

#### Sample request

```
GET https://loshar001.azureedge.net/latest/fake/latest-stable.json
```

#### Sample response

```json
{
  "stable": {
    "id": "Newtonsoft.Json",
    "version": "13.0.1",
     ...
  }
}
```

### Latest, including pre-release versions

Fetch the metadata for a package's latest version, including pre-release versions but excluding unlisted versions.

```
GET {@id}/{LOWER_ID}/latest-prerelease.json
```

#### Request parameters

Name | In | Type | Required | Notes
-- | -- | -- | -- | --
LOWER_ID | URL | string | yes | The package ID, lowercased

#### Response

If the package source has no versions of the provided package ID, a 404 status code is returned.

If the package source has one or more versions, including unlisted, a 200 status code is returned. The response body is a JSON object with the following properties:

Name | Type | Required | Notes
-- | -- | -- | --
prerelease | [`catalogEntry` object](https://docs.microsoft.com/nuget/api/registration-base-url-resource#catalog-entry) | No | The metadata for the package's latest version, excluding unlisted versions, or `null` if the package does not have any listed versions.

#### Sample request

```
GET https://loshar001.azureedge.net/latest/fake/latest-prerelease.json
```

#### Sample response

```json
{
  "prerelease": {
    "id": "Newtonsoft.Json",
    "version": "13.0.1-preview1",
     ...
  }
}
```

## SDK Sample

The project provides a client SDK to interact with the latest resource:

```cs
using var http = new HttpClient();

var clientFactory = new NuGetClientFactory(http, "https://package-source");
var serviceIndexClient = clientFactory.CreateServiceIndexClient();

var latestClient = new LatestClient(serviceIndexClient, http);
var latest = await latestClient.GetLatestOrNullAsync("Newtonsoft.Json");

Console.WriteLine($"Latest stable version: {latest.Stable.Version}");
Console.WriteLine($"Latest stable description: {latest.Stable.Description}");
```

<details>
<summary>⚠ Current SDK is a bit hacky. Ideally usage would look like...</summary>

```cs
using var http = new HttpClient();

var clientFactory = new NuGetClientFactory(http, "https://package-source");
var client = clientFactory.CreateLatestClient();

var latest = await latestClient.GetLatestOrNullAsync("Newtonsoft.Json");

Console.WriteLine($"Latest stable version: {latest.Stable.Version}");
Console.WriteLine($"Latest stable description: {latest.Stable.Description}");
```

</details>