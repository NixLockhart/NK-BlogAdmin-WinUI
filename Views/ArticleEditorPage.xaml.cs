using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Blog_Manager.ViewModels;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.Web.WebView2.Core;
using Windows.Storage.Pickers;
using Windows.Storage;
using WinRT.Interop;

namespace Blog_Manager.Views
{
    public sealed partial class ArticleEditorPage : Page
    {
        public ArticleEditorViewModel ViewModel { get; }
        private bool _isWebViewInitialized = false;
        private bool _isNavigationComplete = false;
        private bool _isSyncingScroll = false;

        /// <summary>
        /// 是否为只读模式（查看已删除文章或只读查看）
        /// </summary>
        public bool IsReadOnlyMode { get; private set; } = false;

        public ArticleEditorPage()
        {
            this.InitializeComponent();
            ViewModel = new ArticleEditorViewModel();
            InitializeWebView();
        }

        private async void InitializeWebView()
        {
            try
            {
                await PreviewWebView.EnsureCoreWebView2Async();

                // 启用开发者工具（用于调试）
                PreviewWebView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = true;
                PreviewWebView.CoreWebView2.Settings.AreDevToolsEnabled = true;

                // 监听导航完成事件
                PreviewWebView.NavigationCompleted += PreviewWebView_NavigationCompleted;

                // 标记WebView已初始化
                _isWebViewInitialized = true;

                // Subscribe to content changes for preview updates
                ViewModel.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(ViewModel.PreviewHtml))
                    {
                        // 完整页面更新（首次加载）
                        UpdatePreviewFull();
                    }
                    else if (e.PropertyName == nameof(ViewModel.PreviewContentHtml))
                    {
                        // 增量内容更新
                        UpdatePreviewIncremental();
                    }
                };

                // 初始化完成后立即更新预览
                UpdatePreviewFull();
            }
            catch (Exception ex)
            {
                ShowError($"预览初始化失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 编辑器滚动事件处理 - 同步到预览（XAML 中直接绑定）
        /// </summary>
        private async void EditorScrollViewer_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
        {
            if (_isSyncingScroll || !_isNavigationComplete)
                return;

            try
            {
                _isSyncingScroll = true;

                // 计算滚动百分比
                var scrollableHeight = EditorScrollViewer.ScrollableHeight;
                if (scrollableHeight > 0)
                {
                    var scrollPercentage = EditorScrollViewer.VerticalOffset / scrollableHeight;
                    // 调用 JavaScript 同步滚动
                    var script = $"syncScroll({scrollPercentage.ToString(System.Globalization.CultureInfo.InvariantCulture)});";
                    await PreviewWebView.ExecuteScriptAsync(script);
                }
            }
            catch { }
            finally
            {
                _isSyncingScroll = false;
            }
        }

        /// <summary>
        /// WebView 导航完成事件
        /// </summary>
        private void PreviewWebView_NavigationCompleted(WebView2 sender, CoreWebView2NavigationCompletedEventArgs args)
        {
            _isNavigationComplete = args.IsSuccess;
        }

        /// <summary>
        /// 完整页面更新（首次加载或需要重置时）
        /// </summary>
        private void UpdatePreviewFull()
        {
            try
            {
                // 确保WebView已初始化
                if (_isWebViewInitialized && !string.IsNullOrEmpty(ViewModel.PreviewHtml))
                {
                    _isNavigationComplete = false;
                    PreviewWebView.NavigateToString(ViewModel.PreviewHtml);
                }
            }
            catch { }
        }

        /// <summary>
        /// 增量内容更新（通过 JavaScript 只更新内容部分）
        /// </summary>
        private async void UpdatePreviewIncremental()
        {
            try
            {
                if (!_isWebViewInitialized || !_isNavigationComplete)
                    return;

                var contentHtml = ViewModel.PreviewContentHtml;
                if (string.IsNullOrEmpty(contentHtml))
                    return;

                // 将 HTML 内容转义为 JavaScript 字符串
                var escapedHtml = EscapeJavaScriptString(contentHtml);
                var script = $"updateContent(\"{escapedHtml}\");";
                await PreviewWebView.ExecuteScriptAsync(script);
            }
            catch { }
        }

        /// <summary>
        /// 转义字符串以安全地嵌入 JavaScript
        /// </summary>
        private string EscapeJavaScriptString(string input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            return input
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\r\n", "\\n")
                .Replace("\n", "\\n")
                .Replace("\r", "\\n")
                .Replace("\t", "\\t");
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            LoadingPanel.Visibility = Visibility.Visible;
            ContentPanel.Visibility = Visibility.Collapsed;
            ErrorInfoBar.Visibility = Visibility.Collapsed;

            try
            {
                // Load categories
                await ViewModel.LoadCategoriesAsync();

                // Load article if editing or viewing
                if (e.Parameter is long parameter)
                {
                    // 负数ID表示只读模式
                    bool isViewOnlyMode = parameter < 0;
                    long articleId = Math.Abs(parameter);

                    await ViewModel.LoadArticleAsync(articleId);

                    // 检查是否为只读模式或已删除文章
                    if (isViewOnlyMode || ViewModel.Status == 0)
                    {
                        // 设置为只读模式
                        IsReadOnlyMode = true;
                        ErrorInfoBar.Title = isViewOnlyMode ? "查看模式" : "文章已删除";
                        ErrorInfoBar.Message = "只能查看和复制内容，无法编辑或保存";
                        ErrorInfoBar.Severity = InfoBarSeverity.Informational;
                        ErrorInfoBar.Visibility = Visibility.Visible;

                        // 禁用所有编辑控件
                        TitleTextBox.IsReadOnly = true;
                        MarkdownEditor.IsReadOnly = true;

                        // 禁用所有快捷工具栏按钮
                        DisableMarkdownToolbar();

                        // 通知 MainWindow 刷新工具栏（移除保存/发布按钮）
                        GetMainWindow()?.RefreshEditorToolbar();
                    }
                }

                ContentPanel.Visibility = Visibility.Visible;

                // 加载完成后重置预览状态并手动触发预览更新
                await System.Threading.Tasks.Task.Delay(100); // 短暂延迟确保UI渲染完成
                ViewModel.ResetPreviewState();
                UpdatePreviewFull();
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
            finally
            {
                LoadingPanel.Visibility = Visibility.Collapsed;
            }
        }

        private void DisableMarkdownToolbar()
        {
            // 禁用所有快捷工具栏按钮
            InsertH1Button.IsEnabled = false;
            InsertH2Button.IsEnabled = false;
            InsertH3Button.IsEnabled = false;
            InsertBoldButton.IsEnabled = false;
            InsertItalicButton.IsEnabled = false;
            InsertLinkButton.IsEnabled = false;
            UploadImageButton.IsEnabled = false;
            InsertCodeButton.IsEnabled = false;
            InsertQuoteButton.IsEnabled = false;
            InsertListButton.IsEnabled = false;
            InsertMathButton.IsEnabled = false;

            // 通知 MainWindow 禁用工具栏按钮（保存/发布按钮现在在 MainWindow 中）
            // 只读模式下的文章编辑器不需要禁用 MainWindow 工具栏，因为工具栏会根据页面状态自动更新
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            // 停止自动保存定时器
            ViewModel.StopAutoSaveTimer();

            // 清理事件订阅
            if (PreviewWebView?.CoreWebView2 != null)
            {
                PreviewWebView.NavigationCompleted -= PreviewWebView_NavigationCompleted;
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            OnBackClick();
        }

        /// <summary>
        /// 返回按钮点击 - 供 MainWindow 调用
        /// </summary>
        public void OnBackClick()
        {
            _ = HandleBackNavigationAsync();
        }

        /// <summary>
        /// 开发者工具按钮点击 - 供 MainWindow 调用
        /// </summary>
        public void OnDevToolsClick()
        {
            if (PreviewWebView?.CoreWebView2 != null)
            {
                PreviewWebView.CoreWebView2.OpenDevToolsWindow();
            }
        }

        /// <summary>
        /// 保存草稿按钮点击 - 供 MainWindow 调用
        /// </summary>
        public void OnSaveDraftClick()
        {
            SaveDraftButton_Click(null, null);
        }

        /// <summary>
        /// 发布按钮点击 - 供 MainWindow 调用
        /// </summary>
        public void OnPublishClick()
        {
            PublishButton_Click(null, null);
        }

        /// <summary>
        /// 获取 MainWindow 实例
        /// </summary>
        private MainWindow GetMainWindow()
        {
            var window = (Application.Current as App)?.Window;
            return window?.Content as MainWindow;
        }

        /// <summary>
        /// 导航返回到父页面
        /// </summary>
        private void NavigateBack()
        {
            var mainWindow = GetMainWindow();
            if (mainWindow != null)
            {
                mainWindow.NavigateBackFromSubPage();
            }
            else
            {
                // 回退方案
                Frame.GoBack();
            }
        }

        /// <summary>
        /// 处理返回导航，检查是否有未保存的更改
        /// </summary>
        private async System.Threading.Tasks.Task HandleBackNavigationAsync()
        {
            // 如果没有未保存的更改，直接返回
            if (!ViewModel.HasUnsavedChanges)
            {
                NavigateBack();
                return;
            }

            // 显示确认对话框
            var dialog = new ContentDialog
            {
                Title = "未保存的更改",
                Content = "您有未保存的更改。是否保存？\n\n选择\"保存\"将保存当前修改\n选择\"不保存\"将回滚到打开文章时的状态\n选择\"取消\"将继续编辑",
                PrimaryButtonText = "保存",
                SecondaryButtonText = "不保存",
                CloseButtonText = "取消",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();

            switch (result)
            {
                case ContentDialogResult.Primary:
                    // 用户选择保存
                    await SaveAndGoBack();
                    break;

                case ContentDialogResult.Secondary:
                    // 用户选择不保存（回滚）
                    await RollbackAndGoBack();
                    break;

                case ContentDialogResult.None:
                    // 用户取消，继续编辑
                    break;
            }
        }

        /// <summary>
        /// 保存当前修改并返回
        /// </summary>
        private async System.Threading.Tasks.Task SaveAndGoBack()
        {
            LoadingPanel.Visibility = Visibility.Visible;
            ContentPanel.Visibility = Visibility.Collapsed;

            try
            {
                await ViewModel.SaveArticleAsync();
                NavigateBack();
            }
            catch (Exception ex)
            {
                ShowError($"保存失败: {ex.Message}");
                ContentPanel.Visibility = Visibility.Visible;
            }
            finally
            {
                LoadingPanel.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// 回滚到原始状态并返回
        /// </summary>
        private async System.Threading.Tasks.Task RollbackAndGoBack()
        {
            LoadingPanel.Visibility = Visibility.Visible;
            ContentPanel.Visibility = Visibility.Collapsed;

            try
            {
                await ViewModel.RollbackToOriginalAsync();
                NavigateBack();
            }
            catch (Exception ex)
            {
                ShowError($"回滚失败: {ex.Message}");
                ContentPanel.Visibility = Visibility.Visible;
            }
            finally
            {
                LoadingPanel.Visibility = Visibility.Collapsed;
            }
        }

        private void DevToolsButton_Click(object sender, RoutedEventArgs e)
        {
            // 打开WebView2开发者工具
            if (PreviewWebView?.CoreWebView2 != null)
            {
                PreviewWebView.CoreWebView2.OpenDevToolsWindow();
            }
        }

        private async void SaveDraftButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ViewModel.Content))
            {
                ShowError("请输入文章内容");
                return;
            }

            ViewModel.Status = 2; // Draft
            await ShowPublishDialogAndSave();
        }

        private async void PublishButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ViewModel.Content))
            {
                ShowError("请输入文章内容");
                return;
            }

            ViewModel.Status = 1; // Published
            await ShowPublishDialogAndSave();
        }

        private async System.Threading.Tasks.Task ShowPublishDialogAndSave()
        {
            // Create publish configuration dialog
            var dialog = new PublishConfigDialog(ViewModel);
            dialog.XamlRoot = this.XamlRoot;

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                await SaveArticle();
            }
        }

        private async System.Threading.Tasks.Task SaveArticle()
        {
            LoadingPanel.Visibility = Visibility.Visible;
            ContentPanel.Visibility = Visibility.Collapsed;
            ErrorInfoBar.Visibility = Visibility.Collapsed;

            try
            {
                await ViewModel.SaveArticleAsync();

                var dialog = new ContentDialog
                {
                    Title = "保存成功",
                    Content = ViewModel.Status == 1 ? "文章已发布" : "文章已保存为草稿",
                    CloseButtonText = "确定",
                    XamlRoot = this.XamlRoot
                };

                await dialog.ShowAsync();
                NavigateBack();
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
                ContentPanel.Visibility = Visibility.Visible;
            }
            finally
            {
                LoadingPanel.Visibility = Visibility.Collapsed;
            }
        }

        private void ShowError(string message)
        {
            ErrorInfoBar.Message = message;
            ErrorInfoBar.Visibility = Visibility.Visible;
        }

        // Markdown Toolbar Helpers
        private void InsertMarkdown(string before, string after = "", string placeholder = "")
        {
            var selectionStart = MarkdownEditor.SelectionStart;
            var selectionLength = MarkdownEditor.SelectionLength;
            var text = ViewModel.Content;

            string selectedText = selectionLength > 0
                ? text.Substring(selectionStart, selectionLength)
                : placeholder;

            var newText = text.Substring(0, selectionStart) +
                         before + selectedText + after +
                         text.Substring(selectionStart + selectionLength);

            ViewModel.Content = newText;

            // Set cursor position
            MarkdownEditor.SelectionStart = selectionStart + before.Length;
            MarkdownEditor.SelectionLength = selectedText.Length;
            MarkdownEditor.Focus(FocusState.Programmatic);
        }

        private void InsertH1_Click(object sender, RoutedEventArgs e)
        {
            InsertMarkdown("# ", "", "标题 1");
        }

        private void InsertH2_Click(object sender, RoutedEventArgs e)
        {
            InsertMarkdown("## ", "", "标题 2");
        }

        private void InsertH3_Click(object sender, RoutedEventArgs e)
        {
            InsertMarkdown("### ", "", "标题 3");
        }

        private void InsertBold_Click(object sender, RoutedEventArgs e)
        {
            InsertMarkdown("**", "**", "粗体文本");
        }

        private void InsertItalic_Click(object sender, RoutedEventArgs e)
        {
            InsertMarkdown("*", "*", "斜体文本");
        }

        private void InsertLink_Click(object sender, RoutedEventArgs e)
        {
            InsertMarkdown("[", "](https://)", "链接文本");
        }

        private async void UploadImage_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 如果是新建文章，需要先保存获取文章ID
                if (!ViewModel.ArticleId.HasValue)
                {
                    ShowError("请先保存文章为草稿后再上传图片");
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
                    UploadImageButton.IsEnabled = false;
                    await UploadArticleImage(file);
                }
            }
            catch (Exception ex)
            {
                ShowError($"上传图片失败: {ex.Message}");
            }
            finally
            {
                UploadImageButton.IsEnabled = true;
            }
        }

        private async System.Threading.Tasks.Task UploadArticleImage(StorageFile file)
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
                var token = app?.AuthService.CurrentToken;
                if (!string.IsNullOrEmpty(token))
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                }

                var uploadUrl = Helpers.AppContext.GetApiUrl($"/api/admin/files/articles/{ViewModel.ArticleId}/images");
                var response = await client.PostAsync(uploadUrl, content);

                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"上传失败: {response.StatusCode}");
                }

                // Parse response to get URL
                var result = Newtonsoft.Json.JsonConvert.DeserializeObject<ApiResponse<string>>(responseContent);
                if (result?.Code == 200 && !string.IsNullOrEmpty(result.Data))
                {
                    // 将相对路径转换为完整URL（根据当前连接的服务器）
                    var fullUrl = Helpers.AppContext.GetFileUrl(result.Data);
                    // Insert markdown image syntax at cursor position
                    InsertMarkdown($"![图片描述]({fullUrl})");
                }
                else
                {
                    throw new Exception("上传失败: " + (result?.Message ?? "未知错误"));
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"上传图片时出错: {ex.Message}");
            }
        }

        private void InsertCode_Click(object sender, RoutedEventArgs e)
        {
            InsertMarkdown("```\n", "\n```", "代码");
        }

        private void InsertQuote_Click(object sender, RoutedEventArgs e)
        {
            InsertMarkdown("> ", "", "引用文本");
        }

        private void InsertList_Click(object sender, RoutedEventArgs e)
        {
            InsertMarkdown("- ", "", "列表项");
        }

        private void InsertMath_Click(object sender, RoutedEventArgs e)
        {
            InsertMarkdown("$", "$", "x^2 + y^2 = z^2");
        }

        private class ApiResponse<T>
        {
            [Newtonsoft.Json.JsonProperty("code")]
            public int Code { get; set; }

            [Newtonsoft.Json.JsonProperty("message")]
            public string? Message { get; set; }

            [Newtonsoft.Json.JsonProperty("data")]
            public T? Data { get; set; }
        }
    }
}
