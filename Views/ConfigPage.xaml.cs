using Blog_Manager.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Blog_Manager.Views
{
    public sealed partial class ConfigPage : Page
    {
        public ConfigViewModel ViewModel { get; }

        public ConfigPage()
        {
            this.InitializeComponent();
            ViewModel = new ConfigViewModel();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // Ensure XamlRoot is available before loading
            await Task.Yield();

            // Load data asynchronously
            try
            {
                await LoadConfigsAsync();
            }
            catch (Exception ex)
            {
                // Log error but don't crash
                System.Diagnostics.Debug.WriteLine($"ConfigPage: Failed to load data - {ex.Message}");
            }
        }

        private async System.Threading.Tasks.Task LoadConfigsAsync()
        {
            try
            {
                await ViewModel.LoadConfigsAsync();
            }
            catch (Exception ex)
            {
                App.ShowError(ex.Message);
            }
        }

        // 公共方法供 MainWindow 调用
        public async void OnRefreshClick()
        {
            await LoadConfigsAsync();
        }

        public async void OnCreateConfigClick()
        {
            var dialog = new CreateConfigDialog();

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary && dialog.Result != null)
            {
                try
                {
                    await ViewModel.CreateConfigAsync(dialog.Result);
                    await LoadConfigsAsync();
                    App.ShowSuccess("配置创建成功");
                }
                catch (Exception ex)
                {
                    App.ShowError(ex.Message);
                }
            }
        }

        private async void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string configKey)
            {
                // Find the TextBox in the same container
                var border = FindParent<Border>(button);
                if (border != null)
                {
                    var textBox = FindChild<TextBox>(border, "ValueTextBox");
                    if (textBox != null)
                    {
                        try
                        {
                            await ViewModel.UpdateConfigAsync(configKey, textBox.Text);
                            await LoadConfigsAsync();
                            App.ShowSuccess("配置已更新");
                        }
                        catch (Exception ex)
                        {
                            App.ShowError(ex.Message);
                        }
                    }
                }
            }
        }

        private async void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string configKey)
            {
                var confirmDialog = new ContentDialog
                {
                    Title = "确认删除",
                    Content = $"确定要删除配置项 '{configKey}' 吗？此操作不可恢复！",
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
                        await ViewModel.DeleteConfigAsync(configKey);
                        await LoadConfigsAsync();
                        App.ShowSuccess("配置已删除");
                    }
                    catch (Exception ex)
                    {
                        App.ShowError(ex.Message);
                    }
                }
            }
        }

        // Helper method to find parent element of a specific type
        private T? FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            var parent = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(child);

            if (parent == null)
                return null;

            if (parent is T typedParent)
                return typedParent;

            return FindParent<T>(parent);
        }

        // Helper method to find child element by name
        private T? FindChild<T>(DependencyObject parent, string childName) where T : FrameworkElement
        {
            if (parent == null)
                return null;

            int childCount = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(parent);

            for (int i = 0; i < childCount; i++)
            {
                var child = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(parent, i);

                if (child is T typedChild && typedChild.Name == childName)
                {
                    return typedChild;
                }

                var foundChild = FindChild<T>(child, childName);
                if (foundChild != null)
                    return foundChild;
            }

            return null;
        }
    }
}
