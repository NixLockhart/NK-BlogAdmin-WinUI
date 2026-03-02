using Blog_Manager.Helpers;
using Blog_Manager.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace Blog_Manager.Services
{
    public class UpdateService
    {
        private const string GitHubRepo = "NixLockhart/NK-BlogAdmin-WinUI";
        private const string SkippedVersionKey = "skipped_version";

        // ETag cache to avoid counting against rate limit
        private static string? _releasesETag;
        private static List<GitHubRelease>? _releasesCache;

        public string CurrentVersion => AppVersion.Current;

        /// <summary>
        /// 从 GitHub Releases API 获取最新的 Release（不含 prerelease）
        /// </summary>
        public async Task<GitHubRelease?> GetLatestReleaseAsync()
        {
            using var client = CreateHttpClient();
            var url = $"https://api.github.com/repos/{GitHubRepo}/releases/latest";
            var response = await client.GetAsync(url);
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<GitHubRelease>(json);
        }

        /// <summary>
        /// 从 GitHub Releases API 获取所有 Release（带 ETag 缓存）
        /// </summary>
        public async Task<List<GitHubRelease>> GetAllReleasesAsync()
        {
            using var client = CreateHttpClient();
            var url = $"https://api.github.com/repos/{GitHubRepo}/releases";

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            if (_releasesETag != null)
                request.Headers.IfNoneMatch.Add(new System.Net.Http.Headers.EntityTagHeaderValue(_releasesETag));

            var response = await client.SendAsync(request);

            if (response.StatusCode == System.Net.HttpStatusCode.NotModified && _releasesCache != null)
                return _releasesCache;

            response.EnsureSuccessStatusCode();

            // Cache ETag for future conditional requests
            if (response.Headers.ETag != null)
                _releasesETag = response.Headers.ETag.Tag;

            var json = await response.Content.ReadAsStringAsync();
            _releasesCache = JsonConvert.DeserializeObject<List<GitHubRelease>>(json) ?? new List<GitHubRelease>();
            return _releasesCache;
        }

        /// <summary>
        /// 检查是否有可用更新（先用 /latest 轻量检查，有更新时再拉完整列表）
        /// </summary>
        public async Task<UpdateCheckResult?> CheckForUpdateAsync()
        {
            // Step 1: Lightweight check via /releases/latest (single API call)
            var latest = await GetLatestReleaseAsync();
            if (latest == null) return null;

            var latestVersion = latest.TagName;
            var hasUpdate = VersionHelper.IsNewer(latestVersion, CurrentVersion);

            if (!hasUpdate)
            {
                return new UpdateCheckResult
                {
                    HasUpdate = false,
                    LatestVersion = latestVersion,
                    AllReleases = new List<GitHubRelease> { latest }
                };
            }

            // Step 2: Has update — fetch all releases for changelog (with ETag cache)
            List<GitHubRelease> stableReleases;
            try
            {
                var allReleases = await GetAllReleasesAsync();
                stableReleases = allReleases
                    .Where(r => !r.Prerelease)
                    .OrderByDescending(r => r.TagName, new VersionComparer())
                    .ToList();
            }
            catch
            {
                // If fetching all releases fails (rate limit), fall back to latest only
                stableReleases = new List<GitHubRelease> { latest };
            }

            var newReleases = stableReleases
                .Where(r => VersionHelper.IsNewer(r.TagName, CurrentVersion))
                .ToList();

            var isMajor = newReleases.Any(r => r.IsMajor);

            var exeAsset = latest.Assets?.FirstOrDefault(a =>
                a.Name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase));

            return new UpdateCheckResult
            {
                HasUpdate = true,
                IsMajorUpdate = isMajor,
                LatestVersion = latestVersion,
                DownloadUrl = exeAsset?.DownloadUrl,
                ReleasePageUrl = latest.HtmlUrl,
                DownloadSize = exeAsset?.Size ?? 0,
                NewReleases = newReleases,
                AllReleases = stableReleases
            };
        }

        /// <summary>
        /// 下载安装包到临时目录，带进度回调
        /// </summary>
        public async Task<string> DownloadUpdateAsync(string downloadUrl, IProgress<double> progress, CancellationToken ct)
        {
            using var client = CreateHttpClient();

            using var response = await client.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead, ct);
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength ?? -1;
            var tempDir = Path.GetTempPath();
            var fileName = $"BlogManager_Setup_{DateTime.Now:yyyyMMddHHmmss}.exe";
            var filePath = Path.Combine(tempDir, fileName);

            using var contentStream = await response.Content.ReadAsStreamAsync();
            using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);

            var buffer = new byte[81920]; // 80KB buffer
            long bytesRead = 0;
            int read;

            while ((read = await contentStream.ReadAsync(buffer, 0, buffer.Length, ct)) > 0)
            {
                await fileStream.WriteAsync(buffer, 0, read, ct);
                bytesRead += read;

                if (totalBytes > 0)
                {
                    progress.Report((double)bytesRead / totalBytes * 100);
                }
            }

            progress.Report(100);
            return filePath;
        }

        /// <summary>
        /// 启动安装器
        /// </summary>
        public Task LaunchInstallerAsync(string installerPath)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = installerPath,
                UseShellExecute = true
            });
            return Task.CompletedTask;
        }

        /// <summary>
        /// 获取跳过的版本
        /// </summary>
        public string? GetSkippedVersion()
        {
            return SettingsHelper.GetString(SkippedVersionKey);
        }

        /// <summary>
        /// 设置跳过的版本
        /// </summary>
        public void SetSkippedVersion(string version)
        {
            SettingsHelper.SetValue(SkippedVersionKey, version);
        }

        private HttpClient CreateHttpClient()
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
            client.DefaultRequestHeaders.UserAgent.Add(
                new ProductInfoHeaderValue("BlogManager", CurrentVersion));

            return client;
        }

        /// <summary>
        /// 版本号排序比较器
        /// </summary>
        private class VersionComparer : IComparer<string>
        {
            public int Compare(string? x, string? y)
            {
                if (x == null && y == null) return 0;
                if (x == null) return -1;
                if (y == null) return 1;
                return VersionHelper.Compare(x, y);
            }
        }
    }
}
