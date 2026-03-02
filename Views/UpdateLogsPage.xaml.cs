using Blog_Manager.Models;
using Blog_Manager.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Threading.Tasks;

namespace Blog_Manager.Views
{
    public sealed partial class UpdateLogsPage : Page
    {
        public UpdateLogsViewModel ViewModel { get; }

        public UpdateLogsPage()
        {
            this.InitializeComponent();
            ViewModel = new UpdateLogsViewModel();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // Ensure XamlRoot is available before loading
            await Task.Yield();

            // Load data asynchronously
            try
            {
                await LoadUpdateLogsAsync();
            }
            catch (Exception ex)
            {
                // Log error but don't crash
                System.Diagnostics.Debug.WriteLine($"UpdateLogsPage: Failed to load data - {ex.Message}");
            }
        }

        private async System.Threading.Tasks.Task LoadUpdateLogsAsync()
        {
            try
            {
                await ViewModel.LoadUpdateLogsAsync();
            }
            catch (Exception ex)
            {
                App.ShowError(ex.Message);
            }
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadUpdateLogsAsync();
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is long updateLogId)
            {
                // 导航到编辑器页面（编辑模式）
                Frame.Navigate(typeof(UpdateLogEditorPage), updateLogId);
            }
        }

        private async void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is long updateLogId)
            {
                var confirmDialog = new ContentDialog
                {
                    Title = "确认删除",
                    Content = "确定要删除这条更新日志吗？此操作不可恢复！",
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
                        await ViewModel.DeleteUpdateLogAsync(updateLogId);
                        await LoadUpdateLogsAsync();
                        App.ShowSuccess("更新日志已删除");
                    }
                    catch (Exception ex)
                    {
                        App.ShowError(ex.Message);
                    }
                }
            }
        }

        // 公共方法供 MainWindow 调用
        public async void OnRefreshClick()
        {
            await LoadUpdateLogsAsync();
        }

        public void OnCreateClick()
        {
            // 导航到编辑器页面（新建模式）
            Frame.Navigate(typeof(UpdateLogEditorPage));
        }
    }
}
