using Blog_Manager.Models;
using Refit;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Blog_Manager.Services.Api
{
    /// <summary>
    /// 公告API接口
    /// </summary>
    public interface IAnnouncementApi
    {
        [Get("/api/admin/announcements")]
        [Headers("Authorization: Bearer")]
        Task<ApiResult<List<Announcement>>> GetAnnouncementsAsync();

        [Post("/api/admin/announcements")]
        [Headers("Authorization: Bearer")]
        Task<ApiResult<long>> CreateAnnouncementAsync([Body] Announcement announcement);

        [Put("/api/admin/announcements/{id}")]
        [Headers("Authorization: Bearer")]
        Task<ApiResult<object>> UpdateAnnouncementAsync(long id, [Body] Announcement announcement);

        [Delete("/api/admin/announcements/{id}")]
        [Headers("Authorization: Bearer")]
        Task<ApiResult<object>> DeleteAnnouncementAsync(long id);
    }
}
