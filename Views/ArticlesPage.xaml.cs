using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Blog_Manager.ViewModels;
using System;
using System.Threading;

namespace Blog_Manager.Views
{
    public sealed partial class ArticlesPage : Page
    {
        public ArticlesViewModel ViewModel { get; }
        private bool _isInitialized = false;
        private CancellationTokenSource? _searchDebounce;

        public ArticlesPage()
        {
            this.InitializeComponent();
            ViewModel = new ArticlesViewModel();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            try
            {
                await System.Threading.Tasks.Task.Yield();
                await InitializeFiltersAsync();
                await LoadArticlesAsync();
                _isInitialized = true;
            }
            catch (Exception ex)
            {
                ErrorInfoBar.Message = $"页面加载失败: {ex.Message}";
                ErrorInfoBar.Visibility = Visibility.Visible;
                LoadingPanel.Visibility = Visibility.Collapsed;
            }
        }

        private async System.Threading.Tasks.Task InitializeFiltersAsync()
        {
            // 加载分类列表
            await ViewModel.LoadCategoriesAsync();

            // 填充分类筛选 ComboBox
            CategoryFilterComboBox.Items.Clear();
            var allItem = new ComboBoxItem { Content = "全部分类", Tag = (long?)null };
            CategoryFilterComboBox.Items.Add(allItem);

            foreach (var category in ViewModel.Categories)
            {
                var item = new ComboBoxItem
                {
                    Content = category.Name,
                    Tag = (long?)category.Id
                };
                CategoryFilterComboBox.Items.Add(item);
            }

            // 设置初始选中项
            StatusFilterComboBox.SelectedIndex = 0;
            CategoryFilterComboBox.SelectedIndex = 0;
        }

        private async System.Threading.Tasks.Task LoadArticlesAsync()
        {
            LoadingPanel.Visibility = Visibility.Visible;
            ContentPanel.Visibility = Visibility.Collapsed;
            ErrorInfoBar.Visibility = Visibility.Collapsed;

            try
            {
                await ViewModel.LoadArticlesAsync();
                ContentPanel.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                ErrorInfoBar.Message = $"加载失败: {ex.Message}";
                ErrorInfoBar.Visibility = Visibility.Visible;
            }
            finally
            {
                LoadingPanel.Visibility = Visibility.Collapsed;
            }
        }

        private void CreateArticleButton_Click(object sender, RoutedEventArgs e)
        {
            // 导航到文章编辑页（新建模式）
            Frame.Navigate(typeof(ArticleEditorPage));
        }

        // 公共方法供 MainWindow 调用
        public void OnCreateArticleClick()
        {
            CreateArticleButton_Click(null, null);
        }

        public async void OnSearchTextChanged(string searchText)
        {
            // 取消之前的防抖
            _searchDebounce?.Cancel();
            _searchDebounce = new CancellationTokenSource();
            var token = _searchDebounce.Token;

            try
            {
                await System.Threading.Tasks.Task.Delay(300, token);
                if (token.IsCancellationRequested) return;

                ViewModel.SearchKeyword = searchText;
                await LoadArticlesAsync();
            }
            catch (OperationCanceledException)
            {
                // 防抖取消，忽略
            }
        }

        private async void StatusFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitialized) return;

            if (StatusFilterComboBox.SelectedItem is ComboBoxItem item)
            {
                var tagStr = item.Tag as string;
                ViewModel.FilterStatus = string.IsNullOrEmpty(tagStr) ? null : tagStr;
                await LoadArticlesAsync();
            }
        }

        private async void CategoryFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitialized) return;

            if (CategoryFilterComboBox.SelectedItem is ComboBoxItem item)
            {
                ViewModel.FilterCategoryId = item.Tag as long?;
                await LoadArticlesAsync();
            }
        }

        private void EditArticleButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is long articleId)
            {
                // 导航到文章编辑页（编辑模式）
                Frame.Navigate(typeof(ArticleEditorPage), articleId);
            }
        }

        private void ViewArticleButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is long articleId)
            {
                // 导航到文章编辑页（查看模式，传递负数ID表示只读）
                Frame.Navigate(typeof(ArticleEditorPage), -articleId);
            }
        }

        private async void ToggleTopButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is long articleId)
            {
                try
                {
                    await ViewModel.ToggleTopAsync(articleId);
                    await LoadArticlesAsync();
                }
                catch (Exception ex)
                {
                    var dialog = new ContentDialog
                    {
                        Title = "操作失败",
                        Content = ex.Message,
                        CloseButtonText = "确定",
                        XamlRoot = this.XamlRoot
                    };
                    await dialog.ShowAsync();
                }
            }
        }

        private async void DeleteArticleButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is long articleId)
            {
                var dialog = new ContentDialog
                {
                    Title = "确认删除",
                    Content = "确定要删除这篇文章吗？",
                    PrimaryButtonText = "删除",
                    CloseButtonText = "取消",
                    DefaultButton = ContentDialogButton.Close,
                    XamlRoot = this.XamlRoot
                };

                var result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    try
                    {
                        await ViewModel.DeleteArticleAsync(articleId);
                        await LoadArticlesAsync();
                    }
                    catch (Exception ex)
                    {
                        var errorDialog = new ContentDialog
                        {
                            Title = "删除失败",
                            Content = ex.Message,
                            CloseButtonText = "确定",
                            XamlRoot = this.XamlRoot
                        };
                        await errorDialog.ShowAsync();
                    }
                }
            }
        }
    }
}
