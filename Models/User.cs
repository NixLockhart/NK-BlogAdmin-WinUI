using Newtonsoft.Json;

namespace Blog_Manager.Models
{
    /// <summary>
    /// 用户模型
    /// </summary>
    public class User
    {
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("username")]
        public string Username { get; set; } = string.Empty;

        [JsonProperty("email")]
        public string? Email { get; set; }

        [JsonProperty("avatar")]
        public string? Avatar { get; set; }

        [JsonProperty("role")]
        public string Role { get; set; } = "USER";

        [JsonProperty("createdAt")]
        public string CreatedAt { get; set; } = string.Empty;
    }
}
