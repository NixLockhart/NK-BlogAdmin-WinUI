using Blog_Manager.Models;
using Refit;
using System.Threading.Tasks;

namespace Blog_Manager.Services.Api
{
    /// <summary>
    /// 认证API接口
    /// </summary>
    public interface IAuthApi
    {
        [Post("/api/auth/login")]
        Task<ApiResult<LoginResponse>> LoginAsync([Body] LoginRequest request);

        [Post("/api/auth/logout")]
        [Headers("Authorization: Bearer")]
        Task<ApiResult<object>> LogoutAsync();

        [Get("/api/auth/me")]
        [Headers("Authorization: Bearer")]
        Task<ApiResult<User>> GetCurrentUserAsync();
    }
}
