using Blog_Manager.Models;
using Refit;
using System.Threading.Tasks;

namespace Blog_Manager.Services.Api
{
    /// <summary>
    /// 统计API接口
    /// </summary>
    public interface IStatsApi
    {
        [Get("/api/stats")]
        [Headers("Authorization: Bearer")]
        Task<ApiResult<Statistics>> GetStatisticsAsync();
    }
}
