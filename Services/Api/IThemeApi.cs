using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Blog_Manager.Models;
using Refit;

namespace Blog_Manager.Services.Api
{
    /// <summary>
    /// 主题API接口
    /// </summary>
    public interface IThemeApi
    {
        /// <summary>
        /// 获取所有主题列表
        /// </summary>
        [Get("/api/admin/themes")]
        [Headers("Authorization: Bearer")]
        Task<ApiResult<List<Theme>>> GetThemesAsync();

        /// <summary>
        /// 获取主题详情
        /// </summary>
        [Get("/api/admin/themes/{id}")]
        [Headers("Authorization: Bearer")]
        Task<ApiResult<Theme>> GetThemeByIdAsync(long id);

        /// <summary>
        /// 创建主题
        /// </summary>
        [Multipart]
        [Post("/api/admin/themes")]
        [Headers("Authorization: Bearer")]
        Task<ApiResult<long>> CreateThemeAsync(
            [AliasAs("name")] string name,
            [AliasAs("description")] string? description,
            [AliasAs("author")] string? author,
            [AliasAs("version")] string? version,
            [AliasAs("displayOrder")] int? displayOrder,
            [AliasAs("coverImage")] string? coverImage,
            [AliasAs("lightCss")] StreamPart lightCss,
            [AliasAs("darkCss")] StreamPart darkCss);

        /// <summary>
        /// 更新主题
        /// </summary>
        [Put("/api/admin/themes/{id}")]
        [Headers("Authorization: Bearer")]
        Task<ApiResult<object>> UpdateThemeAsync(long id, [Body] ThemeUpdateRequest request);

        /// <summary>
        /// 删除主题
        /// </summary>
        [Delete("/api/admin/themes/{id}")]
        [Headers("Authorization: Bearer")]
        Task<ApiResult<object>> DeleteThemeAsync(long id);

        /// <summary>
        /// 应用/取消应用主题
        /// </summary>
        [Put("/api/admin/themes/{id}/toggle")]
        [Headers("Authorization: Bearer")]
        Task<ApiResult<object>> ToggleThemeApplicationAsync(long id, [Query] bool isApplied);

        /// <summary>
        /// 上传主题封面
        /// </summary>
        [Multipart]
        [Post("/api/admin/themes/{id}/cover")]
        [Headers("Authorization: Bearer")]
        Task<ApiResult<string>> UploadThemeCoverAsync(long id, [AliasAs("file")] StreamPart file);

        /// <summary>
        /// 导出主题
        /// </summary>
        [Get("/api/admin/themes/{id}/export")]
        [Headers("Authorization: Bearer")]
        Task<HttpResponseMessage> ExportThemeAsync(long id);

        /// <summary>
        /// 更新主题文件
        /// </summary>
        [Multipart]
        [Put("/api/admin/themes/{id}/files")]
        [Headers("Authorization: Bearer")]
        Task<ApiResult<object>> UpdateThemeFilesAsync(
            long id,
            [AliasAs("lightCss")] StreamPart? lightCss,
            [AliasAs("darkCss")] StreamPart? darkCss);
    }
}
