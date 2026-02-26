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
    public partial class ThemesViewModel : ObservableObject
    {
        private readonly IThemeApi _themeApi;

        [ObservableProperty]
        private ObservableCollection<Theme> _themes = new();

        [ObservableProperty]
        private bool _isLoading;

        public ThemesViewModel()
        {
            var app = (Application.Current as App) ?? throw new InvalidOperationException("App instance not found");
            _themeApi = app.ApiServiceFactory.CreateThemeApi();
        }

        public async Task LoadThemesAsync()
        {
            try
            {
                IsLoading = true;
                var response = await _themeApi.GetThemesAsync();

                if (response.Code == 200 && response.Data != null)
                {
                    Themes = new ObservableCollection<Theme>(response.Data.OrderBy(t => t.DisplayOrder));
                }
                else
                {
                    throw new Exception(response.Message ?? "加载主题列表失败");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"加载主题列表失败: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        public async Task<Theme> GetThemeByIdAsync(long id)
        {
            var response = await _themeApi.GetThemeByIdAsync(id);
            if (response.Code == 200 && response.Data != null)
            {
                return response.Data;
            }
            throw new Exception(response.Message ?? "获取主题详情失败");
        }

        public async Task DeleteThemeAsync(long id)
        {
            var response = await _themeApi.DeleteThemeAsync(id);
            if (response.Code != 200)
            {
                throw new Exception(response.Message ?? "删除主题失败");
            }
        }

        public async Task ToggleThemeApplicationAsync(long id, bool isApplied)
        {
            var response = await _themeApi.ToggleThemeApplicationAsync(id, isApplied);
            if (response.Code != 200)
            {
                throw new Exception(response.Message ?? "切换主题应用状态失败");
            }
        }
    }
}
