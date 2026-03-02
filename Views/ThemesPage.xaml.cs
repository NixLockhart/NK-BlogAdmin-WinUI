using Blog_Manager.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace Blog_Manager.Views
{
    public sealed partial class ThemesPage : Page
    {
        public ThemesViewModel ViewModel { get; private set; }

        public ThemesPage()
        {
            this.InitializeComponent();
            ViewModel = new ThemesViewModel();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            await LoadThemesAsync();
        }

        private async Task LoadThemesAsync()
        {
            try
            {
                await ViewModel.LoadThemesAsync();
            }
            catch (Exception ex)
            {
                await ShowErrorDialogAsync("加载主题列表失败", ex.Message);
            }
        }

        private async void AddThemeButton_Click(object sender, RoutedEventArgs e)
        {
            await ShowAddThemeDialogAsync();
        }

        // 公共方法供 MainWindow 调用
        public async void OnAddThemeClick()
        {
            await ShowAddThemeDialogAsync();
        }

        private async Task ShowAddThemeDialogAsync()
        {
            // 创建输入表单
            StackPanel stackPanel = new StackPanel { Spacing = 12, Width = 480 };

            TextBox nameTextBox = new TextBox
            {
                Header = "主题名称 *",
                PlaceholderText = "例如：活力主题"
            };
            stackPanel.Children.Add(nameTextBox);

            TextBox descriptionTextBox = new TextBox
            {
                Header = "主题描述",
                PlaceholderText = "简要描述主题特点",
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                Height = 80
            };
            stackPanel.Children.Add(descriptionTextBox);

            TextBox authorTextBox = new TextBox
            {
                Header = "作者",
                PlaceholderText = "主题作者名称"
            };
            stackPanel.Children.Add(authorTextBox);

            TextBox versionTextBox = new TextBox
            {
                Header = "版本号",
                PlaceholderText = "例如：1.0.0"
            };
            stackPanel.Children.Add(versionTextBox);

            NumberBox displayOrderBox = new NumberBox
            {
                Header = "显示顺序",
                Value = 0,
                Minimum = 0,
                SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Compact
            };
            stackPanel.Children.Add(displayOrderBox);

            // 文件选择按钮
            Button lightCssButton = new Button
            {
                Content = "选择亮色主题文件 (light.css) *",
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Margin = new Thickness(0, 8, 0, 0)
            };
            TextBlock lightCssPath = new TextBlock
            {
                Text = "未选择文件",
                Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Gray),
                Margin = new Thickness(0, 4, 0, 0)
            };
            stackPanel.Children.Add(lightCssButton);
            stackPanel.Children.Add(lightCssPath);

            Button darkCssButton = new Button
            {
                Content = "选择暗色主题文件 (dark.css) *",
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Margin = new Thickness(0, 8, 0, 0)
            };
            TextBlock darkCssPath = new TextBlock
            {
                Text = "未选择文件",
                Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Gray),
                Margin = new Thickness(0, 4, 0, 0)
            };
            stackPanel.Children.Add(darkCssButton);
            stackPanel.Children.Add(darkCssPath);

            Button coverButton = new Button
            {
                Content = "选择封面图片（可选）",
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Margin = new Thickness(0, 8, 0, 0)
            };
            TextBlock coverPath = new TextBlock
            {
                Text = "未选择文件",
                Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Gray),
                Margin = new Thickness(0, 4, 0, 0)
            };
            stackPanel.Children.Add(coverButton);
            stackPanel.Children.Add(coverPath);

            string? lightCssFilePath = null;
            string? darkCssFilePath = null;
            string? coverFilePath = null;

            // 文件选择事件
            lightCssButton.Click += async (s, args) =>
            {
                var file = await PickFileAsync(".css");
                if (file != null)
                {
                    lightCssFilePath = file.Path;
                    lightCssPath.Text = file.Name;
                    lightCssPath.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Green);
                }
            };

            darkCssButton.Click += async (s, args) =>
            {
                var file = await PickFileAsync(".css");
                if (file != null)
                {
                    darkCssFilePath = file.Path;
                    darkCssPath.Text = file.Name;
                    darkCssPath.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Green);
                }
            };

            coverButton.Click += async (s, args) =>
            {
                var file = await PickFileAsync(".jpg", ".jpeg", ".png");
                if (file != null)
                {
                    coverFilePath = file.Path;
                    coverPath.Text = file.Name;
                    coverPath.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Green);
                }
            };

            ContentDialog dialog = new ContentDialog
            {
                Title = "添加主题",
                Content = new ScrollViewer
                {
                    Content = stackPanel,
                    MaxHeight = 500
                },
                PrimaryButtonText = "上传",
                CloseButtonText = "取消",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                // 验证必填项
                if (string.IsNullOrWhiteSpace(nameTextBox.Text))
                {
                    await ShowErrorDialogAsync("验证失败", "请输入主题名称");
                    return;
                }

                if (string.IsNullOrEmpty(lightCssFilePath))
                {
                    await ShowErrorDialogAsync("验证失败", "请选择亮色主题文件");
                    return;
                }

                if (string.IsNullOrEmpty(darkCssFilePath))
                {
                    await ShowErrorDialogAsync("验证失败", "请选择暗色主题文件");
                    return;
                }

                try
                {
                    await UploadThemeAsync(
                        nameTextBox.Text,
                        descriptionTextBox.Text,
                        authorTextBox.Text,
                        versionTextBox.Text,
                        (int)displayOrderBox.Value,
                        lightCssFilePath,
                        darkCssFilePath,
                        coverFilePath
                    );

                    await LoadThemesAsync();
                    await ShowSuccessDialogAsync("上传成功", "主题已成功上传到服务器");
                }
                catch (Exception ex)
                {
                    await ShowErrorDialogAsync("上传失败", ex.Message);
                }
            }
        }

        private async Task<Windows.Storage.StorageFile?> PickFileAsync(params string[] extensions)
        {
            var picker = new FileOpenPicker();
            var app = (Application.Current as App) ?? throw new InvalidOperationException("App instance not found");
            var window = app.Window ?? throw new InvalidOperationException("Window not found");
            var hwnd = WindowNative.GetWindowHandle(window);
            InitializeWithWindow.Initialize(picker, hwnd);

            picker.ViewMode = PickerViewMode.List;
            picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;

            foreach (var ext in extensions)
            {
                picker.FileTypeFilter.Add(ext);
            }

            return await picker.PickSingleFileAsync();
        }

        private async Task UploadThemeAsync(
            string name,
            string? description,
            string? author,
            string? version,
            int displayOrder,
            string lightCssPath,
            string darkCssPath,
            string? coverImagePath)
        {
            var app = (Application.Current as App) ?? throw new InvalidOperationException("App instance not found");
            var themeApi = app.ApiServiceFactory.CreateThemeApi();

            using var lightStream = File.OpenRead(lightCssPath);
            using var darkStream = File.OpenRead(darkCssPath);

            var lightPart = new Refit.StreamPart(lightStream, Path.GetFileName(lightCssPath), "text/css");
            var darkPart = new Refit.StreamPart(darkStream, Path.GetFileName(darkCssPath), "text/css");

            var response = await themeApi.CreateThemeAsync(
                name,
                description,
                author,
                version,
                displayOrder,
                coverImagePath,
                lightPart,
                darkPart
            );

            if (response.Code != 200)
            {
                throw new Exception(response.Message ?? "上传主题失败");
            }
        }

        private async void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button button && button.Tag is long themeId)
                {
                    await ViewModel.ToggleThemeApplicationAsync(themeId, true);
                    await LoadThemesAsync();
                    await ShowSuccessDialogAsync("主题已应用", "主题已成功应用到博客");
                }
            }
            catch (Exception ex)
            {
                await ShowErrorDialogAsync("应用主题失败", ex.Message);
            }
        }

        private async void CancelApplyButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button button && button.Tag is long themeId)
                {
                    await ViewModel.ToggleThemeApplicationAsync(themeId, false);
                    await LoadThemesAsync();
                    await ShowSuccessDialogAsync("已取消应用", "已切换回默认主题");
                }
            }
            catch (Exception ex)
            {
                await ShowErrorDialogAsync("取消应用失败", ex.Message);
            }
        }

        private void MoreButton_Click(object sender, RoutedEventArgs e)
        {
            // 右上角更多按钮，Flyout会自动弹出
        }

        private async void DeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is MenuFlyoutItem item && item.Tag is long themeId)
                {
                    var theme = ViewModel.Themes.FirstOrDefault(t => t.Id == themeId);
                    if (theme == null) return;

                    // 确认对话框
                    ContentDialog confirmDialog = new ContentDialog
                    {
                        Title = "确认删除",
                        Content = $"确定要删除主题 \"{theme.Name}\" 吗？\n\n此操作不可恢复。",
                        PrimaryButtonText = "删除",
                        CloseButtonText = "取消",
                        DefaultButton = ContentDialogButton.Close,
                        XamlRoot = this.XamlRoot
                    };

                    var result = await confirmDialog.ShowAsync();
                    if (result == ContentDialogResult.Primary)
                    {
                        await ViewModel.DeleteThemeAsync(themeId);
                        await LoadThemesAsync();
                        await ShowSuccessDialogAsync("删除成功", "主题已成功删除");
                    }
                }
            }
            catch (Exception ex)
            {
                await ShowErrorDialogAsync("删除主题失败", ex.Message);
            }
        }

        private Task ShowSuccessDialogAsync(string title, string message)
        {
            App.ShowSuccess(message);
            return Task.CompletedTask;
        }

        private Task ShowErrorDialogAsync(string title, string message)
        {
            App.ShowError(message);
            return Task.CompletedTask;
        }

        private Task ShowInfoDialogAsync(string title, string message)
        {
            App.ShowInfo(message);
            return Task.CompletedTask;
        }

        private async void ExportMenuItem_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is MenuFlyoutItem item && item.Tag is long themeId)
                {
                    var theme = ViewModel.Themes.FirstOrDefault(t => t.Id == themeId);
                    if (theme == null) return;

                    // 选择保存位置
                    var folderPicker = new FolderPicker();
                    var app = (Application.Current as App) ?? throw new InvalidOperationException("App instance not found");
                    var window = app.Window ?? throw new InvalidOperationException("Window not found");
                    var hwnd = WindowNative.GetWindowHandle(window);
                    InitializeWithWindow.Initialize(folderPicker, hwnd);

                    folderPicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                    folderPicker.FileTypeFilter.Add("*");

                    var folder = await folderPicker.PickSingleFolderAsync();
                    if (folder == null) return;

                    // 调用API导出主题
                    var themeApi = app.ApiServiceFactory.CreateThemeApi();
                    var response = await themeApi.ExportThemeAsync(themeId);

                    if (response.IsSuccessStatusCode)
                    {
                        var zipData = await response.Content.ReadAsByteArrayAsync();
                        var filename = $"{theme.Name}-{theme.Slug}.zip";
                        var filePath = Path.Combine(folder.Path, filename);

                        await File.WriteAllBytesAsync(filePath, zipData);
                        await ShowSuccessDialogAsync("导出成功", $"主题已成功导出到：\n{filePath}");
                    }
                    else
                    {
                        await ShowErrorDialogAsync("导出失败", "无法从服务器获取主题文件");
                    }
                }
            }
            catch (Exception ex)
            {
                await ShowErrorDialogAsync("导出主题失败", ex.Message);
            }
        }

        private async void EditMenuItem_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is MenuFlyoutItem item && item.Tag is long themeId)
                {
                    var theme = ViewModel.Themes.FirstOrDefault(t => t.Id == themeId);
                    if (theme == null) return;

                    await ShowEditThemeDialogAsync(theme);
                }
            }
            catch (Exception ex)
            {
                await ShowErrorDialogAsync("打开编辑对话框失败", ex.Message);
            }
        }

        private async void EditButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button button && button.Tag is long themeId)
                {
                    var theme = ViewModel.Themes.FirstOrDefault(t => t.Id == themeId);
                    if (theme == null) return;

                    await ShowEditThemeDialogAsync(theme);
                }
            }
            catch (Exception ex)
            {
                await ShowErrorDialogAsync("打开编辑对话框失败", ex.Message);
            }
        }

        private async Task ShowEditThemeDialogAsync(Models.Theme theme)
        {
            // 创建输入表单
            StackPanel stackPanel = new StackPanel { Spacing = 12, Width = 480 };

            TextBox nameTextBox = new TextBox
            {
                Header = "主题名称 *",
                Text = theme.Name
            };
            stackPanel.Children.Add(nameTextBox);

            TextBox descriptionTextBox = new TextBox
            {
                Header = "主题描述",
                Text = theme.Description ?? "",
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                Height = 80
            };
            stackPanel.Children.Add(descriptionTextBox);

            TextBox authorTextBox = new TextBox
            {
                Header = "作者",
                Text = theme.Author ?? ""
            };
            stackPanel.Children.Add(authorTextBox);

            TextBox versionTextBox = new TextBox
            {
                Header = "版本号",
                Text = theme.Version ?? ""
            };
            stackPanel.Children.Add(versionTextBox);

            NumberBox displayOrderBox = new NumberBox
            {
                Header = "显示顺序",
                Value = theme.DisplayOrder,
                Minimum = 0,
                SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Compact
            };
            stackPanel.Children.Add(displayOrderBox);

            // 主题文件选择
            TextBlock filesHeader = new TextBlock
            {
                Text = "主题文件（可选，不选则保持原文件）",
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                Margin = new Thickness(0, 8, 0, 4)
            };
            stackPanel.Children.Add(filesHeader);

            // 文件选择按钮
            Button lightCssButton = new Button
            {
                Content = "选择亮色主题文件 (light.css)",
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Margin = new Thickness(0, 4, 0, 0)
            };
            TextBlock lightCssPath = new TextBlock
            {
                Text = "未选择文件",
                Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Gray),
                Margin = new Thickness(0, 4, 0, 0)
            };
            stackPanel.Children.Add(lightCssButton);
            stackPanel.Children.Add(lightCssPath);

            Button darkCssButton = new Button
            {
                Content = "选择暗色主题文件 (dark.css)",
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Margin = new Thickness(0, 8, 0, 0)
            };
            TextBlock darkCssPath = new TextBlock
            {
                Text = "未选择文件",
                Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Gray),
                Margin = new Thickness(0, 4, 0, 0)
            };
            stackPanel.Children.Add(darkCssButton);
            stackPanel.Children.Add(darkCssPath);

            Button coverButton = new Button
            {
                Content = "选择封面图片（可选）",
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Margin = new Thickness(0, 8, 0, 0)
            };
            TextBlock coverPath = new TextBlock
            {
                Text = "未选择文件",
                Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Gray),
                Margin = new Thickness(0, 4, 0, 0)
            };
            stackPanel.Children.Add(coverButton);
            stackPanel.Children.Add(coverPath);

            string? lightCssFilePath = null;
            string? darkCssFilePath = null;
            string? coverFilePath = null;

            // 文件选择事件
            lightCssButton.Click += async (s, args) =>
            {
                var file = await PickFileAsync(".css");
                if (file != null)
                {
                    lightCssFilePath = file.Path;
                    lightCssPath.Text = file.Name;
                    lightCssPath.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Green);
                }
            };

            darkCssButton.Click += async (s, args) =>
            {
                var file = await PickFileAsync(".css");
                if (file != null)
                {
                    darkCssFilePath = file.Path;
                    darkCssPath.Text = file.Name;
                    darkCssPath.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Green);
                }
            };

            coverButton.Click += async (s, args) =>
            {
                var file = await PickFileAsync(".jpg", ".jpeg", ".png");
                if (file != null)
                {
                    coverFilePath = file.Path;
                    coverPath.Text = file.Name;
                    coverPath.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Green);
                }
            };

            ContentDialog dialog = new ContentDialog
            {
                Title = "编辑主题",
                Content = new ScrollViewer
                {
                    Content = stackPanel,
                    MaxHeight = 500
                },
                PrimaryButtonText = "保存",
                CloseButtonText = "取消",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                // 验证必填项
                if (string.IsNullOrWhiteSpace(nameTextBox.Text))
                {
                    await ShowErrorDialogAsync("验证失败", "请输入主题名称");
                    return;
                }

                try
                {
                    var app = (Application.Current as App) ?? throw new InvalidOperationException("App instance not found");
                    var themeApi = app.ApiServiceFactory.CreateThemeApi();

                    // 1. 更新基本信息
                    var updateRequest = new Models.ThemeUpdateRequest
                    {
                        Name = nameTextBox.Text,
                        Description = descriptionTextBox.Text,
                        Author = authorTextBox.Text,
                        Version = versionTextBox.Text,
                        DisplayOrder = (int)displayOrderBox.Value,
                        CoverImage = coverFilePath != null ? await ConvertImageToBase64(coverFilePath) : null
                    };

                    var updateResponse = await themeApi.UpdateThemeAsync(theme.Id, updateRequest);
                    if (updateResponse.Code != 200)
                    {
                        throw new Exception(updateResponse.Message ?? "更新主题信息失败");
                    }

                    // 2. 如果选择了新的CSS文件，更新文件
                    if (!string.IsNullOrEmpty(lightCssFilePath) || !string.IsNullOrEmpty(darkCssFilePath))
                    {
                        Refit.StreamPart? lightPart = null;
                        Refit.StreamPart? darkPart = null;

                        if (!string.IsNullOrEmpty(lightCssFilePath))
                        {
                            var lightStream = File.OpenRead(lightCssFilePath);
                            lightPart = new Refit.StreamPart(lightStream, Path.GetFileName(lightCssFilePath), "text/css");
                        }

                        if (!string.IsNullOrEmpty(darkCssFilePath))
                        {
                            var darkStream = File.OpenRead(darkCssFilePath);
                            darkPart = new Refit.StreamPart(darkStream, Path.GetFileName(darkCssFilePath), "text/css");
                        }

                        var filesResponse = await themeApi.UpdateThemeFilesAsync(theme.Id, lightPart, darkPart);

                        // 关闭流
                        if (lightPart != null) lightPart.Value.Dispose();
                        if (darkPart != null) darkPart.Value.Dispose();

                        if (filesResponse.Code != 200)
                        {
                            throw new Exception(filesResponse.Message ?? "更新主题文件失败");
                        }
                    }

                    await LoadThemesAsync();
                    await ShowSuccessDialogAsync("更新成功", "主题已成功更新");
                }
                catch (Exception ex)
                {
                    await ShowErrorDialogAsync("更新失败", ex.Message);
                }
            }
        }

        private async Task<string?> ConvertImageToBase64(string imagePath)
        {
            try
            {
                var bytes = await File.ReadAllBytesAsync(imagePath);
                var base64 = Convert.ToBase64String(bytes);
                var extension = Path.GetExtension(imagePath).ToLower();
                var mimeType = extension switch
                {
                    ".jpg" or ".jpeg" => "image/jpeg",
                    ".png" => "image/png",
                    _ => "image/jpeg"
                };
                return $"data:{mimeType};base64,{base64}";
            }
            catch
            {
                return null;
            }
        }
    }
}
