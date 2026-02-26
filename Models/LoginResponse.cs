using Newtonsoft.Json;

namespace Blog_Manager.Models
{
    /// <summary>
    /// 登录响应模型 (匹配后端LoginResponse)
    /// </summary>
    public class LoginResponse
    {
        [JsonProperty("accessToken")]
        public string AccessToken { get; set; } = string.Empty;

        [JsonProperty("tokenType")]
        public string TokenType { get; set; } = "Bearer";

        [JsonProperty("username")]
        public string Username { get; set; } = string.Empty;

        [JsonProperty("nickname")]
        public string Nickname { get; set; } = string.Empty;
    }
}
