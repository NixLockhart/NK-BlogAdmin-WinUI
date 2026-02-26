using CommunityToolkit.Mvvm.ComponentModel;
using Blog_Manager.Models;
using Blog_Manager.Services.Api;
using Microsoft.UI.Xaml;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Blog_Manager.ViewModels
{
    /// <summary>
    /// ViewModel for configuration management page
    /// </summary>
    public partial class ConfigViewModel : ObservableObject
    {
        private readonly IConfigApi _configApi;

        [ObservableProperty]
        private ObservableCollection<SiteConfig> _configs = new();

        [ObservableProperty]
        private SiteConfig? _selectedConfig;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

        public ConfigViewModel()
        {
            var app = (Application.Current as App) ?? throw new InvalidOperationException("App instance not found");
            _configApi = app.ApiServiceFactory.CreateConfigApi();
        }

        /// <summary>
        /// Load all configurations
        /// </summary>
        public async Task LoadConfigsAsync()
        {
            IsLoading = true;
            ErrorMessage = string.Empty;
            OnPropertyChanged(nameof(HasError));

            try
            {
                var response = await _configApi.GetConfigsAsync();

                if (response.Code == 200 && response.Data != null)
                {
                    Configs = new ObservableCollection<SiteConfig>(response.Data);
                }
                else
                {
                    ErrorMessage = response.Message ?? "加载配置列表失败";
                    OnPropertyChanged(nameof(HasError));
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"加载配置列表失败: {ex.Message}";
                OnPropertyChanged(nameof(HasError));
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Update configuration value
        /// </summary>
        public async Task UpdateConfigAsync(string key, string value)
        {
            try
            {
                var response = await _configApi.UpdateConfigAsync(key, value);
                if (response.Code != 200)
                {
                    throw new Exception(response.Message ?? "更新配置失败");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"更新配置失败: {ex.Message}");
            }
        }

        /// <summary>
        /// Create new configuration
        /// </summary>
        public async Task<SiteConfig> CreateConfigAsync(ConfigCreateRequest request)
        {
            try
            {
                var response = await _configApi.CreateConfigAsync(request);
                if (response.Code != 200 || response.Data == null)
                {
                    throw new Exception(response.Message ?? "创建配置失败");
                }
                return response.Data;
            }
            catch (Exception ex)
            {
                throw new Exception($"创建配置失败: {ex.Message}");
            }
        }

        /// <summary>
        /// Delete configuration
        /// </summary>
        public async Task DeleteConfigAsync(string key)
        {
            try
            {
                var response = await _configApi.DeleteConfigAsync(key);
                if (response.Code != 200)
                {
                    throw new Exception(response.Message ?? "删除配置失败");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"删除配置失败: {ex.Message}");
            }
        }
    }
}
