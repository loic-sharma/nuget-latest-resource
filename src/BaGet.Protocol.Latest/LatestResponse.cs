using System.Text.Json.Serialization;

namespace BaGet.Protocol.Models
{
    public class LatestResponse
    {
        /// <summary>
        /// The metadata for the package's latest listed stable version.
        /// <c>null</c> if the package does not have any listed stable versions.
        /// </summary>
        [JsonPropertyName("stable")]
        public PackageMetadata Stable { get; set; }

        /// <summary>
        /// The metadata for the package's latest listed version, including pre-releases.
        /// <c>null</c> if the package does not have any listed versions.
        /// </summary>
        [JsonPropertyName("prerelease")]
        public PackageMetadata Prerelease { get; set; }
    }
}

