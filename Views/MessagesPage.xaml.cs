using Blog_Manager.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Threading.Tasks;

namespace Blog_Manager.Views
{
    public sealed partial class MessagesPage : Page
    {
        public MessagesViewModel ViewModel { get; }

        public MessagesPage()
        {
            this.InitializeComponent();
            ViewModel = new MessagesViewModel();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // Ensure XamlRoot is available before loading
            await Task.Yield();

            // Load data asynchronously
            try
            {
                await LoadMessagesAsync();
            }
            catch (Exception ex)
            {
                // Log error but don't crash
                System.Diagnostics.Debug.WriteLine($"MessagesPage: Failed to load data - {ex.Message}");
            }
        }

        private async System.Threading.Tasks.Task LoadMessagesAsync()
        {
            try
            {
                await ViewModel.LoadMessagesAsync();
            }
            catch (Exception ex)
            {
                App.ShowError(ex.Message);
            }
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadMessagesAsync();
        }

        // 公共方法供 MainWindow 调用
        public async void OnRefreshClick()
        {
            await LoadMessagesAsync();
        }

        private async void ToggleFriendButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is long messageId)
            {
                try
                {
                    await ViewModel.ToggleFriendLinkAsync(messageId);
                    await LoadMessagesAsync();
                    App.ShowSuccess("友链标记已更新");
                }
                catch (Exception ex)
                {
                    App.ShowError(ex.Message);
                }
            }
        }

        private async void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is long messageId)
            {
                var confirmDialog = new ContentDialog
                {
                    Title = "确认删除",
                    Content = "确定要删除这条留言吗？此操作不可恢复！",
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
                        await ViewModel.DeleteMessageAsync(messageId);
                        await LoadMessagesAsync();
                        App.ShowSuccess("留言已删除");
                    }
                    catch (Exception ex)
                    {
                        App.ShowError(ex.Message);
                    }
                }
            }
        }
    }
}
