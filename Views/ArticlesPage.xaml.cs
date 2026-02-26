using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Blog_Manager.ViewModels;
using System;

namespace Blog_Manager.Views
{
    public sealed partial class ArticlesPage : Page
    {
        public ArticlesViewModel ViewModel { get; }

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
                await LoadArticlesAsync();
            }
            catch (Exception ex)
            {
                ErrorInfoBar.Message = $"页面加载失败: {ex.Message}";
                ErrorInfoBar.Visibility = Visibility.Visible;
                LoadingPanel.Visibility = Visibility.Collapsed;
            }
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
            ViewModel.SearchKeyword = searchText;
            await LoadArticlesAsync();
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
