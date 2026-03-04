using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using Newtonsoft.Json;
using System;

namespace Blog_Manager.Models
{
    /// <summary>
    /// 留言模型 (匹配 MessageResponse)
    /// </summary>
    public partial class Message : ObservableObject
    {
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("nickname")]
        public string Nickname { get; set; } = string.Empty;

        [JsonProperty("email")]
        public string? Email { get; set; }

        [JsonProperty("blogUrl")]
        public string? BlogUrl { get; set; }

        [JsonProperty("avatar")]
        public string? Avatar { get; set; }

        [JsonProperty("content")]
        public string Content { get; set; } = string.Empty;

        [JsonProperty("isFriendLink")]
        public int IsFriendLink { get; set; }

        [JsonProperty("status")]
        public int Status { get; set; } = 1; // 0=已删除, 1=已显示

        [JsonProperty("ipAddress")]
        public string? IpAddress { get; set; }

        [JsonProperty("createdAt")]
        public DateTime CreatedAt { get; set; }

        [JsonProperty("deletedAt")]
        public DateTime? DeletedAt { get; set; }

        // UI helper properties
        public bool IsFriend => IsFriendLink == 1;

        public bool IsDeleted => Status == 0;

        public Visibility IsFriendVisibility => IsFriend ? Visibility.Visible : Visibility.Collapsed;

        public Visibility IsDeletedVisibility => IsDeleted ? Visibility.Visible : Visibility.Collapsed;

        public Visibility NotDeletedVisibility => IsDeleted ? Visibility.Collapsed : Visibility.Visible;

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

        public string FriendLinkButtonText => IsFriend ? "取消友链" : "设为友链";

        // Safe property for displaying email (returns "未填写" if null or empty)
        public string DisplayEmail => string.IsNullOrWhiteSpace(Email) ? "未填写" : Email;

        // Safe property for displaying blog URL as text
        public string DisplayBlogUrl => BlogUrl ?? string.Empty;

        // Safe property for NavigateUri - returns null if BlogUrl is empty/invalid
        public Uri? BlogUri
        {
            get
            {
                if (string.IsNullOrWhiteSpace(BlogUrl))
                    return null;

                try
                {
                    return new Uri(BlogUrl);
                }
                catch
                {
                    return null;
                }
            }
        }

        // Visibility for blog URL section
        public Visibility BlogUrlVisibility => string.IsNullOrWhiteSpace(BlogUrl) ? Visibility.Collapsed : Visibility.Visible;

        // Visibility for friend link button (only show if BlogUrl is not empty)
        public Visibility FriendLinkButtonVisibility => string.IsNullOrWhiteSpace(BlogUrl) ? Visibility.Collapsed : Visibility.Visible;

        // Formatted created time for display
        public string DisplayCreatedAt => CreatedAt.ToString("yyyy-MM-dd HH:mm");

        /// <summary>
        /// IP属地（从API获取）
        /// </summary>
        [ObservableProperty]
        private string _ipLocation = "查询中...";
    }
}
