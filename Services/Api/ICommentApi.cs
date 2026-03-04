using Blog_Manager.Models;
using Refit;
using System.Threading.Tasks;

namespace Blog_Manager.Services.Api
{
    /// <summary>
    /// 评论API接口 (匹配 AdminCommentController)
    /// </summary>
    public interface ICommentApi
    {
        [Get("/api/admin/comments")]
        [Headers("Authorization: Bearer")]
        Task<ApiResult<PagedResponse<Comment>>> GetCommentsAsync(
            [Query] int page = 0,
            [Query] int size = 20,
            [Query] long? articleId = null,
            [Query] int? status = null,
            [Query] string? sort = "desc");

        [Put("/api/admin/comments/{id}/status")]
        [Headers("Authorization: Bearer")]
        Task<ApiResult<object>> UpdateCommentStatusAsync(long id, [Query] int status);

        [Delete("/api/admin/comments/{id}")]
        [Headers("Authorization: Bearer")]
        Task<ApiResult<object>> DeleteCommentAsync(long id);

        [Delete("/api/admin/comments/{id}/permanent")]
        [Headers("Authorization: Bearer")]
        Task<ApiResult<object>> PermanentlyDeleteCommentAsync(long id);

        [Put("/api/admin/comments/{id}/restore")]
        [Headers("Authorization: Bearer")]
        Task<ApiResult<object>> RestoreCommentAsync(long id);
    }
}
