using Newtonsoft.Json;
using System.Collections.Generic;

namespace Blog_Manager.Models
{
    public class GitHubRelease
    {
        [JsonProperty("tag_name")]
        public string TagName { get; set; } = string.Empty;

        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        [JsonProperty("body")]
        public string Body { get; set; } = string.Empty;

        [JsonProperty("prerelease")]
        public bool Prerelease { get; set; }

        [JsonProperty("html_url")]
        public string HtmlUrl { get; set; } = string.Empty;

        [JsonProperty("assets")]
        public List<GitHubAsset> Assets { get; set; } = new();

        /// <summary>
        /// 是否为大版本更新：标题或正文包含 [MAJOR]
        /// </summary>
        public bool IsMajor =>
            (Name?.Contains("[MAJOR]") == true) ||
            (Body?.Contains("[MAJOR]") == true);
    }

    public class GitHubAsset
    {
        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        [JsonProperty("browser_download_url")]
        public string DownloadUrl { get; set; } = string.Empty;

        [JsonProperty("size")]
        public long Size { get; set; }
    }

    public class UpdateCheckResult
    {
        public bool HasUpdate { get; set; }
        public bool IsMajorUpdate { get; set; }
        public string LatestVersion { get; set; } = string.Empty;
        public string? DownloadUrl { get; set; }
        public string? ReleasePageUrl { get; set; }
        public long DownloadSize { get; set; }
        public List<GitHubRelease> NewReleases { get; set; } = new();
        public List<GitHubRelease> AllReleases { get; set; } = new();
    }
}
