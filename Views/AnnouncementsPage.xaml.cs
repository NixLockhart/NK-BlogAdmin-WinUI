using Blog_Manager.Models;
using Blog_Manager.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Blog_Manager.Views
{
    public sealed partial class AnnouncementsPage : Page
    {
        public AnnouncementsViewModel ViewModel { get; }

        public AnnouncementsPage()
        {
            this.InitializeComponent();
            ViewModel = new AnnouncementsViewModel();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // Ensure XamlRoot is available before loading
            await Task.Yield();

            // Load data asynchronously
            try
            {
                await LoadAnnouncementsAsync();
            }
            catch (Exception ex)
            {
                // Log error but don't crash
                System.Diagnostics.Debug.WriteLine($"AnnouncementsPage: Failed to load data - {ex.Message}");
            }
        }

        private async System.Threading.Tasks.Task LoadAnnouncementsAsync()
        {
            try
            {
                await ViewModel.LoadAnnouncementsAsync();
            }
            catch (Exception ex)
            {
                App.ShowError(ex.Message);
            }
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadAnnouncementsAsync();
        }

        private async void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = CreateAnnouncementDialog(null);
            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                try
                {
                    var announcement = GetAnnouncementFromDialog(dialog);
                    await ViewModel.CreateAnnouncementAsync(announcement);
                    await LoadAnnouncementsAsync();

                    App.ShowSuccess("公告已创建");
                }
                catch (Exception ex)
                {
                    App.ShowError(ex.Message);
                }
            }
        }

        private async void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is long announcementId)
            {
                var announcement = ViewModel.Announcements.FirstOrDefault(a => a.Id == announcementId);
                if (announcement != null)
                {
                    var dialog = CreateAnnouncementDialog(announcement);
                    var result = await dialog.ShowAsync();

                    if (result == ContentDialogResult.Primary)
                    {
                        try
                        {
                            var updatedAnnouncement = GetAnnouncementFromDialog(dialog);
                            await ViewModel.UpdateAnnouncementAsync(announcementId, updatedAnnouncement);
                            await LoadAnnouncementsAsync();

                            App.ShowSuccess("公告已更新");
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
            if (sender is Button button && button.Tag is long announcementId)
            {
                var confirmDialog = new ContentDialog
                {
                    Title = "确认删除",
                    Content = "确定要删除这条公告吗？此操作不可恢复！",
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
                        await ViewModel.DeleteAnnouncementAsync(announcementId);
                        await LoadAnnouncementsAsync();

                        App.ShowSuccess("公告已删除");
                    }
                    catch (Exception ex)
                    {
                        App.ShowError(ex.Message);
                    }
                }
            }
        }

        private ContentDialog CreateAnnouncementDialog(Announcement? announcement)
        {
            var titleBox = new TextBox { Header = "标题", PlaceholderText = "输入公告标题", Text = announcement?.Title ?? "" };
            var contentBox = new TextBox { Header = "内容", PlaceholderText = "输入公告内容", AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, MinHeight = 120, Text = announcement?.Content ?? "" };
            var startTimePicker = new DatePicker { Header = "开始时间" };
            var endTimePicker = new DatePicker { Header = "结束时间" };
            var enabledCheckBox = new CheckBox { Content = "启用公告", IsChecked = announcement?.IsEnabled ?? true };

            if (announcement?.StartTime.HasValue == true)
            {
                startTimePicker.Date = announcement.StartTime.Value;
            }
            if (announcement?.EndTime.HasValue == true)
            {
                endTimePicker.Date = announcement.EndTime.Value;
            }

            var stackPanel = new StackPanel { Spacing = 12, Width = 480 };
            stackPanel.Children.Add(titleBox);
            stackPanel.Children.Add(contentBox);
            stackPanel.Children.Add(startTimePicker);
            stackPanel.Children.Add(endTimePicker);
            stackPanel.Children.Add(enabledCheckBox);

            var scrollViewer = new ScrollViewer
            {
                Content = stackPanel,
                MaxHeight = 500
            };

            return new ContentDialog
            {
                Title = announcement == null ? "新建公告" : "编辑公告",
                Content = scrollViewer,
                PrimaryButtonText = announcement == null ? "创建" : "保存",
                CloseButtonText = "取消",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot
            };
        }

        private Announcement GetAnnouncementFromDialog(ContentDialog dialog)
        {
            var scrollViewer = dialog.Content as ScrollViewer;
            var stackPanel = scrollViewer?.Content as StackPanel;
            if (stackPanel == null) throw new InvalidOperationException("Invalid dialog content");

            var titleBox = stackPanel.Children[0] as TextBox;
            var contentBox = stackPanel.Children[1] as TextBox;
            var startTimePicker = stackPanel.Children[2] as DatePicker;
            var endTimePicker = stackPanel.Children[3] as DatePicker;
            var enabledCheckBox = stackPanel.Children[4] as CheckBox;

            return new Announcement
            {
                Title = titleBox?.Text ?? "",
                Content = contentBox?.Text ?? "",
                StartTime = startTimePicker?.Date.DateTime,
                EndTime = endTimePicker?.Date.DateTime,
                Enabled = enabledCheckBox?.IsChecked == true ? 1 : 0
            };
        }

        // 公共方法供 MainWindow 调用
        public async void OnRefreshClick()
        {
            await LoadAnnouncementsAsync();
        }

        public void OnCreateClick()
        {
            CreateButton_Click(null, null);
        }
    }
}
