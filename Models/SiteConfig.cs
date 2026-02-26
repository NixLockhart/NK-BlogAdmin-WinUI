using Newtonsoft.Json;

namespace Blog_Manager.Models
{
    /// <summary>
    /// 网站配置模型
    /// </summary>
    public class SiteConfig
    {
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("configKey")]
        public string ConfigKey { get; set; } = string.Empty;

        [JsonProperty("configValue")]
        public string? ConfigValue { get; set; }

        [JsonProperty("configType")]
        public string ConfigType { get; set; } = "TEXT";

        [JsonProperty("description")]
        public string? Description { get; set; }

        [JsonProperty("isPublic")]
        public bool IsPublic { get; set; }

        [JsonProperty("createdAt")]
        public string CreatedAt { get; set; } = string.Empty;

        [JsonProperty("updatedAt")]
        public string UpdatedAt { get; set; } = string.Empty;
    }
}
