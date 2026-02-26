using Blog_Manager.Models;
using Blog_Manager.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace Blog_Manager.Views
{
    public sealed partial class WidgetsPage : Page
    {
        public WidgetsViewModel ViewModel { get; }

        public WidgetsPage()
        {
            this.InitializeComponent();
            ViewModel = new WidgetsViewModel();
        }

        /// <summary>
        /// 获取窗口句柄
        /// </summary>
        private IntPtr GetWindowHandle()
        {
            // 从主进程获取主窗口句柄
            return System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            await Task.Yield();
            await LoadWidgetsAsync();
        }

        private async Task LoadWidgetsAsync()
        {
            try
            {
                await ViewModel.LoadWidgetsAsync();
            }
            catch (Exception ex)
            {
                await ShowErrorDialog("加载失败", ex.Message);
            }
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadWidgetsAsync();
        }

        private async void AddWidgetButton_Click(object sender, RoutedEventArgs e)
        {
            await ShowAddWidgetDialog();
        }

        // 公共方法供 MainWindow 调用
        public async void OnRefreshClick()
        {
            await LoadWidgetsAsync();
        }

        public async void OnAddWidgetClick()
        {
            await ShowAddWidgetDialog();
        }

        private async void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is long widgetId)
            {
                await ShowEditWidgetDialog(widgetId);
            }
        }

        private async void ToggleApplyButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is long widgetId)
            {
                try
                {
                    var widget = ViewModel.Widgets.FirstOrDefault(w => w.Id == widgetId);
                    if (widget == null) return;

                    bool newState = !widget.IsApplied;
                    await ViewModel.ToggleWidgetApplicationAsync(widgetId, newState);
                    await LoadWidgetsAsync();

                    await ShowSuccessDialog(newState ? "已应用小工具" : "已取消应用小工具");
                }
                catch (Exception ex)
                {
                    await ShowErrorDialog("操作失败", ex.Message);
                }
            }
        }

        private void MoreButton_Click(object sender, RoutedEventArgs e)
        {
            // Flyout会自动显示
        }

        private async void ExportMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem item && item.Tag is long widgetId)
            {
                try
                {
                    var widget = ViewModel.Widgets.FirstOrDefault(w => w.Id == widgetId);
                    if (widget == null) return;

                    // 获取代码
                    string code = await ViewModel.ExportWidgetAsync(widgetId);

                    // 文件保存选择器
                    var savePicker = new FileSavePicker();
                    var hwnd = GetWindowHandle();
                    WinRT.Interop.InitializeWithWindow.Initialize(savePicker, hwnd);

                    savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                    savePicker.FileTypeChoices.Add("HTML文件", new[] { ".html" });
                    savePicker.SuggestedFileName = $"{widget.Name}.html";

                    StorageFile file = await savePicker.PickSaveFileAsync();
                    if (file != null)
                    {
                        await FileIO.WriteTextAsync(file, code);
                        await ShowSuccessDialog("导出成功", $"小工具已导出到：{file.Path}");
                    }
                }
                catch (Exception ex)
                {
                    await ShowErrorDialog("导出失败", ex.Message);
                }
            }
        }

        private async void DeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem item && item.Tag is long widgetId)
            {
                var widget = ViewModel.Widgets.FirstOrDefault(w => w.Id == widgetId);
                if (widget == null) return;

                if (widget.IsSystem)
                {
                    await ShowErrorDialog("无法删除", "系统自带小工具不可删除");
                    return;
                }

                var confirmDialog = new ContentDialog
                {
                    Title = "确认删除",
                    Content = $"确定要删除小工具「{widget.Name}」吗？此操作不可恢复！",
                    PrimaryButtonText = "删除",
                    CloseButtonText = "取消",
                    DefaultButton = ContentDialogButton.Close,
                    XamlRoot = this.XamlRoot
                };

                var result = await confirmDialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    try
                    {
                        await ViewModel.DeleteWidgetAsync(widgetId);
                        await LoadWidgetsAsync();
                        await ShowSuccessDialog("删除成功");
                    }
                    catch (Exception ex)
                    {
                        await ShowErrorDialog("删除失败", ex.Message);
                    }
                }
            }
        }

        private async Task ShowAddWidgetDialog()
        {
            var dialog = new ContentDialog
            {
                Title = "添加小工具",
                PrimaryButtonText = "创建",
                CloseButtonText = "取消",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot
            };

            var panel = new StackPanel { Spacing = 12 };

            var nameBox = new TextBox
            {
                Header = "小工具名称",
                PlaceholderText = "请输入小工具名称"
            };
            panel.Children.Add(nameBox);

            var codeBox = new TextBox
            {
                Header = "HTML代码",
                PlaceholderText = "请输入小工具HTML代码（容器宽度固定为300px）",
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                Height = 200,
                Text = GetDefaultWidgetTemplate()
            };
            panel.Children.Add(codeBox);

            var coverButton = new Button
            {
                Content = "选择封面图片（可选）"
            };
            string? coverBase64 = null;
            coverButton.Click += async (s, e) =>
            {
                coverBase64 = await PickCoverImageAsync();
                if (coverBase64 != null)
                {
                    coverButton.Content = "封面已选择 ✓";
                }
            };
            panel.Children.Add(coverButton);

            dialog.Content = panel;

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                if (string.IsNullOrWhiteSpace(nameBox.Text))
                {
                    await ShowErrorDialog("输入错误", "请输入小工具名称");
                    return;
                }

                if (string.IsNullOrWhiteSpace(codeBox.Text))
                {
                    await ShowErrorDialog("输入错误", "请输入小工具代码");
                    return;
                }

                try
                {
                    var request = new WidgetCreateRequest
                    {
                        Name = nameBox.Text,
                        Code = codeBox.Text,
                        CoverImage = coverBase64
                    };

                    await ViewModel.CreateWidgetAsync(request);
                    await LoadWidgetsAsync();
                    await ShowSuccessDialog("创建成功");
                }
                catch (Exception ex)
                {
                    await ShowErrorDialog("创建失败", ex.Message);
                }
            }
        }

        private async Task ShowEditWidgetDialog(long widgetId)
        {
            try
            {
                var widgetCode = await ViewModel.GetWidgetCodeAsync(widgetId);

                var dialog = new ContentDialog
                {
                    Title = "编辑小工具",
                    PrimaryButtonText = "保存",
                    CloseButtonText = "取消",
                    DefaultButton = ContentDialogButton.Primary,
                    XamlRoot = this.XamlRoot
                };

                var panel = new StackPanel { Spacing = 12 };

                var nameBox = new TextBox
                {
                    Header = "小工具名称",
                    Text = widgetCode.Name
                };
                panel.Children.Add(nameBox);

                var codeBox = new TextBox
                {
                    Header = "HTML代码",
                    AcceptsReturn = true,
                    TextWrapping = TextWrapping.Wrap,
                    Height = 200,
                    Text = widgetCode.Code
                };
                panel.Children.Add(codeBox);

                var coverButton = new Button
                {
                    Content = "更换封面图片（可选）"
                };
                string? coverBase64 = null;
                coverButton.Click += async (s, e) =>
                {
                    coverBase64 = await PickCoverImageAsync();
                    if (coverBase64 != null)
                    {
                        coverButton.Content = "新封面已选择 ✓";
                    }
                };
                panel.Children.Add(coverButton);

                dialog.Content = panel;

                var result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    if (string.IsNullOrWhiteSpace(nameBox.Text))
                    {
                        await ShowErrorDialog("输入错误", "请输入小工具名称");
                        return;
                    }

                    if (string.IsNullOrWhiteSpace(codeBox.Text))
                    {
                        await ShowErrorDialog("输入错误", "请输入小工具代码");
                        return;
                    }

                    try
                    {
                        var request = new WidgetUpdateRequest
                        {
                            Name = nameBox.Text,
                            Code = codeBox.Text,
                            CoverImage = coverBase64
                        };

                        await ViewModel.UpdateWidgetAsync(widgetId, request);
                        await LoadWidgetsAsync();
                        await ShowSuccessDialog("保存成功");
                    }
                    catch (Exception ex)
                    {
                        await ShowErrorDialog("保存失败", ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                await ShowErrorDialog("加载失败", ex.Message);
            }
        }

        private async Task<string?> PickCoverImageAsync()
        {
            try
            {
                var picker = new FileOpenPicker();
                var hwnd = GetWindowHandle();
                WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

                picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
                picker.FileTypeFilter.Add(".jpg");
                picker.FileTypeFilter.Add(".jpeg");
                picker.FileTypeFilter.Add(".png");
                picker.FileTypeFilter.Add(".gif");

                StorageFile file = await picker.PickSingleFileAsync();
                if (file != null)
                {
                    var bytes = await File.ReadAllBytesAsync(file.Path);
                    var base64 = Convert.ToBase64String(bytes);
                    var mimeType = file.ContentType;
                    return $"data:{mimeType};base64,{base64}";
                }
            }
            catch (Exception ex)
            {
                await ShowErrorDialog("选择图片失败", ex.Message);
            }

            return null;
        }

        private string GetDefaultWidgetTemplate()
        {
            return @"<div class=""widget-container"" style=""width: 100%; max-width: 300px;"">
  <style>
    .my-widget {
      background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
      border-radius: 12px;
      padding: 20px;
      color: #fff;
      text-align: center;
    }
  </style>

  <div class=""my-widget"">
    <h3>我的小工具</h3>
    <p>在这里编写你的小工具内容</p>
  </div>

  <script>
    // 可以在这里编写JavaScript代码
  </script>
</div>";
        }

        private async Task ShowSuccessDialog(string title, string? message = null)
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = message ?? "",
                CloseButtonText = "确定",
                XamlRoot = this.XamlRoot
            };
            await dialog.ShowAsync();
        }

        private async Task ShowErrorDialog(string title, string message)
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = message,
                CloseButtonText = "确定",
                XamlRoot = this.XamlRoot
            };
            await dialog.ShowAsync();
        }
    }
}
