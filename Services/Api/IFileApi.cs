using Blog_Manager.Models;
using Refit;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Blog_Manager.Services.Api
{
    /// <summary>
    /// 文件API接口
    /// </summary>
    public interface IFileApi
    {
        [Multipart]
        [Post("/api/files/upload")]
        [Headers("Authorization: Bearer")]
        Task<ApiResult<string>> UploadFileAsync([AliasAs("file")] StreamPart file);

        /// <summary>
        /// 上传头像（与评论/留言使用相同接口）
        /// </summary>
        [Multipart]
        [Post("/api/files/avatar")]
        Task<ApiResult<Dictionary<string, string>>> UploadAvatarAsync([AliasAs("file")] StreamPart file);
    }
}
