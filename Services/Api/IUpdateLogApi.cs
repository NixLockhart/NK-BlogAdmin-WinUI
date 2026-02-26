using Blog_Manager.Models;
using Refit;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Blog_Manager.Services.Api
{
    /// <summary>
    /// 更新日志API接口
    /// </summary>
    public interface IUpdateLogApi
    {
        [Get("/api/admin/update-logs")]
        [Headers("Authorization: Bearer")]
        Task<ApiResult<List<UpdateLog>>> GetUpdateLogsAsync();

        [Post("/api/admin/update-logs")]
        [Headers("Authorization: Bearer")]
        Task<ApiResult<long>> CreateUpdateLogAsync([Body] UpdateLogRequest updateLogRequest);

        [Put("/api/admin/update-logs/{id}")]
        [Headers("Authorization: Bearer")]
        Task<ApiResult<object>> UpdateUpdateLogAsync(long id, [Body] UpdateLogRequest updateLogRequest);

        [Delete("/api/admin/update-logs/{id}")]
        [Headers("Authorization: Bearer")]
        Task<ApiResult<object>> DeleteUpdateLogAsync(long id);
    }
}
