using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Blog_Manager.Models;
using Blog_Manager.Services.Api;
using LiveChartsCore;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
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

        // Chart properties
        [ObservableProperty]
        private ISeries[] _visitSeries = Array.Empty<ISeries>();

        [ObservableProperty]
        private ICartesianAxis[] _visitXAxes = Array.Empty<ICartesianAxis>();

        [ObservableProperty]
        private ICartesianAxis[] _visitYAxes = Array.Empty<ICartesianAxis>();

        [ObservableProperty]
        private ISeries[] _rankingSeries = Array.Empty<ISeries>();

        [ObservableProperty]
        private ICartesianAxis[] _rankingXAxes = Array.Empty<ICartesianAxis>();

        [ObservableProperty]
        private ICartesianAxis[] _rankingYAxes = Array.Empty<ICartesianAxis>();

        [ObservableProperty]
        private bool _hasChartData;

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
                    SystemVersion = string.IsNullOrEmpty(stats.Version) ? "v1.0.0" : stats.Version;
                }
                else
                {
                    ErrorMessage = result.Message ?? "加载统计数据失败";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"加载失败: {ex.Message}";
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

            // Load charts in background (don't block stats cards)
            await LoadChartsAsync();
        }

        private async Task LoadChartsAsync()
        {
            try
            {
                var trendTask = _statsApi.GetVisitTrendAsync(30);
                var rankingTask = _statsApi.GetArticleRankingAsync(10);

                await Task.WhenAll(trendTask, rankingTask);

                var trendResult = trendTask.Result;
                var rankingResult = rankingTask.Result;

                if (trendResult.IsSuccess && trendResult.Data != null)
                {
                    BuildVisitTrendChart(trendResult.Data);
                }

                if (rankingResult.IsSuccess && rankingResult.Data != null)
                {
                    BuildArticleRankingChart(rankingResult.Data);
                }

                HasChartData = VisitSeries.Length > 0 || RankingSeries.Length > 0;
            }
            catch
            {
                // Chart loading failure is non-critical
                HasChartData = false;
            }
        }

        private void BuildVisitTrendChart(Dictionary<string, long> data)
        {
            if (data.Count == 0) return;

            var values = data.Values.ToArray();
            var labels = data.Keys.Select(d =>
            {
                // "2026-02-28" -> "02/28"
                if (d.Length >= 10)
                    return d.Substring(5).Replace("-", "/");
                return d;
            }).ToArray();

            VisitSeries = new ISeries[]
            {
                new LineSeries<long>
                {
                    Values = values,
                    Name = "访问量",
                    GeometrySize = 6,
                    GeometryStroke = new SolidColorPaint(new SKColor(0x60, 0x9B, 0xD1)) { StrokeThickness = 2 },
                    GeometryFill = new SolidColorPaint(SKColors.White),
                    Stroke = new SolidColorPaint(new SKColor(0x60, 0x9B, 0xD1)) { StrokeThickness = 2 },
                    Fill = new SolidColorPaint(new SKColor(0x60, 0x9B, 0xD1, 0x33)),
                    LineSmoothness = 0.3
                }
            };

            VisitXAxes = new ICartesianAxis[]
            {
                new Axis
                {
                    Labels = labels,
                    LabelsRotation = 45,
                    TextSize = 11,
                    LabelsPaint = new SolidColorPaint(new SKColor(0x99, 0x99, 0x99))
                }
            };

            VisitYAxes = new ICartesianAxis[]
            {
                new Axis
                {
                    MinLimit = 0,
                    TextSize = 11,
                    LabelsPaint = new SolidColorPaint(new SKColor(0x99, 0x99, 0x99))
                }
            };
        }

        private void BuildArticleRankingChart(Dictionary<string, long> data)
        {
            if (data.Count == 0) return;

            // Reverse so highest is on top in horizontal bar chart
            var items = data.Reverse().ToList();
            var values = items.Select(kv => kv.Value).ToArray();
            var fullTitles = items.Select(kv => kv.Key).ToArray();
            var labels = items.Select(kv =>
            {
                return kv.Key.Length > 12 ? kv.Key.Substring(0, 12) + "..." : kv.Key;
            }).ToArray();

            RankingSeries = new ISeries[]
            {
                new RowSeries<long>
                {
                    Values = values,
                    Name = "浏览量",
                    Stroke = null,
                    Fill = new SolidColorPaint(new SKColor(0x9B, 0x59, 0xB6)),
                    MaxBarWidth = 24,
                    DataLabelsPaint = new SolidColorPaint(new SKColor(0x99, 0x99, 0x99)),
                    DataLabelsSize = 11,
                    DataLabelsPosition = LiveChartsCore.Measure.DataLabelsPosition.End,
                    YToolTipLabelFormatter = point =>
                    {
                        var idx = point.Index;
                        var title = idx >= 0 && idx < fullTitles.Length ? fullTitles[idx] : "";
                        return $"{title}: {point.Model}";
                    }
                }
            };

            RankingXAxes = new ICartesianAxis[]
            {
                new Axis
                {
                    MinLimit = 0,
                    TextSize = 11,
                    LabelsPaint = new SolidColorPaint(new SKColor(0x99, 0x99, 0x99))
                }
            };

            RankingYAxes = new ICartesianAxis[]
            {
                new Axis
                {
                    Labels = labels,
                    TextSize = 11,
                    MinStep = 1,
                    ForceStepToMin = true,
                    LabelsPaint = new SolidColorPaint(new SKColor(0x99, 0x99, 0x99))
                }
            };
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
