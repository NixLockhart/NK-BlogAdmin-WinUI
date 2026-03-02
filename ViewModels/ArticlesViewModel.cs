using CommunityToolkit.Mvvm.ComponentModel;
using Blog_Manager.Models;
using Blog_Manager.Services.Api;
using Microsoft.UI.Xaml;
using Microsoft.UI.Dispatching;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Blog_Manager.ViewModels
{
    public partial class ArticlesViewModel : ObservableObject
    {
        private readonly IArticleApi _articleApi;
        private readonly ICategoryApi _categoryApi;
        private readonly DispatcherQueue? _dispatcherQueue;

        [ObservableProperty]
        private ObservableCollection<Article> _articles = new();

        [ObservableProperty]
        private string _searchKeyword = string.Empty;

        [ObservableProperty]
        private string? _filterStatus = null;

        [ObservableProperty]
        private long? _filterCategoryId = null;

        public List<Category> Categories { get; private set; } = new();

        public ArticlesViewModel()
        {
            var app = (Application.Current as App) ?? throw new InvalidOperationException("App instance not found");
            _articleApi = app.ApiServiceFactory.CreateArticleApi();
            _categoryApi = app.ApiServiceFactory.CreateCategoryApi();

            try
            {
                if (app.Window?.DispatcherQueue != null)
                {
                    _dispatcherQueue = app.Window.DispatcherQueue;
                }
            }
            catch { }
        }

        public async Task LoadCategoriesAsync()
        {
            try
            {
                var response = await _categoryApi.GetCategoriesAsync();
                if (response.Code == 200 && response.Data != null)
                {
                    Categories = response.Data;
                }
            }
            catch
            {
                Categories = new List<Category>();
            }
        }

        public async Task LoadArticlesAsync()
        {
            try
            {
                var keyword = string.IsNullOrWhiteSpace(SearchKeyword) ? null : SearchKeyword.Trim();

                var response = await _articleApi.GetArticlesAsync(
                    page: 0,
                    size: 10000,
                    keyword: keyword,
                    categoryId: FilterCategoryId,
                    status: FilterStatus);

                if (response.Code == 200 && response.Data != null)
                {
                    var articles = response.Data.Content;

                    void UpdateCollection()
                    {
                        Articles.Clear();
                        foreach (var article in articles)
                        {
                            Articles.Add(article);
                        }
                    }

                    if (_dispatcherQueue != null && !_dispatcherQueue.HasThreadAccess)
                    {
                        _dispatcherQueue.TryEnqueue(UpdateCollection);
                    }
                    else
                    {
                        UpdateCollection();
                    }
                }
                else
                {
                    throw new Exception(response.Message ?? "加载文章列表失败");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"加载文章列表失败: {ex.Message}", ex);
            }
        }

        public async Task ToggleTopAsync(long articleId)
        {
            try
            {
                var response = await _articleApi.ToggleTopAsync(articleId);
                if (response.Code != 200)
                {
                    throw new Exception(response.Message ?? "置顶操作失败");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"置顶操作失败: {ex.Message}");
            }
        }

        public async Task DeleteArticleAsync(long articleId)
        {
            try
            {
                var response = await _articleApi.DeleteArticleAsync(articleId);
                if (response.Code != 200)
                {
                    throw new Exception(response.Message ?? "删除文章失败");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"删除文章失败: {ex.Message}");
            }
        }

        public async Task RestoreArticleAsync(long articleId)
        {
            try
            {
                var response = await _articleApi.RestoreArticleAsync(articleId);
                if (response.Code != 200)
                {
                    throw new Exception(response.Message ?? "恢复文章失败");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"恢复文章失败: {ex.Message}");
            }
        }

        public async Task PermanentlyDeleteArticleAsync(long articleId)
        {
            try
            {
                var response = await _articleApi.PermanentlyDeleteArticleAsync(articleId);
                if (response.Code != 200)
                {
                    throw new Exception(response.Message ?? "永久删除文章失败");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"永久删除文章失败: {ex.Message}");
            }
        }
    }
}
