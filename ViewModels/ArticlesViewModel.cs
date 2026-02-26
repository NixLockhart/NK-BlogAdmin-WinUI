using CommunityToolkit.Mvvm.ComponentModel;
using Blog_Manager.Models;
using Blog_Manager.Services.Api;
using Microsoft.UI.Xaml;
using Microsoft.UI.Dispatching;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Blog_Manager.ViewModels
{
    public partial class ArticlesViewModel : ObservableObject
    {
        private readonly IArticleApi _articleApi;
        private readonly DispatcherQueue? _dispatcherQueue;

        [ObservableProperty]
        private ObservableCollection<Article> _articles = new();

        [ObservableProperty]
        private string _searchKeyword = string.Empty;

        public ArticlesViewModel()
        {
            var app = (Application.Current as App) ?? throw new InvalidOperationException("App instance not found");
            _articleApi = app.ApiServiceFactory.CreateArticleApi();

            // 获取 UI 线程的 DispatcherQueue
            try
            {
                if (app.Window?.DispatcherQueue != null)
                {
                    _dispatcherQueue = app.Window.DispatcherQueue;
                }
            }
            catch
            {
                // 如果获取失败，后续会在 UI 线程上直接更新
            }
        }

        public async Task LoadArticlesAsync()
        {
            try
            {
                // 获取所有文章，不分页
                var response = await _articleApi.GetArticlesAsync(page: 0, size: 10000);

                if (response.Code == 200 && response.Data != null)
                {
                    // 确保在 UI 线程上更新集合
                    var articles = response.Data.Content;

                    void UpdateCollection()
                    {
                        // 清空并重新填充集合，而不是替换整个集合
                        // 这样可以避免 x:Bind 的绑定问题
                        Articles.Clear();
                        foreach (var article in articles)
                        {
                            Articles.Add(article);
                        }
                    }

                    if (_dispatcherQueue != null && !_dispatcherQueue.HasThreadAccess)
                    {
                        // 如果不在 UI 线程上，调度到 UI 线程执行
                        bool enqueued = false;
                        _dispatcherQueue.TryEnqueue(() =>
                        {
                            UpdateCollection();
                            enqueued = true;
                        });

                        // 等待更新完成
                        while (!enqueued)
                        {
                            await System.Threading.Tasks.Task.Delay(10);
                        }
                    }
                    else
                    {
                        // 已经在 UI 线程上，直接更新
                        UpdateCollection();
                    }
                }
                else
                {
                    var errorMsg = response.Message ?? "加载文章列表失败";
                    throw new Exception(errorMsg);
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
    }
}
