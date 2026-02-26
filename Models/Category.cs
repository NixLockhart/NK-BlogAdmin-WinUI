using Newtonsoft.Json;
using System;

namespace Blog_Manager.Models
{
    /// <summary>
    /// 分类模型（匹配后端CategoryResponse）
    /// </summary>
    public class Category
    {
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        [JsonProperty("description")]
        public string? Description { get; set; }

        [JsonProperty("slug")]
        public string? Slug { get; set; }

        [JsonProperty("sortOrder")]
        public int SortOrder { get; set; }

        [JsonProperty("articleCount")]
        public int ArticleCount { get; set; }

        [JsonProperty("createdAt")]
        public DateTime CreatedAt { get; set; }

        [JsonProperty("updatedAt")]
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// 分类保存请求（用于创建和更新）
    /// </summary>
    public class CategorySaveRequest
    {
        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        [JsonProperty("description")]
        public string? Description { get; set; }

        [JsonProperty("slug")]
        public string? Slug { get; set; }

        [JsonProperty("sortOrder")]
        public int SortOrder { get; set; } = 0;
    }
}
