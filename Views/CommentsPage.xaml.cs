using Blog_Manager.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Threading.Tasks;

namespace Blog_Manager.Views
{
    public sealed partial class CommentsPage : Page
    {
        public CommentsViewModel ViewModel { get; }
        private bool _isInitialized = false;

        public CommentsPage()
        {
            this.InitializeComponent();
            ViewModel = new CommentsViewModel();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // Ensure XamlRoot is available before loading
            await Task.Yield();

            // Load data asynchronously
            try
            {
                await InitializeFiltersAsync();
                await LoadCommentsAsync();
                _isInitialized = true;
            }
            catch (Exception ex)
            {
                // Log error but don't crash
                System.Diagnostics.Debug.WriteLine($"CommentsPage: Failed to load data - {ex.Message}");
            }
        }

        private async System.Threading.Tasks.Task InitializeFiltersAsync()
        {
            // Load articles for the filter dropdown
            await ViewModel.LoadArticlesAsync();

            // Clear and populate the article filter ComboBox
            ArticleFilterComboBox.Items.Clear();

            // Add "All Articles" option
            var allArticlesItem = new ComboBoxItem
            {
                Content = "全部文章",
                Tag = (long?)null
            };
            ArticleFilterComboBox.Items.Add(allArticlesItem);

            // Add each article
            foreach (var article in ViewModel.Articles)
            {
                var item = new ComboBoxItem
                {
                    Content = article.Title,
                    Tag = article.Id
                };
                ArticleFilterComboBox.Items.Add(item);
            }

            // Set initial selections without triggering events
            ArticleFilterComboBox.SelectedIndex = 0; // "全部文章"
            StatusFilterComboBox.SelectedIndex = 0;  // "全部"
            TimeSortComboBox.SelectedIndex = 0;      // "最新优先"
        }

        private async System.Threading.Tasks.Task LoadCommentsAsync()
        {
            try
            {
                await ViewModel.LoadCommentsAsync();
            }
            catch (Exception ex)
            {
                var dialog = new ContentDialog
                {
                    Title = "错误",
                    Content = ex.Message,
                    CloseButtonText = "确定",
                    XamlRoot = this.XamlRoot
                };
                await dialog.ShowAsync();
            }
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadCommentsAsync();
        }

        // 公共方法供 MainWindow 调用
        public async void OnRefreshClick()
        {
            await LoadCommentsAsync();
        }

        private async void ApproveButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is long commentId)
            {
                try
                {
                    await ViewModel.ApproveCommentAsync(commentId);
                    await LoadCommentsAsync();

                    var dialog = new ContentDialog
                    {
                        Title = "成功",
                        Content = "评论已审核通过",
                        CloseButtonText = "确定",
                        XamlRoot = this.XamlRoot
                    };
                    await dialog.ShowAsync();
                }
                catch (Exception ex)
                {
                    var dialog = new ContentDialog
                    {
                        Title = "错误",
                        Content = ex.Message,
                        CloseButtonText = "确定",
                        XamlRoot = this.XamlRoot
                    };
                    await dialog.ShowAsync();
                }
            }
        }

        private async void RejectButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is long commentId)
            {
                try
                {
                    await ViewModel.RejectCommentAsync(commentId);
                    await LoadCommentsAsync();

                    var dialog = new ContentDialog
                    {
                        Title = "成功",
                        Content = "评论已标记为待审核",
                        CloseButtonText = "确定",
                        XamlRoot = this.XamlRoot
                    };
                    await dialog.ShowAsync();
                }
                catch (Exception ex)
                {
                    var dialog = new ContentDialog
                    {
                        Title = "错误",
                        Content = ex.Message,
                        CloseButtonText = "确定",
                        XamlRoot = this.XamlRoot
                    };
                    await dialog.ShowAsync();
                }
            }
        }

        private async void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is long commentId)
            {
                var confirmDialog = new ContentDialog
                {
                    Title = "确认删除",
                    Content = "确定要删除这条评论吗？此操作将级联删除所有子评论，且不可恢复！",
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
                        await ViewModel.DeleteCommentAsync(commentId);
                        await LoadCommentsAsync();

                        var dialog = new ContentDialog
                        {
                            Title = "成功",
                            Content = "评论已删除",
                            CloseButtonText = "确定",
                            XamlRoot = this.XamlRoot
                        };
                        await dialog.ShowAsync();
                    }
                    catch (Exception ex)
                    {
                        var dialog = new ContentDialog
                        {
                            Title = "错误",
                            Content = ex.Message,
                            CloseButtonText = "确定",
                            XamlRoot = this.XamlRoot
                        };
                        await dialog.ShowAsync();
                    }
                }
            }
        }

        private async void ArticleFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitialized)
                return;

            if (ArticleFilterComboBox.SelectedItem is ComboBoxItem item)
            {
                ViewModel.FilterArticleId = item.Tag as long?;
                await LoadCommentsAsync();
            }
        }

        private async void StatusFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitialized)
                return;

            if (StatusFilterComboBox.SelectedItem is ComboBoxItem item && item.Tag is string tagStr)
            {
                if (int.TryParse(tagStr, out int status))
                {
                    ViewModel.FilterStatus = status == 0 ? null : (int?)status;
                    await LoadCommentsAsync();
                }
            }
        }

        private async void TimeSortComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitialized)
                return;

            if (TimeSortComboBox.SelectedItem is ComboBoxItem item && item.Tag is string sortOrder)
            {
                ViewModel.SortOrder = sortOrder;
                await LoadCommentsAsync();
            }
        }
    }
}
