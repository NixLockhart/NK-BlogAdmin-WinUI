using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Blog_Manager.Models;
using Blog_Manager.Services.Api;
using System;
using System.Threading.Tasks;

namespace Blog_Manager.ViewModels
{
    /// <summary>
    /// ViewModel for the Dashboard page
    /// </summary>
    public partial class DashboardViewModel : ObservableObject
    {
        private readonly IStatsApi _statsApi;

        [ObservableProperty]
        private long _totalVisits;

        [ObservableProperty]
        private long _todayVisits;

        [ObservableProperty]
        private int _articleCount;

        [ObservableProperty]
        private int _likeCount;

        [ObservableProperty]
        private int _commentCount;

        [ObservableProperty]
        private int _messageCount;

        [ObservableProperty]
        private string _runtime = "计算中...";

        [ObservableProperty]
        private string _systemVersion = "加载中...";

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasError))]
        private string _errorMessage = string.Empty;

        /// <summary>
        /// Whether there is an error
        /// </summary>
        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

        public DashboardViewModel(IStatsApi statsApi)
        {
            _statsApi = statsApi;
        }

        /// <summary>
        /// Load statistics data from API
        /// </summary>
        [RelayCommand]
        public async Task LoadDataAsync()
        {
            IsLoading = true;
            ErrorMessage = string.Empty;

            try
            {
                var result = await _statsApi.GetStatisticsAsync();

                if (result.IsSuccess && result.Data != null)
                {
                    var stats = result.Data;

                    TotalVisits = stats.TotalViews;
                    TodayVisits = stats.TodayViews;
                    ArticleCount = (int)stats.TotalArticles;
                    LikeCount = (int)stats.TotalLikes;
                    CommentCount = (int)stats.TotalComments;
                    MessageCount = (int)stats.TotalMessages;

                    // Calculate runtime string from runningDays
                    Runtime = CalculateRuntime(stats.RunningDays);

                    // System version
                    SystemVersion = string.IsNullOrEmpty(stats.Version) ? "v1.0.0" : $"v{stats.Version}";
                }
                else
                {
                    ErrorMessage = result.Message ?? "加载统计数据失败";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"加载失败: {ex.Message}";
                // Set default values on error
                TotalVisits = 0;
                TodayVisits = 0;
                ArticleCount = 0;
                LikeCount = 0;
                CommentCount = 0;
                MessageCount = 0;
                Runtime = "未知";
                SystemVersion = "v1.0.0";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Calculate runtime string from running days
        /// </summary>
        private string CalculateRuntime(long runningDays)
        {
            if (runningDays >= 1)
            {
                return $"{runningDays} 天";
            }
            else
            {
                return "刚刚启动";
            }
        }

        /// <summary>
        /// Refresh statistics data
        /// </summary>
        [RelayCommand]
        public async Task RefreshAsync()
        {
            await LoadDataAsync();
        }
    }
}
