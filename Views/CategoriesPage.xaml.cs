using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Blog_Manager.ViewModels;
using Blog_Manager.Models;
using System;
using System.Linq;

namespace Blog_Manager.Views
{
    public sealed partial class CategoriesPage : Page
    {
        public CategoriesViewModel ViewModel { get; }

        public CategoriesPage()
        {
            this.InitializeComponent();
            ViewModel = new CategoriesViewModel();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            await LoadCategoriesAsync();
        }

        private async System.Threading.Tasks.Task LoadCategoriesAsync()
        {
            LoadingPanel.Visibility = Visibility.Visible;
            ContentPanel.Visibility = Visibility.Collapsed;
            ErrorInfoBar.Visibility = Visibility.Collapsed;

            try
            {
                await ViewModel.LoadCategoriesAsync();
                ContentPanel.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                ErrorInfoBar.Message = ex.Message;
                ErrorInfoBar.Visibility = Visibility.Visible;
            }
            finally
            {
                LoadingPanel.Visibility = Visibility.Collapsed;
            }
        }

        private async void CreateCategoryButton_Click(object sender, RoutedEventArgs e)
        {
            await ShowCategoryDialog(null);
        }

        // 公共方法供 MainWindow 调用
        public async void OnCreateCategoryClick()
        {
            await ShowCategoryDialog(null);
        }

        private async void EditCategoryButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is long categoryId)
            {
                var category = ViewModel.Categories.FirstOrDefault(c => c.Id == categoryId);
                if (category != null)
                {
                    await ShowCategoryDialog(category);
                }
            }
        }

        private async System.Threading.Tasks.Task ShowCategoryDialog(Category? category)
        {
            var isEdit = category != null;

            var nameBox = new TextBox
            {
                Header = "分类名称 *",
                PlaceholderText = "请输入分类名称",
                Text = category?.Name ?? string.Empty,
                MaxLength = 50
            };

            var slugBox = new TextBox
            {
                Header = "分类别名 (Slug)",
                PlaceholderText = "用于URL，如: tech-articles（留空则自动生成）",
                Text = category?.Slug ?? string.Empty,
                MaxLength = 100,
                Margin = new Thickness(0, 8, 0, 0)
            };

            var descriptionBox = new TextBox
            {
                Header = "分类描述",
                PlaceholderText = "请输入分类描述（可选）",
                Text = category?.Description ?? string.Empty,
                MaxLength = 200,
                TextWrapping = TextWrapping.Wrap,
                AcceptsReturn = true,
                Height = 80,
                Margin = new Thickness(0, 8, 0, 0)
            };

            var sortOrderBox = new NumberBox
            {
                Header = "排序权重",
                PlaceholderText = "数值越大越靠前",
                Value = category?.SortOrder ?? 0,
                SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Inline,
                Margin = new Thickness(0, 8, 0, 0)
            };

            var stackPanel = new StackPanel
            {
                Spacing = 8,
                Children = { nameBox, slugBox, descriptionBox, sortOrderBox }
            };

            var dialog = new ContentDialog
            {
                Title = isEdit ? "编辑分类" : "新建分类",
                Content = stackPanel,
                PrimaryButtonText = "保存",
                CloseButtonText = "取消",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                if (string.IsNullOrWhiteSpace(nameBox.Text))
                {
                    var errorDialog = new ContentDialog
                    {
                        Title = "验证失败",
                        Content = "分类名称不能为空",
                        CloseButtonText = "确定",
                        XamlRoot = this.XamlRoot
                    };
                    await errorDialog.ShowAsync();
                    return;
                }

                var request = new CategorySaveRequest
                {
                    Name = nameBox.Text.Trim(),
                    Description = string.IsNullOrWhiteSpace(descriptionBox.Text) ? null : descriptionBox.Text.Trim(),
                    Slug = string.IsNullOrWhiteSpace(slugBox.Text) ? null : slugBox.Text.Trim(),
                    SortOrder = (int)sortOrderBox.Value
                };

                try
                {
                    if (isEdit)
                    {
                        await ViewModel.UpdateCategoryAsync(category!.Id, request);
                    }
                    else
                    {
                        await ViewModel.CreateCategoryAsync(request);
                    }

                    await LoadCategoriesAsync();
                }
                catch (Exception ex)
                {
                    var errorDialog = new ContentDialog
                    {
                        Title = "操作失败",
                        Content = ex.Message,
                        CloseButtonText = "确定",
                        XamlRoot = this.XamlRoot
                    };
                    await errorDialog.ShowAsync();
                }
            }
        }

        private async void DeleteCategoryButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is long categoryId)
            {
                var category = ViewModel.Categories.FirstOrDefault(c => c.Id == categoryId);
                if (category == null) return;

                var hasArticles = category.ArticleCount > 0;
                string content = hasArticles
                    ? $"该分类下有 {category.ArticleCount} 篇文章。\n\n请选择删除方式："
                    : $"确定要删除分类 \"{category.Name}\" 吗？";

                var deleteArticlesCheckBox = new CheckBox
                {
                    Content = "同时删除该分类下的所有文章",
                    IsChecked = false,
                    Margin = new Thickness(0, 8, 0, 0)
                };

                var stackPanel = new StackPanel
                {
                    Children = { new TextBlock { Text = content, TextWrapping = TextWrapping.Wrap } }
                };

                if (hasArticles)
                {
                    stackPanel.Children.Add(deleteArticlesCheckBox);
                    stackPanel.Children.Add(new TextBlock
                    {
                        Text = "注意：如果不选择此项，文章将保留但不属于任何分类。",
                        Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["SystemFillColorCautionBrush"],
                        TextWrapping = TextWrapping.Wrap,
                        FontSize = 12,
                        Margin = new Thickness(0, 4, 0, 0)
                    });
                }

                var dialog = new ContentDialog
                {
                    Title = "确认删除",
                    Content = stackPanel,
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
                        bool deleteArticles = hasArticles && deleteArticlesCheckBox.IsChecked == true;
                        await ViewModel.DeleteCategoryAsync(categoryId, deleteArticles);
                        await LoadCategoriesAsync();
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

        private async void MoveUpButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is long categoryId)
            {
                var category = ViewModel.Categories.FirstOrDefault(c => c.Id == categoryId);
                if (category != null)
                {
                    try
                    {
                        await ViewModel.MoveCategoryUpAsync(category);
                    }
                    catch (Exception ex)
                    {
                        var errorDialog = new ContentDialog
                        {
                            Title = "操作失败",
                            Content = ex.Message,
                            CloseButtonText = "确定",
                            XamlRoot = this.XamlRoot
                        };
                        await errorDialog.ShowAsync();
                    }
                }
            }
        }

        private async void MoveDownButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is long categoryId)
            {
                var category = ViewModel.Categories.FirstOrDefault(c => c.Id == categoryId);
                if (category != null)
                {
                    try
                    {
                        await ViewModel.MoveCategoryDownAsync(category);
                    }
                    catch (Exception ex)
                    {
                        var errorDialog = new ContentDialog
                        {
                            Title = "操作失败",
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
