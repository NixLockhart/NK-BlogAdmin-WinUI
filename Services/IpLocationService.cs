using Blog_Manager.Helpers;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Threading.Tasks;

namespace Blog_Manager.Services
{
    /// <summary>
    /// IP属地查询服务
    /// 使用 cn.apihz.cn 的免费API
    /// 需要在设置中配置 API 凭据后才能使用
    /// </summary>
    public class IpLocationService
    {
        private const string ApiUrl = "https://cn.apihz.cn/api/ip/chaapi.php";
        private const string IP_API_USERID_KEY = "ip_api_userid";
        private const string IP_API_USERKEY_KEY = "ip_api_userkey";

        private readonly HttpClient _httpClient;

        // 缓存已查询的IP属地，避免重复请求
        private readonly ConcurrentDictionary<string, string> _locationCache = new();

        /// <summary>
        /// API 凭据是否已配置
        /// </summary>
        public bool IsConfigured
        {
            get
            {
                var (userId, userKey) = GetCredentials();
                return !string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(userKey);
            }
        }

        public IpLocationService()
        {
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(5)
            };
        }

        /// <summary>
        /// 从本地存储读取 API 凭据
        /// </summary>
        private (string? userId, string? userKey) GetCredentials()
        {
            var userId = SettingsHelper.GetString(IP_API_USERID_KEY);
            var userKey = SettingsHelper.GetString(IP_API_USERKEY_KEY);
            return (userId, userKey);
        }

        /// <summary>
        /// 保存 API 凭据到本地存储
        /// </summary>
        public void SetCredentials(string userId, string userKey)
        {
            SettingsHelper.SetValue(IP_API_USERID_KEY, userId);
            SettingsHelper.SetValue(IP_API_USERKEY_KEY, userKey);
        }

        /// <summary>
        /// 查询IP属地
        /// </summary>
        /// <param name="ip">IP地址</param>
        /// <returns>属地文本：国内返回省份，国外返回国家</returns>
        public async Task<string> GetLocationAsync(string ip)
        {
            if (string.IsNullOrWhiteSpace(ip))
                return "未知";

            // 检查缓存
            if (_locationCache.TryGetValue(ip, out var cachedLocation))
                return cachedLocation;

            try
            {
                var (userId, userKey) = GetCredentials();
                if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(userKey))
                {
                    var location = "未配置";
                    _locationCache.TryAdd(ip, location);
                    return location;
                }

                var url = $"{ApiUrl}?id={userId}&key={userKey}&ip={ip}";
                var response = await _httpClient.GetStringAsync(url);

                var result = JsonConvert.DeserializeObject<IpLocationResponse>(response);

                if (result == null || result.Code != 200)
                {
                    var location = "未知";
                    _locationCache.TryAdd(ip, location);
                    return location;
                }

                // 根据国家判断显示内容
                string displayLocation;
                if (result.Guo == "中国")
                {
                    // 国内显示省份
                    displayLocation = FormatProvince(result.Sheng);
                }
                else
                {
                    // 国外显示国家
                    displayLocation = result.Guo ?? "未知";
                }

                _locationCache.TryAdd(ip, displayLocation);
                return displayLocation;
            }
            catch (Exception)
            {
                // 查询失败时返回未知
                var location = "未知";
                _locationCache.TryAdd(ip, location);
                return location;
            }
        }

        /// <summary>
        /// 格式化省份名称（去掉"省"、"市"等后缀）
        /// </summary>
        private static string FormatProvince(string? province)
        {
            if (string.IsNullOrWhiteSpace(province))
                return "未知";

            // 直辖市和特别行政区保持原样
            if (province is "北京" or "上海" or "天津" or "重庆" or "香港" or "澳门" or "台湾")
                return province;

            // 去掉常见后缀
            return province
                .Replace("省", "")
                .Replace("自治区", "")
                .Replace("维吾尔", "")
                .Replace("壮族", "")
                .Replace("回族", "")
                .Trim();
        }

        /// <summary>
        /// 批量查询IP属地
        /// </summary>
        public async Task<ConcurrentDictionary<string, string>> GetLocationsAsync(string[] ips)
        {
            var results = new ConcurrentDictionary<string, string>();

            // 并行查询，但限制并发数
            var tasks = new System.Collections.Generic.List<Task>();
            var semaphore = new System.Threading.SemaphoreSlim(5); // 最多5个并发

            foreach (var ip in ips)
            {
                if (string.IsNullOrWhiteSpace(ip) || results.ContainsKey(ip))
                    continue;

                tasks.Add(Task.Run(async () =>
                {
                    await semaphore.WaitAsync();
                    try
                    {
                        var location = await GetLocationAsync(ip);
                        results.TryAdd(ip, location);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }));
            }

            await Task.WhenAll(tasks);
            return results;
        }
    }

    /// <summary>
    /// IP属地API响应模型
    /// </summary>
    public class IpLocationResponse
    {
        [JsonProperty("code")]
        public int Code { get; set; }

        [JsonProperty("msg")]
        public string? Msg { get; set; }

        [JsonProperty("zhou")]
        public string? Zhou { get; set; }

        [JsonProperty("guo")]
        public string? Guo { get; set; }

        [JsonProperty("sheng")]
        public string? Sheng { get; set; }

        [JsonProperty("shi")]
        public string? Shi { get; set; }

        [JsonProperty("qu")]
        public string? Qu { get; set; }

        [JsonProperty("isp")]
        public string? Isp { get; set; }

        [JsonProperty("ip")]
        public string? Ip { get; set; }

        [JsonProperty("lat")]
        public string? Lat { get; set; }

        [JsonProperty("lon")]
        public string? Lon { get; set; }

        [JsonProperty("guocode")]
        public string? GuoCode { get; set; }
    }
}
