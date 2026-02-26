using Blog_Manager.Models;
using Refit;
using System.Threading.Tasks;

namespace Blog_Manager.Services.Api
{
    /// <summary>
    /// 文章API接口
    /// </summary>
    public interface IArticleApi
    {
        [Get("/api/admin/articles")]
        [Headers("Authorization: Bearer")]
        Task<ApiResult<PagedResponse<Article>>> GetArticlesAsync(
            [Query] int? page = null,
            [Query] int? size = null,
            [Query] string? keyword = null,
            [Query] long? categoryId = null,
            [Query] string? status = null);

        [Get("/api/admin/articles/{id}")]
        [Headers("Authorization: Bearer")]
        Task<ApiResult<Article>> GetArticleAsync(long id);

        [Post("/api/admin/articles")]
        [Headers("Authorization: Bearer")]
        Task<ApiResult<Article>> CreateArticleAsync([Body] ArticleSaveRequest article);

        [Put("/api/admin/articles/{id}")]
        [Headers("Authorization: Bearer")]
        Task<ApiResult<Article>> UpdateArticleAsync(long id, [Body] ArticleSaveRequest article);

        [Delete("/api/admin/articles/{id}")]
        [Headers("Authorization: Bearer")]
        Task<ApiResult<object>> DeleteArticleAsync(long id);

        [Put("/api/admin/articles/{id}/top")]
        [Headers("Authorization: Bearer")]
        Task<ApiResult<object>> ToggleTopAsync(long id);

        [Put("/api/admin/articles/{id}/publish")]
        [Headers("Authorization: Bearer")]
        Task<ApiResult<Article>> PublishArticleAsync(long id);

        [Put("/api/admin/articles/{id}/draft")]
        [Headers("Authorization: Bearer")]
        Task<ApiResult<Article>> UnpublishArticleAsync(long id);
    }
}
