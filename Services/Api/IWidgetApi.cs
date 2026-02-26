using Refit;
using Blog_Manager.Models;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Blog_Manager.Services.Api
{
    /// <summary>
    /// 小工具API接口
    /// </summary>
    public interface IWidgetApi
    {
        [Get("/api/admin/widgets")]
        [Headers("Authorization: Bearer")]
        Task<ApiResult<List<Widget>>> GetWidgetsAsync();

        [Get("/api/admin/widgets/{id}")]
        [Headers("Authorization: Bearer")]
        Task<ApiResult<Widget>> GetWidgetByIdAsync(long id);

        [Get("/api/admin/widgets/{id}/code")]
        [Headers("Authorization: Bearer")]
        Task<ApiResult<WidgetCode>> GetWidgetCodeAsync(long id);

        [Post("/api/admin/widgets")]
        [Headers("Authorization: Bearer")]
        Task<ApiResult<long>> CreateWidgetAsync([Body] WidgetCreateRequest request);

        [Put("/api/admin/widgets/{id}")]
        [Headers("Authorization: Bearer")]
        Task<ApiResult<object>> UpdateWidgetAsync(long id, [Body] WidgetUpdateRequest request);

        [Delete("/api/admin/widgets/{id}")]
        [Headers("Authorization: Bearer")]
        Task<ApiResult<object>> DeleteWidgetAsync(long id);

        [Put("/api/admin/widgets/{id}/toggle")]
        [Headers("Authorization: Bearer")]
        Task<ApiResult<object>> ToggleWidgetApplicationAsync(long id, [Query] bool isApplied);

        [Get("/api/admin/widgets/{id}/export")]
        [Headers("Authorization: Bearer")]
        Task<ApiResult<string>> ExportWidgetAsync(long id);
    }
}
