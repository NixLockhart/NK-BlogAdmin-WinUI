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
    /// ViewModel for announcement management page
    /// </summary>
    public partial class AnnouncementsViewModel : ObservableObject
    {
        private readonly IAnnouncementApi _announcementApi;

        [ObservableProperty]
        private ObservableCollection<Announcement> _announcements = new();

        [ObservableProperty]
        private Announcement? _selectedAnnouncement;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

        public AnnouncementsViewModel()
        {
            var app = (Application.Current as App) ?? throw new InvalidOperationException("App instance not found");
            _announcementApi = app.ApiServiceFactory.CreateAnnouncementApi();
        }

        /// <summary>
        /// Load all announcements
        /// </summary>
        public async Task LoadAnnouncementsAsync()
        {
            IsLoading = true;
            ErrorMessage = string.Empty;
            OnPropertyChanged(nameof(HasError));

            try
            {
                var response = await _announcementApi.GetAnnouncementsAsync();

                if (response.Code == 200 && response.Data != null)
                {
                    Announcements = new ObservableCollection<Announcement>(response.Data);
                }
                else
                {
                    ErrorMessage = response.Message ?? "加载公告列表失败";
                    OnPropertyChanged(nameof(HasError));
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"加载公告列表失败: {ex.Message}";
                OnPropertyChanged(nameof(HasError));
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Create announcement
        /// </summary>
        public async Task CreateAnnouncementAsync(Announcement announcement)
        {
            try
            {
                var response = await _announcementApi.CreateAnnouncementAsync(announcement);
                if (response.Code != 200)
                {
                    throw new Exception(response.Message ?? "创建公告失败");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"创建公告失败: {ex.Message}");
            }
        }

        /// <summary>
        /// Update announcement
        /// </summary>
        public async Task UpdateAnnouncementAsync(long id, Announcement announcement)
        {
            try
            {
                var response = await _announcementApi.UpdateAnnouncementAsync(id, announcement);
                if (response.Code != 200)
                {
                    throw new Exception(response.Message ?? "更新公告失败");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"更新公告失败: {ex.Message}");
            }
        }

        /// <summary>
        /// Delete announcement
        /// </summary>
        public async Task DeleteAnnouncementAsync(long id)
        {
            try
            {
                var response = await _announcementApi.DeleteAnnouncementAsync(id);
                if (response.Code != 200)
                {
                    throw new Exception(response.Message ?? "删除公告失败");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"删除公告失败: {ex.Message}");
            }
        }
    }
}
