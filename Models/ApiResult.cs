using Newtonsoft.Json;

namespace Blog_Manager.Models
{
    /// <summary>
    /// API统一响应结果
    /// </summary>
    public class ApiResult<T>
    {
        [JsonProperty("code")]
        public int Code { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; } = string.Empty;

        [JsonProperty("data")]
        public T? Data { get; set; }

        [JsonProperty("timestamp")]
        public long Timestamp { get; set; }

        public bool IsSuccess => Code == 200;
    }
}
