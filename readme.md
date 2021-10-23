# NuGet V3 latest resource

**Goal**: Speed up Visual Studio's Package Manager UI by prototyping new server APIs! 🚀

Kudos to [@nkolev92](https://github.com/nkolev92) for the idea!

## Background

Customers use Visual Studio's Package Manager (PM) UI to browse and install packages. The PM UI displays packages' latest version by default. Today Visual Studio does this using [NuGet V3's package metadata API](https://docs.microsoft.com/nuget/api/registration-base-url-resource). This API has a few flaws:

1. It includes the metadata for all versions of a package.
1. Packages that with more than 128 versions are split across multiple "pages"
1. It includes [unlisted versions](https://docs.microsoft.com/en-us/nuget/nuget-org/policies/deleting-packages), which are hidden from the UI.

This makes it difficult for Visual Studio to quickly display packages' latest metadata.

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