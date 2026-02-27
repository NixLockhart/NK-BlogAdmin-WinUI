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

        public string CurrentVersion => AppVersion.Current;

        /// <summary>
        /// 从 GitHub Releases API 获取所有 Release
        /// </summary>
        public async Task<List<GitHubRelease>> GetAllReleasesAsync()
        {
            using var client = CreateHttpClient();
            var url = $"https://api.github.com/repos/{GitHubRepo}/releases";
            var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<List<GitHubRelease>>(json) ?? new List<GitHubRelease>();
        }

        /// <summary>
        /// 检查是否有可用更新
        /// </summary>
        public async Task<UpdateCheckResult?> CheckForUpdateAsync()
        {
            var allReleases = await GetAllReleasesAsync();
            if (allReleases.Count == 0) return null;

            // 过滤掉 prerelease，按版本号降序排序
            var stableReleases = allReleases
                .Where(r => !r.Prerelease)
                .OrderByDescending(r => r.TagName, new VersionComparer())
                .ToList();

            if (stableReleases.Count == 0) return null;

            var latest = stableReleases[0];
            var latestVersion = latest.TagName;
            var hasUpdate = VersionHelper.IsNewer(latestVersion, CurrentVersion);

            if (!hasUpdate)
            {
                return new UpdateCheckResult
                {
                    HasUpdate = false,
                    LatestVersion = latestVersion,
                    AllReleases = stableReleases
                };
            }

            // 筛选比当前版本更新的所有 Release
            var newReleases = stableReleases
                .Where(r => VersionHelper.IsNewer(r.TagName, CurrentVersion))
                .ToList();

            // 任一新 Release 标记了 [MAJOR] 则视为大版本更新
            var isMajor = newReleases.Any(r => r.IsMajor);

            // 从最新 Release 的 assets 中找 .exe 安装包
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
