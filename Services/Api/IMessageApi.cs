using Blog_Manager.Models;
using Refit;
using System.Threading.Tasks;

namespace Blog_Manager.Services.Api
{
    /// <summary>
    /// 留言API接口 (匹配 AdminMessageController)
    /// </summary>
    public interface IMessageApi
    {
        [Get("/api/admin/messages")]
        [Headers("Authorization: Bearer")]
        Task<ApiResult<PagedResponse<Message>>> GetMessagesAsync(
            [Query] int page = 0,
            [Query] int size = 20);

        [Put("/api/admin/messages/{id}/friend")]
        [Headers("Authorization: Bearer")]
        Task<ApiResult<object>> ToggleFriendLinkAsync(long id);

        [Delete("/api/admin/messages/{id}")]
        [Headers("Authorization: Bearer")]
        Task<ApiResult<object>> DeleteMessageAsync(long id);
    }
}
