using Microsoft.UI.Xaml;
using Newtonsoft.Json;
using System;

namespace Blog_Manager.Models
{
    /// <summary>
    /// 更新日志模型 (匹配UpdateLogResponse)
    /// </summary>
    public class UpdateLog
    {
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("version")]
        public string Version { get; set; } = string.Empty;

        [JsonProperty("title")]
        public string Title { get; set; } = string.Empty;

        [JsonProperty("content")]
        public string Content { get; set; } = string.Empty;

        [JsonProperty("contentHtml")]
        public string? ContentHtml { get; set; }

        [JsonProperty("isMajor")]
        public int IsMajor { get; set; }

        [JsonProperty("releaseDate")]
        public DateTime ReleaseDate { get; set; }

        [JsonProperty("isPending")]
        public bool IsPending { get; set; }

        [JsonProperty("createdAt")]
        public DateTime CreatedAt { get; set; }

        // UI helper properties
        public bool IsMajorUpdate => IsMajor == 1;
        public Visibility IsMajorUpdateVisibility => IsMajorUpdate ? Visibility.Visible : Visibility.Collapsed;
        public Visibility IsPendingVisibility => IsPending ? Visibility.Visible : Visibility.Collapsed;
        public string UpdateTypeText => IsMajorUpdate ? "重大更新" : "常规更新";
        public string StatusText => IsPending ? "待发布" : "已发布";

        // Formatted dates for display
        public string DisplayCreatedAt => CreatedAt.ToString("yyyy-MM-dd HH:mm");
        public string DisplayReleaseDate => ReleaseDate.ToString("yyyy-MM-dd");
    }
}
