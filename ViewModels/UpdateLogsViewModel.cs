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
    /// ViewModel for update log management page
    /// </summary>
    public partial class UpdateLogsViewModel : ObservableObject
    {
        private readonly IUpdateLogApi _updateLogApi;

        [ObservableProperty]
        private ObservableCollection<UpdateLog> _updateLogs = new();

        [ObservableProperty]
        private UpdateLog? _selectedUpdateLog;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

        public UpdateLogsViewModel()
        {
            var app = (Application.Current as App) ?? throw new InvalidOperationException("App instance not found");
            _updateLogApi = app.ApiServiceFactory.CreateUpdateLogApi();
        }

        /// <summary>
        /// Load all update logs
        /// </summary>
        public async Task LoadUpdateLogsAsync()
        {
            IsLoading = true;
            ErrorMessage = string.Empty;
            OnPropertyChanged(nameof(HasError));

            try
            {
                var response = await _updateLogApi.GetUpdateLogsAsync();

                if (response.Code == 200 && response.Data != null)
                {
                    UpdateLogs = new ObservableCollection<UpdateLog>(response.Data);
                }
                else
                {
                    ErrorMessage = response.Message ?? "加载更新日志列表失败";
                    OnPropertyChanged(nameof(HasError));
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"加载更新日志列表失败: {ex.Message}";
                OnPropertyChanged(nameof(HasError));
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Create update log
        /// </summary>
        public async Task CreateUpdateLogAsync(UpdateLogRequest request)
        {
            try
            {
                var response = await _updateLogApi.CreateUpdateLogAsync(request);
                if (response.Code != 200)
                {
                    throw new Exception(response.Message ?? "创建更新日志失败");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"创建更新日志失败: {ex.Message}");
            }
        }

        /// <summary>
        /// Update update log
        /// </summary>
        public async Task UpdateUpdateLogAsync(long id, UpdateLogRequest request)
        {
            try
            {
                var response = await _updateLogApi.UpdateUpdateLogAsync(id, request);
                if (response.Code != 200)
                {
                    throw new Exception(response.Message ?? "更新更新日志失败");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"更新更新日志失败: {ex.Message}");
            }
        }

        /// <summary>
        /// Delete update log
        /// </summary>
        public async Task DeleteUpdateLogAsync(long id)
        {
            try
            {
                var response = await _updateLogApi.DeleteUpdateLogAsync(id);
                if (response.Code != 200)
                {
                    throw new Exception(response.Message ?? "删除更新日志失败");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"删除更新日志失败: {ex.Message}");
            }
        }
    }
}
