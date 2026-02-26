using Newtonsoft.Json;

namespace Blog_Manager.Models
{
    /// <summary>
    /// 登录请求
    /// </summary>
    public class LoginRequest
    {
        [JsonProperty("username")]
        public string Username { get; set; } = string.Empty;

        [JsonProperty("password")]
        public string Password { get; set; } = string.Empty;
    }
}
