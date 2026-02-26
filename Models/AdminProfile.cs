using Newtonsoft.Json;
using System;

namespace Blog_Manager.Models
{
    /// <summary>
    /// 管理员个人信息模型
    /// </summary>
    public class AdminProfile
    {
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("username")]
        public string Username { get; set; } = string.Empty;

        [JsonProperty("nickname")]
        public string? Nickname { get; set; }

        [JsonProperty("email")]
        public string? Email { get; set; }

        [JsonProperty("avatar")]
        public string? Avatar { get; set; }

        [JsonProperty("status")]
        public int Status { get; set; }

        [JsonProperty("lastLoginAt")]
        public DateTime? LastLoginAt { get; set; }

        [JsonProperty("lastLoginIp")]
        public string? LastLoginIp { get; set; }

        [JsonProperty("createdAt")]
        public DateTime CreatedAt { get; set; }

        [JsonProperty("updatedAt")]
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// 管理员信息更新请求
    /// </summary>
    public class AdminProfileUpdateRequest
    {
        [JsonProperty("nickname")]
        public string? Nickname { get; set; }

        [JsonProperty("email")]
        public string? Email { get; set; }

        [JsonProperty("avatar")]
        public string? Avatar { get; set; }
    }

    /// <summary>
    /// 密码修改请求
    /// </summary>
    public class PasswordChangeRequest
    {
        [JsonProperty("oldPassword")]
        public string OldPassword { get; set; } = string.Empty;

        [JsonProperty("newPassword")]
        public string NewPassword { get; set; } = string.Empty;

        [JsonProperty("confirmPassword")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
