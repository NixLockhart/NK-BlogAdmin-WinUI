using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Blog_Manager.Models
{
    /// <summary>
    /// 图片存储配置
    /// </summary>
    public class ImageStorageConfig
    {
        /// <summary>
        /// 存储模式: "local" 或 "cdn"
        /// </summary>
        [JsonPropertyName("mode")]
        public string Mode { get; set; } = "local";

        /// <summary>
        /// 图床配置
        /// </summary>
        [JsonPropertyName("cdn")]
        public CdnConfig Cdn { get; set; } = new();
    }

    /// <summary>
    /// 图床配置
    /// </summary>
    public class CdnConfig
    {
        [JsonPropertyName("uploadUrl")]
        public string UploadUrl { get; set; } = string.Empty;

        [JsonPropertyName("method")]
        public string Method { get; set; } = "POST";

        [JsonPropertyName("headers")]
        public Dictionary<string, string> Headers { get; set; } = new();

        [JsonPropertyName("fileField")]
        public string FileField { get; set; } = "file";

        [JsonPropertyName("extraParams")]
        public Dictionary<string, string> ExtraParams { get; set; } = new();

        [JsonPropertyName("responseUrlField")]
        public string ResponseUrlField { get; set; } = "data.url";

        [JsonPropertyName("responseDeleteField")]
        public string ResponseDeleteField { get; set; } = string.Empty;

        [JsonPropertyName("deleteUrlTemplate")]
        public string DeleteUrlTemplate { get; set; } = string.Empty;

        [JsonPropertyName("deleteMethod")]
        public string DeleteMethod { get; set; } = "GET";

        [JsonPropertyName("timeout")]
        public int Timeout { get; set; } = 30;

        [JsonPropertyName("maxRetries")]
        public int MaxRetries { get; set; } = 2;
    }
}
