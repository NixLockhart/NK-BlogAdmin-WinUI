using Blog_Manager.Models;
using Refit;
using System.Threading.Tasks;

namespace Blog_Manager.Services.Api
{
    /// <summary>
    /// 管理员个人信息API接口
    /// </summary>
    public interface IAdminProfileApi
    {
        /// <summary>
        /// 获取当前管理员信息
        /// </summary>
        [Get("/api/admin/profile")]
        [Headers("Authorization: Bearer")]
        Task<ApiResult<AdminProfile>> GetProfileAsync();

        /// <summary>
        /// 更新管理员信息
        /// </summary>
        [Put("/api/admin/profile")]
        [Headers("Authorization: Bearer")]
        Task<ApiResult<AdminProfile>> UpdateProfileAsync([Body] AdminProfileUpdateRequest request);

        /// <summary>
        /// 修改密码
        /// </summary>
        [Post("/api/admin/profile/password")]
        [Headers("Authorization: Bearer")]
        Task<ApiResult<object>> ChangePasswordAsync([Body] PasswordChangeRequest request);
    }
}
