using CommunityToolkit.Mvvm.ComponentModel;
using Blog_Manager.Models;
using Blog_Manager.Services.Api;
using Microsoft.UI.Xaml;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Blog_Manager.ViewModels
{
    public partial class CategoriesViewModel : ObservableObject
    {
        private readonly ICategoryApi _categoryApi;

        [ObservableProperty]
        private ObservableCollection<Category> _categories = new();

        [ObservableProperty]
        private long _totalCount;

        public CategoriesViewModel()
        {
            var app = (Application.Current as App) ?? throw new InvalidOperationException("App instance not found");
            _categoryApi = app.ApiServiceFactory.CreateCategoryApi();
        }

        public async Task LoadCategoriesAsync()
        {
            try
            {
                var response = await _categoryApi.GetCategoriesAsync();

                if (response.Code == 200 && response.Data != null)
                {
                    Categories = new ObservableCollection<Category>(response.Data);
                    TotalCount = response.Data.Count;
                }
                else
                {
                    throw new Exception(response.Message ?? "加载分类列表失败");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"加载分类列表失败: {ex.Message}");
            }
        }

        public async Task CreateCategoryAsync(CategorySaveRequest request)
        {
            try
            {
                var response = await _categoryApi.CreateCategoryAsync(request);
                if (response.Code != 200)
                {
                    throw new Exception(response.Message ?? "创建分类失败");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"创建分类失败: {ex.Message}");
            }
        }

        public async Task UpdateCategoryAsync(long categoryId, CategorySaveRequest request)
        {
            try
            {
                var response = await _categoryApi.UpdateCategoryAsync(categoryId, request);
                if (response.Code != 200)
                {
                    throw new Exception(response.Message ?? "更新分类失败");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"更新分类失败: {ex.Message}");
            }
        }

        public async Task DeleteCategoryAsync(long categoryId, bool deleteArticles)
        {
            try
            {
                var response = await _categoryApi.DeleteCategoryAsync(categoryId, deleteArticles);
                if (response.Code != 200)
                {
                    throw new Exception(response.Message ?? "删除分类失败");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"删除分类失败: {ex.Message}");
            }
        }

        public async Task MoveCategoryUpAsync(Category category)
        {
            var index = Categories.IndexOf(category);
            if (index > 0)
            {
                // Swap with previous item
                var temp = Categories[index - 1];
                Categories[index - 1] = category;
                Categories[index] = temp;

                await UpdateSortOrderAsync();
            }
        }

        public async Task MoveCategoryDownAsync(Category category)
        {
            var index = Categories.IndexOf(category);
            if (index < Categories.Count - 1)
            {
                // Swap with next item
                var temp = Categories[index + 1];
                Categories[index + 1] = category;
                Categories[index] = temp;

                await UpdateSortOrderAsync();
            }
        }

        private async Task UpdateSortOrderAsync()
        {
            try
            {
                var categoryIds = Categories.Select(c => c.Id).ToList();
                var response = await _categoryApi.UpdateCategoriesSortAsync(categoryIds);
                if (response.Code != 200)
                {
                    throw new Exception(response.Message ?? "更新排序失败");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"更新排序失败: {ex.Message}");
            }
        }
    }
}
