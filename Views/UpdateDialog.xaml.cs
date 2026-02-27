using Blog_Manager.Models;
using Blog_Manager.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Linq;
using System.Text;
using System.Threading;

namespace Blog_Manager.Views
{
    public sealed partial class UpdateDialog : ContentDialog
    {
        private readonly UpdateCheckResult _updateResult;
        private readonly UpdateService _updateService;
        private CancellationTokenSource? _downloadCts;
        private bool _isDownloading;

        public UpdateDialog(UpdateCheckResult updateResult)
        {
            this.InitializeComponent();
            _updateResult = updateResult;
            _updateService = ((App)Application.Current).UpdateService;

            CurrentVersionText.Text = _updateService.CurrentVersion;
            LatestVersionText.Text = _updateResult.LatestVersion;

            // 构建更新日志文本
            var sb = new StringBuilder();
            foreach (var release in _updateResult.NewReleases)
            {
                if (sb.Length > 0) sb.AppendLine();
                sb.AppendLine($"── {release.TagName} {release.Name} ──");
                if (!string.IsNullOrWhiteSpace(release.Body))
                {
                    sb.AppendLine(release.Body.Trim());
                }
            }
            ChangelogText.Text = sb.Length > 0 ? sb.ToString() : "暂无更新说明";

            // 显示下载大小
            if (_updateResult.DownloadSize > 0)
            {
                var sizeMb = _updateResult.DownloadSize / (1024.0 * 1024.0);
                DownloadSizeText.Text = $"安装包大小: {sizeMb:F1} MB";
            }

            // 没有下载链接时禁用更新按钮
            if (string.IsNullOrEmpty(_updateResult.DownloadUrl))
            {
                this.IsPrimaryButtonEnabled = false;
                DownloadSizeText.Text = "未找到安装包，请在浏览器中手动下载";
                if (!string.IsNullOrEmpty(_updateResult.ReleasePageUrl))
                {
                    OpenInBrowserBtn.Visibility = Visibility.Visible;
                }
            }
        }

        private async void PrimaryButton_Click(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            // 阻止对话框关闭，开始下载
            args.Cancel = true;

            if (_isDownloading) return;
            _isDownloading = true;

            // 禁用按钮
            this.IsPrimaryButtonEnabled = false;
            this.IsSecondaryButtonEnabled = false;

            // 显示下载进度
            DownloadPanel.Visibility = Visibility.Visible;
            ErrorInfoBar.IsOpen = false;

            _downloadCts = new CancellationTokenSource();
            var progress = new Progress<double>(percent =>
            {
                DispatcherQueue.TryEnqueue(() =>
                {
                    DownloadProgress.Value = percent;
                    DownloadPercentText.Text = $"{percent:F0}%";
                });
            });

            try
            {
                DownloadStatusText.Text = "正在下载更新...";
                var msixPath = await _updateService.DownloadUpdateAsync(
                    _updateResult.DownloadUrl!, progress, _downloadCts.Token);

                DownloadStatusText.Text = "下载完成，正在启动安装...";
                await _updateService.LaunchInstallerAsync(msixPath);

                // 安装器启动后关闭对话框
                this.Hide();
            }
            catch (OperationCanceledException)
            {
                DownloadStatusText.Text = "下载已取消";
            }
            catch (Exception ex)
            {
                ErrorInfoBar.Message = ex.Message;
                ErrorInfoBar.IsOpen = true;
                DownloadStatusText.Text = "下载失败";

                if (!string.IsNullOrEmpty(_updateResult.ReleasePageUrl))
                {
                    OpenInBrowserBtn.Visibility = Visibility.Visible;
                }
            }
            finally
            {
                _isDownloading = false;
                this.IsPrimaryButtonEnabled = true;
                this.IsSecondaryButtonEnabled = true;
                _downloadCts?.Dispose();
                _downloadCts = null;
            }
        }

        private void SecondaryButton_Click(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            // 跳过此版本
            _updateService.SetSkippedVersion(_updateResult.LatestVersion);
        }

        private async void OpenInBrowser_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(_updateResult.ReleasePageUrl))
            {
                await Windows.System.Launcher.LaunchUriAsync(new Uri(_updateResult.ReleasePageUrl));
            }
        }
    }
}
