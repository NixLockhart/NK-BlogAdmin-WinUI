using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Blog_Manager.Models
{
    /// <summary>
    /// 评论模型 (匹配 AdminCommentResponse)
    /// </summary>
    public partial class Comment : ObservableObject
    {
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("articleId")]
        public long ArticleId { get; set; }

        [JsonProperty("articleTitle")]
        public string? ArticleTitle { get; set; }

        [JsonProperty("parentId")]
        public long? ParentId { get; set; }

        [JsonProperty("replyToNickname")]
        public string? ReplyToNickname { get; set; }

        [JsonProperty("nickname")]
        public string Nickname { get; set; } = string.Empty;

        [JsonProperty("email")]
        public string? Email { get; set; }

        [JsonProperty("website")]
        public string? Website { get; set; }

        [JsonProperty("avatar")]
        public string? Avatar { get; set; }

        [JsonProperty("content")]
        public string Content { get; set; } = string.Empty;

        [JsonProperty("ipAddress")]
        public string? IpAddress { get; set; }

        [JsonProperty("userAgent")]
        public string? UserAgent { get; set; }

        [JsonProperty("status")]
        public int Status { get; set; }

        [JsonProperty("createdAt")]
        public DateTime CreatedAt { get; set; }

        [JsonProperty("deletedAt")]
        public DateTime? DeletedAt { get; set; }

        [JsonProperty("children")]
        public List<Comment> Children { get; set; } = new();

        // UI helper properties
        public string StatusText => Status switch
        {
            1 => "已审核",
            2 => "待审核",
            _ => "未知"
        };

        public bool IsApproved => Status == 1;

        public bool IsDeleted => Status == 0;

        public Visibility IsDeletedVisibility => Status == 0 ? Visibility.Visible : Visibility.Collapsed;

        public Visibility IsNotDeletedVisibility => Status != 0 ? Visibility.Visible : Visibility.Collapsed;

        public int DaysUntilPermanentDeletion
        {
            get
            {
                if (Status != 0 || DeletedAt == null) return -1;
                var deadline = DeletedAt.Value.AddDays(30);
                var remaining = (deadline - DateTime.Now).Days;
                return remaining < 0 ? 0 : remaining;
            }
        }

        public string DeletionCountdownText
        {
            get
            {
                var days = DaysUntilPermanentDeletion;
                if (days < 0) return string.Empty;
                if (days == 0) return "今天将被永久删除";
                return $"{days}天后永久删除";
            }
        }

        public Visibility DeletionCountdownVisibility =>
            (Status == 0 && DeletedAt != null) ? Visibility.Visible : Visibility.Collapsed;

        /// <summary>
        /// 安全获取文章标题
        /// </summary>
        public string SafeArticleTitle => ArticleTitle ?? "未知文章";

        /// <summary>
        /// IP属地（从API获取）
        /// </summary>
        [ObservableProperty]
        private string _ipLocation = "查询中...";

        /// <summary>
        /// 设备信息（解析User-Agent）
        /// </summary>
        public string DeviceInfo
        {
            get
            {
                if (string.IsNullOrEmpty(UserAgent))
                    return "未知设备";

                var ua = UserAgent.ToLower();

                // 解析操作系统
                string os = "未知系统";
                if (ua.Contains("windows nt 10"))
                    os = "Windows 10/11";
                else if (ua.Contains("windows nt 6.3"))
                    os = "Windows 8.1";
                else if (ua.Contains("windows nt 6.2"))
                    os = "Windows 8";
                else if (ua.Contains("windows nt 6.1"))
                    os = "Windows 7";
                else if (ua.Contains("windows"))
                    os = "Windows";
                else if (ua.Contains("mac os x"))
                    os = "macOS";
                else if (ua.Contains("iphone"))
                    os = "iPhone";
                else if (ua.Contains("ipad"))
                    os = "iPad";
                else if (ua.Contains("android"))
                    os = "Android";
                else if (ua.Contains("linux"))
                    os = "Linux";

                // 解析浏览器
                string browser = "未知浏览器";
                if (ua.Contains("edg/"))
                    browser = "Edge";
                else if (ua.Contains("chrome") && !ua.Contains("edg"))
                    browser = "Chrome";
                else if (ua.Contains("firefox"))
                    browser = "Firefox";
                else if (ua.Contains("safari") && !ua.Contains("chrome"))
                    browser = "Safari";
                else if (ua.Contains("opera") || ua.Contains("opr/"))
                    browser = "Opera";

                return $"{os} · {browser}";
            }
        }

        /// <summary>
        /// 格式化的创建时间
        /// </summary>
        public string CreatedAtText => CreatedAt.ToString("yyyy-MM-dd HH:mm");
    }
}
