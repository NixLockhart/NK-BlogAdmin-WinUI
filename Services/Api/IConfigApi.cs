using Blog_Manager.Models;
using Refit;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Blog_Manager.Services.Api
{
    /// <summary>
    /// 网站配置API接口
    /// </summary>
    public interface IConfigApi
    {
        [Get("/api/admin/config")]
        [Headers("Authorization: Bearer")]
        Task<ApiResult<List<SiteConfig>>> GetConfigsAsync();

        [Get("/api/admin/config/{key}")]
        [Headers("Authorization: Bearer")]
        Task<ApiResult<string>> GetConfigAsync(string key);

        [Post("/api/admin/config")]
        [Headers("Authorization: Bearer")]
        Task<ApiResult<SiteConfig>> CreateConfigAsync([Body] ConfigCreateRequest request);

        [Put("/api/admin/config/{key}")]
        [Headers("Authorization: Bearer")]
        Task<ApiResult<object>> UpdateConfigAsync(string key, [Body] string value);

        [Delete("/api/admin/config/{key}")]
        [Headers("Authorization: Bearer")]
        Task<ApiResult<object>> DeleteConfigAsync(string key);
    }

    /// <summary>
    /// 配置创建请求
    /// </summary>
    public class ConfigCreateRequest
    {
        public string ConfigKey { get; set; } = string.Empty;
        public string? ConfigValue { get; set; }
        public string ConfigType { get; set; } = "string";
        public string? Description { get; set; }
        public bool IsPublic { get; set; }
    }
}
