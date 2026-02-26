using Newtonsoft.Json;
using System;

namespace Blog_Manager.Models
{
    /// <summary>
    /// 公告模型 (匹配AnnouncementResponse)
    /// </summary>
    public class Announcement
    {
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; } = string.Empty;

        [JsonProperty("content")]
        public string Content { get; set; } = string.Empty;

        [JsonProperty("startTime")]
        public DateTime? StartTime { get; set; }

        [JsonProperty("endTime")]
        public DateTime? EndTime { get; set; }

        [JsonProperty("enabled")]
        public int Enabled { get; set; }

        [JsonProperty("createdAt")]
        public DateTime CreatedAt { get; set; }

        // UI helper properties
        public bool IsEnabled => Enabled == 1;
        public string EnabledText => IsEnabled ? "已启用" : "已禁用";
    }
}
