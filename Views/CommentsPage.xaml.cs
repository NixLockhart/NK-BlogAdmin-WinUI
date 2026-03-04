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
                App.ShowError(ex.Message);
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
                    App.ShowSuccess("评论已审核通过");
                }
                catch (Exception ex)
                {
                    App.ShowError(ex.Message);
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
                    App.ShowSuccess("评论已标记为待审核");
                }
                catch (Exception ex)
                {
                    App.ShowError(ex.Message);
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
                        App.ShowSuccess("评论已删除");
                    }
                    catch (Exception ex)
                    {
                        App.ShowError(ex.Message);
                    }
                }
            }
        }

        private async void PermanentDeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is long commentId)
            {
                var confirmDialog = new ContentDialog
                {
                    Title = "永久删除",
                    Content = "确定要永久删除这条评论吗？\n\n此操作不可逆，评论及其所有子评论将被彻底删除。",
                    PrimaryButtonText = "永久删除",
                    CloseButtonText = "取消",
                    DefaultButton = ContentDialogButton.Close,
                    XamlRoot = this.XamlRoot
                };

                var result = await confirmDialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    try
                    {
                        await ViewModel.PermanentlyDeleteCommentAsync(commentId);
                        await LoadCommentsAsync();
                        App.ShowSuccess("评论已永久删除");
                    }
                    catch (Exception ex)
                    {
                        App.ShowError($"永久删除失败: {ex.Message}");
                    }
                }
            }
        }

        private async void RestoreButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is long commentId)
            {
                try
                {
                    await ViewModel.RestoreCommentAsync(commentId);
                    await LoadCommentsAsync();
                    App.ShowSuccess("评论已恢复");
                }
                catch (Exception ex)
                {
                    App.ShowError($"恢复失败: {ex.Message}");
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
                    // Tag: 0=全部(null), 1=已审核, 2=待审核, 3=已删除(status=0)
                    if (status == 0)
                        ViewModel.FilterStatus = null;
                    else if (status == 3)
                        ViewModel.FilterStatus = 0;
                    else
                        ViewModel.FilterStatus = status;
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
