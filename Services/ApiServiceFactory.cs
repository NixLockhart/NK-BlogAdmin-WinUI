using Blog_Manager.Services.Api;
using Refit;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Timers;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Blog_Manager.Services
{
    /// <summary>
    /// API服务工厂
    /// </summary>
    public class ApiServiceFactory : IDisposable
    {
        private HttpClient _httpClient;
        private readonly RefitSettings _refitSettings;
        private readonly Func<string?> _getAuthToken;
        private readonly BackendConfigService _backendConfigService;

        // 用于延迟释放旧的 HttpClient，避免正在使用时被释放
        private readonly List<(HttpClient client, DateTime expireTime)> _pendingDisposeClients = new();
        private readonly Timer _cleanupTimer;
        private readonly object _lock = new();
        private bool _disposed;

        public string CurrentBaseUrl { get; private set; }

        public ApiServiceFactory(Func<string?> getAuthToken, BackendConfigService backendConfigService)
        {
            _getAuthToken = getAuthToken;
            _backendConfigService = backendConfigService;

            // Configure Refit to use Newtonsoft.Json with camelCase
            _refitSettings = new RefitSettings
            {
                ContentSerializer = new NewtonsoftJsonContentSerializer(
                    new JsonSerializerSettings
                    {
                        ContractResolver = new CamelCasePropertyNamesContractResolver(),
                        NullValueHandling = NullValueHandling.Ignore,
                        DateParseHandling = DateParseHandling.DateTime,
                        DateTimeZoneHandling = DateTimeZoneHandling.Local
                    })
            };

            // 初始化HttpClient
            CurrentBaseUrl = backendConfigService.CurrentBackendUrl ?? "http://localhost:8080";
            InitializeHttpClient();

            // 监听后端切换事件
            _backendConfigService.CurrentBackendChanged += OnBackendChanged;

            // 启动清理定时器，每30秒检查一次待释放的HttpClient
            _cleanupTimer = new Timer(30000);
            _cleanupTimer.Elapsed += CleanupExpiredClients;
            _cleanupTimer.AutoReset = true;
            _cleanupTimer.Start();
        }

        private void InitializeHttpClient()
        {
            var handler = new AuthorizingHttpClientHandler(_getAuthToken);
            _httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri(CurrentBaseUrl),
                Timeout = TimeSpan.FromSeconds(30)
            };
        }

        private void OnBackendChanged(object? sender, string newUrl)
        {
            lock (_lock)
            {
                CurrentBaseUrl = newUrl;

                // 将旧的 HttpClient 加入待释放队列，延迟60秒后释放
                // 这样可以确保正在进行的请求有足够时间完成
                if (_httpClient != null)
                {
                    _pendingDisposeClients.Add((_httpClient, DateTime.UtcNow.AddSeconds(60)));
                }

                InitializeHttpClient();
            }
        }

        /// <summary>
        /// 清理已过期的 HttpClient 实例
        /// </summary>
        private void CleanupExpiredClients(object? sender, ElapsedEventArgs e)
        {
            lock (_lock)
            {
                var now = DateTime.UtcNow;
                for (int i = _pendingDisposeClients.Count - 1; i >= 0; i--)
                {
                    var (client, expireTime) = _pendingDisposeClients[i];
                    if (now >= expireTime)
                    {
                        try
                        {
                            client.Dispose();
                        }
                        catch
                        {
                            // 忽略释放时的异常
                        }
                        _pendingDisposeClients.RemoveAt(i);
                    }
                }
            }
        }

        /// <summary>
        /// 释放所有资源
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _cleanupTimer?.Stop();
            _cleanupTimer?.Dispose();

            _backendConfigService.CurrentBackendChanged -= OnBackendChanged;

            lock (_lock)
            {
                // 释放当前的 HttpClient
                _httpClient?.Dispose();

                // 释放所有待释放的 HttpClient
                foreach (var (client, _) in _pendingDisposeClients)
                {
                    try
                    {
                        client.Dispose();
                    }
                    catch
                    {
                        // 忽略释放时的异常
                    }
                }
                _pendingDisposeClients.Clear();
            }
        }

        public IAuthApi CreateAuthApi() => RestService.For<IAuthApi>(_httpClient, _refitSettings);
        public IArticleApi CreateArticleApi() => RestService.For<IArticleApi>(_httpClient, _refitSettings);
        public ICategoryApi CreateCategoryApi() => RestService.For<ICategoryApi>(_httpClient, _refitSettings);
        public ICommentApi CreateCommentApi() => RestService.For<ICommentApi>(_httpClient, _refitSettings);
        public IMessageApi CreateMessageApi() => RestService.For<IMessageApi>(_httpClient, _refitSettings);
        public IConfigApi CreateConfigApi() => RestService.For<IConfigApi>(_httpClient, _refitSettings);
        public IAnnouncementApi CreateAnnouncementApi() => RestService.For<IAnnouncementApi>(_httpClient, _refitSettings);
        public IUpdateLogApi CreateUpdateLogApi() => RestService.For<IUpdateLogApi>(_httpClient, _refitSettings);
        public IStatsApi CreateStatsApi() => RestService.For<IStatsApi>(_httpClient, _refitSettings);
        public IFileApi CreateFileApi() => RestService.For<IFileApi>(_httpClient, _refitSettings);
        public IWidgetApi CreateWidgetApi() => RestService.For<IWidgetApi>(_httpClient, _refitSettings);
        public IThemeApi CreateThemeApi() => RestService.For<IThemeApi>(_httpClient, _refitSettings);
        public IAdminProfileApi CreateAdminProfileApi() => RestService.For<IAdminProfileApi>(_httpClient, _refitSettings);
    }
}
