using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Blog_Manager.Helpers;
using Newtonsoft.Json;

namespace Blog_Manager.Services
{
    /// <summary>
    /// 后端服务器配置项
    /// </summary>
    public class BackendServer
    {
        public string Name { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public bool IsDefault { get; set; }
        public bool IsCustom { get; set; }
        public DateTime? LastTestedAt { get; set; }
        public bool? LastTestResult { get; set; }

        public string DisplayName => $"{Name} ({Url})";
    }

    /// <summary>
    /// 后端配置服务
    /// 管理多个后端服务器地址
    /// </summary>
    public class BackendConfigService
    {
        private const string BACKEND_LIST_KEY = "backend_servers";
        private const string CURRENT_BACKEND_KEY = "current_backend_url";

        private readonly ObservableCollection<BackendServer> _backends;

        public ObservableCollection<BackendServer> Backends => _backends;
        public string? CurrentBackendUrl { get; private set; }

        public event EventHandler<string>? CurrentBackendChanged;

        public BackendConfigService()
        {
            _backends = new ObservableCollection<BackendServer>();
            LoadBackends();
        }

        /// <summary>
        /// 加载后端列表
        /// </summary>
        private void LoadBackends()
        {
            _backends.Clear();

            // 加载默认后端
            _backends.Add(new BackendServer
            {
                Name = "本地开发环境",
                Url = "http://localhost:8080",
                IsDefault = true,
                IsCustom = false
            });

            // 从本地存储加载自定义后端
            var backendsJson = SettingsHelper.GetString(BACKEND_LIST_KEY);
            if (!string.IsNullOrEmpty(backendsJson))
            {
                try
                {
                    var customBackends = JsonConvert.DeserializeObject<List<BackendServer>>(backendsJson ?? "[]");
                    if (customBackends != null)
                    {
                        foreach (var backend in customBackends)
                        {
                            backend.IsCustom = true;
                            _backends.Add(backend);
                        }
                    }
                }
                catch
                {
                    // 忽略加载错误
                }
            }

            // 加载当前选中的后端
            CurrentBackendUrl = SettingsHelper.GetString(CURRENT_BACKEND_KEY);

            // 如果没有选中的后端，使用第一个默认后端
            if (string.IsNullOrEmpty(CurrentBackendUrl) && _backends.Count > 0)
            {
                CurrentBackendUrl = _backends[0].Url;
            }
        }

        /// <summary>
        /// 保存后端列表
        /// </summary>
        private void SaveBackends()
        {
            var customBackends = _backends.Where(b => b.IsCustom).ToList();
            var json = JsonConvert.SerializeObject(customBackends);
            SettingsHelper.SetValue(BACKEND_LIST_KEY, json);
        }

        /// <summary>
        /// 添加自定义后端
        /// </summary>
        public void AddCustomBackend(string name, string url)
        {
            // 检查是否已存在
            if (_backends.Any(b => b.Url.Equals(url, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException("该后端地址已存在");
            }

            var backend = new BackendServer
            {
                Name = name,
                Url = url,
                IsDefault = false,
                IsCustom = true
            };

            _backends.Add(backend);
            SaveBackends();
        }

        /// <summary>
        /// 移除自定义后端
        /// </summary>
        public void RemoveCustomBackend(string url)
        {
            var backend = _backends.FirstOrDefault(b => b.Url.Equals(url, StringComparison.OrdinalIgnoreCase) && b.IsCustom);
            if (backend != null)
            {
                _backends.Remove(backend);
                SaveBackends();

                // 如果删除的是当前选中的后端，切换到默认后端
                if (CurrentBackendUrl == url)
                {
                    SetCurrentBackend(_backends.FirstOrDefault(b => b.IsDefault)?.Url ?? string.Empty);
                }
            }
        }

        /// <summary>
        /// 设置当前后端
        /// </summary>
        public void SetCurrentBackend(string url)
        {
            if (_backends.Any(b => b.Url.Equals(url, StringComparison.OrdinalIgnoreCase)))
            {
                CurrentBackendUrl = url;
                SettingsHelper.SetValue(CURRENT_BACKEND_KEY, url);
                CurrentBackendChanged?.Invoke(this, url);
            }
            else
            {
                throw new InvalidOperationException("后端地址不存在");
            }
        }

        /// <summary>
        /// 测试后端连接
        /// </summary>
        public async Task<(bool Success, string Message)> TestBackendAsync(string url)
        {
            try
            {
                using var client = new HttpClient
                {
                    Timeout = TimeSpan.FromSeconds(5)
                };

                // 测试公告接口（无需认证）
                var testUrl = url.TrimEnd('/') + "/api/announcements/active";
                var response = await client.GetAsync(testUrl);

                var content = await response.Content.ReadAsStringAsync();

                // 检查是否返回了预期的JSON结构
                if (content.Contains("\"code\"") || content.Contains("\"data\""))
                {
                    // 更新测试结果
                    var backend = _backends.FirstOrDefault(b => b.Url.Equals(url, StringComparison.OrdinalIgnoreCase));
                    if (backend != null)
                    {
                        backend.LastTestedAt = DateTime.Now;
                        backend.LastTestResult = true;
                        if (backend.IsCustom)
                        {
                            SaveBackends();
                        }
                    }

                    return (true, "连接成功");
                }

                return (false, "返回数据格式不正确");
            }
            catch (TaskCanceledException)
            {
                return (false, "连接超时");
            }
            catch (HttpRequestException ex)
            {
                return (false, $"连接失败: {ex.Message}");
            }
            catch (Exception ex)
            {
                return (false, $"测试失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取当前后端
        /// </summary>
        public BackendServer? GetCurrentBackend()
        {
            return _backends.FirstOrDefault(b => b.Url.Equals(CurrentBackendUrl, StringComparison.OrdinalIgnoreCase));
        }
    }
}
