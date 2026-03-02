using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Blog_Manager.ViewModels;
using Blog_Manager.Services.Api;
using System;
using System.Threading.Tasks;

namespace Blog_Manager.Views
{
    /// <summary>
    /// Dashboard page showing website statistics
    /// </summary>
    public sealed partial class DashboardPage : Page
    {
        public DashboardViewModel ViewModel { get; }

        public DashboardPage()
        {
            this.InitializeComponent();

            // Get StatsApi from App
            var app = Application.Current as App;
            var statsApi = app?.ApiServiceFactory?.CreateStatsApi()
                ?? throw new InvalidOperationException("ApiServiceFactory not found");

            ViewModel = new DashboardViewModel(statsApi);
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // Ensure XamlRoot is available before loading
            await Task.Yield();

            // Load data asynchronously
            try
            {
                await ViewModel.LoadDataAsync();
            }
            catch (Exception ex)
            {
                // Log error but don't crash
                System.Diagnostics.Debug.WriteLine($"DashboardPage: Failed to load data - {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");

                // Update error message on UI thread
                ViewModel.ErrorMessage = $"加载失败: {ex.Message}";
            }
        }

        /// <summary>
        /// Handle refresh button click from toolbar
        /// </summary>
        public async void OnRefreshClick()
        {
            try
            {
                await ViewModel.RefreshAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DashboardPage: Refresh failed - {ex.Message}");
                ViewModel.ErrorMessage = $"刷新失败: {ex.Message}";
            }
        }

        private void ChartSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (VisitTrendChart == null || ArticleRankingChart == null) return;

            var index = ChartSelector.SelectedIndex;
            VisitTrendChart.Visibility = index == 0 ? Visibility.Visible : Visibility.Collapsed;
            ArticleRankingChart.Visibility = index == 1 ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
