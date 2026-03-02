using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Blog_Manager.ViewModels;
using Blog_Manager.Models;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Windows.Storage.Pickers;
using Windows.Storage;
using WinRT.Interop;

namespace Blog_Manager.Views
{
    public sealed partial class PublishConfigDialog : ContentDialog
    {
        private readonly ArticleEditorViewModel _viewModel;
        private string _coverImageRelativePath = string.Empty; // 存储相对路径

        public PublishConfigDialog(ArticleEditorViewModel viewModel)
        {
            this.InitializeComponent();
            _viewModel = viewModel;

            // 绑定分类列表
            CategoryComboBox.ItemsSource = _viewModel.Categories;
            CategoryComboBox.DisplayMemberPath = "Name";
            CategoryComboBox.SelectedValuePath = "Id";

            // 设置当前值
            CategoryComboBox.SelectedValue = _viewModel.CategoryId;
            SummaryTextBox.Text = _viewModel.Summary;

            // 初始化封面
            InitializeCoverImage();

            // 验证必填项
            this.PrimaryButtonClick += ValidateAndSave;
        }

        private void InitializeCoverImage()
        {
            var coverImage = _viewModel.CoverImage;
            if (!string.IsNullOrEmpty(coverImage))
            {
                // 统一转换为相对路径（去掉 /files/ 前缀、查询参数等）
                // 与后端 ImageUrlService.toRelativePath 保持一致
                _coverImageRelativePath = Helpers.AppContext.ToRelativePath(coverImage);

                // 更新UI
                UpdateCoverPreview(_coverImageRelativePath);
                UpdateUploadButtonState(true);
            }
        }

        private void ValidateAndSave(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            // 验证分类
            if (CategoryComboBox.SelectedValue == null)
            {
                args.Cancel = true;
                App.ShowWarning("请选择文章分类");
                return;
            }

            // 保存数据到ViewModel（存储相对路径，而不是完整URL）
            _viewModel.CategoryId = (long?)CategoryComboBox.SelectedValue;
            _viewModel.CoverImage = _coverImageRelativePath; // 保存相对路径
            _viewModel.Summary = SummaryTextBox.Text?.Trim() ?? string.Empty;
        }

        private void UpdateCoverPreview(string relativePath)
        {
            if (!string.IsNullOrEmpty(relativePath))
            {
                try
                {
                    // 转换为完整URL用于预览
                    var fullUrl = relativePath.StartsWith("http")
                        ? relativePath
                        : Helpers.AppContext.GetFileUrl(relativePath);

                    // 添加时间戳参数破坏缓存，确保重新上传后能看到新图片
                    var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    var urlWithTimestamp = fullUrl.Contains("?")
                        ? $"{fullUrl}&t={timestamp}"
                        : $"{fullUrl}?t={timestamp}";

                    // 确保在UI线程上更新
                    this.DispatcherQueue.TryEnqueue(() =>
                    {
                        CoverImageTextBox.Text = fullUrl;
                        CoverPreviewImage.Source = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(new Uri(urlWithTimestamp));
                        CoverPreviewBorder.Visibility = Visibility.Visible;
                    });
                }
                catch (Exception ex)
                {
                    this.DispatcherQueue.TryEnqueue(() =>
                    {
                        CoverPreviewBorder.Visibility = Visibility.Collapsed;
                        CoverImageTextBox.Text = $"封面加载失败: {ex.Message}";
                    });
                }
            }
            else
            {
                this.DispatcherQueue.TryEnqueue(() =>
                {
                    CoverPreviewBorder.Visibility = Visibility.Collapsed;
                    CoverImageTextBox.Text = string.Empty;
                });
            }
        }

        private void UpdateUploadButtonState(bool hasImage)
        {
            // 确保在UI线程上更新
            this.DispatcherQueue.TryEnqueue(() =>
            {
                if (hasImage)
                {
                    UploadButtonText.Text = "重新上传";
                    UploadIcon.Symbol = Symbol.Refresh;
                }
                else
                {
                    UploadButtonText.Text = "上传封面";
                    UploadIcon.Symbol = Symbol.Upload;
                }
            });
        }

        private async void UploadCoverButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 检查是否有文章ID（如果没有，说明自动保存还没触发）
                if (!_viewModel.ArticleId.HasValue)
                {
                    App.ShowInfo("检测到文章尚未自动保存，请稍等片刻后再试，或先输入一些内容以触发自动保存");
                    return;
                }

                // Create file picker
                var picker = new FileOpenPicker();
                var hwnd = WindowNative.GetWindowHandle((Application.Current as App)?.Window);
                InitializeWithWindow.Initialize(picker, hwnd);

                picker.ViewMode = PickerViewMode.Thumbnail;
                picker.FileTypeFilter.Add(".jpg");
                picker.FileTypeFilter.Add(".jpeg");
                picker.FileTypeFilter.Add(".png");

                var file = await picker.PickSingleFileAsync();
                if (file != null)
                {
                    // 显示进度条
                    UploadProgressBar.Visibility = Visibility.Visible;
                    UploadCoverButton.IsEnabled = false;

                    await UploadCover(file);

                    // 隐藏进度条
                    UploadProgressBar.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                // 隐藏进度条
                UploadProgressBar.Visibility = Visibility.Collapsed;
                App.ShowError($"上传封面失败: {ex.Message}");
            }
            finally
            {
                UploadCoverButton.IsEnabled = true;
            }
        }

        private async System.Threading.Tasks.Task UploadCover(StorageFile file)
        {
            try
            {
                using var stream = await file.OpenStreamForReadAsync();
                using var content = new MultipartFormDataContent();
                using var streamContent = new StreamContent(stream);

                streamContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
                content.Add(streamContent, "file", file.Name);

                var app = Application.Current as App;
                using var client = new HttpClient();

                // 设置超时时间为30秒，防止长时间挂起
                client.Timeout = TimeSpan.FromSeconds(30);

                var token = app?.AuthService.CurrentToken;
                if (!string.IsNullOrEmpty(token))
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                }

                // 使用新的 API，按文章 ID 命名
                var uploadUrl = Helpers.AppContext.GetApiUrl($"/api/admin/files/covers/{_viewModel.ArticleId.Value}");
                var response = await client.PostAsync(uploadUrl, content);

                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"HTTP {response.StatusCode}: {responseContent}");
                }

                // Parse response to get relative path - 使用不区分大小写的选项
                var options = new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                var result = System.Text.Json.JsonSerializer.Deserialize<ApiResponse<string>>(responseContent, options);

                if (result?.Code == 200 && !string.IsNullOrEmpty(result.Data))
                {
                    // 保存相对路径
                    _coverImageRelativePath = result.Data;

                    // 立即更新 ViewModel，确保封面信息被保存
                    _viewModel.CoverImage = _coverImageRelativePath;

                    // 刷新全局缓存版本，确保列表中的封面也会更新
                    Models.Article.RefreshGlobalCache();

                    // 更新预览（使用完整URL）
                    UpdateCoverPreview(_coverImageRelativePath);

                    // 更新按钮状态为"重新上传"
                    UpdateUploadButtonState(true);
                }
                else
                {
                    throw new Exception($"上传失败: Code={result?.Code}, Message={result?.Message}, Data={result?.Data}, Response={responseContent}");
                }
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                throw new Exception("上传超时，请检查网络连接或稍后重试", ex);
            }
            catch (TaskCanceledException)
            {
                throw new Exception("上传超时（30秒），请尝试上传更小的图片或检查网络连接");
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"网络请求失败: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new Exception($"上传封面时出错: {ex.Message}", ex);
            }
        }

        private class ApiResponse<T>
        {
            public int Code { get; set; }
            public string? Message { get; set; }
            public T? Data { get; set; }
        }
    }
}
