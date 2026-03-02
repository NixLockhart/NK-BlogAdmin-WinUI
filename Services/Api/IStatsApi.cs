using Blog_Manager.Models;
using Refit;
using System.Collections.Generic;
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

        [Get("/api/stats/visit-trend")]
        [Headers("Authorization: Bearer")]
        Task<ApiResult<Dictionary<string, long>>> GetVisitTrendAsync([Query] int days = 30);

        [Get("/api/stats/article-ranking")]
        [Headers("Authorization: Bearer")]
        Task<ApiResult<Dictionary<string, long>>> GetArticleRankingAsync([Query] int topN = 10);
    }
}
