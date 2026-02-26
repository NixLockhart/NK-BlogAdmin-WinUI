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
    public partial class WidgetsViewModel : ObservableObject
    {
        private readonly IWidgetApi _widgetApi;

        [ObservableProperty]
        private ObservableCollection<Widget> _widgets = new();

        [ObservableProperty]
        private bool _isLoading;

        public WidgetsViewModel()
        {
            var app = (Application.Current as App) ?? throw new InvalidOperationException("App instance not found");
            _widgetApi = app.ApiServiceFactory.CreateWidgetApi();
        }

        public async Task LoadWidgetsAsync()
        {
            try
            {
                IsLoading = true;
                var response = await _widgetApi.GetWidgetsAsync();

                if (response.Code == 200 && response.Data != null)
                {
                    Widgets = new ObservableCollection<Widget>(response.Data.OrderBy(w => w.DisplayOrder));
                }
                else
                {
                    throw new Exception(response.Message ?? "加载小工具列表失败");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"加载小工具列表失败: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        public async Task<WidgetCode> GetWidgetCodeAsync(long id)
        {
            var response = await _widgetApi.GetWidgetCodeAsync(id);
            if (response.Code == 200 && response.Data != null)
            {
                return response.Data;
            }
            throw new Exception(response.Message ?? "获取小工具代码失败");
        }

        public async Task CreateWidgetAsync(WidgetCreateRequest request)
        {
            var response = await _widgetApi.CreateWidgetAsync(request);
            if (response.Code != 200)
            {
                throw new Exception(response.Message ?? "创建小工具失败");
            }
        }

        public async Task UpdateWidgetAsync(long id, WidgetUpdateRequest request)
        {
            var response = await _widgetApi.UpdateWidgetAsync(id, request);
            if (response.Code != 200)
            {
                throw new Exception(response.Message ?? "更新小工具失败");
            }
        }

        public async Task DeleteWidgetAsync(long id)
        {
            var response = await _widgetApi.DeleteWidgetAsync(id);
            if (response.Code != 200)
            {
                throw new Exception(response.Message ?? "删除小工具失败");
            }
        }

        public async Task ToggleWidgetApplicationAsync(long id, bool isApplied)
        {
            var response = await _widgetApi.ToggleWidgetApplicationAsync(id, isApplied);
            if (response.Code != 200)
            {
                throw new Exception(response.Message ?? "切换小工具应用状态失败");
            }
        }

        public async Task<string> ExportWidgetAsync(long id)
        {
            var response = await _widgetApi.ExportWidgetAsync(id);
            if (response.Code == 200 && response.Data != null)
            {
                return response.Data;
            }
            throw new Exception(response.Message ?? "导出小工具失败");
        }
    }
}
