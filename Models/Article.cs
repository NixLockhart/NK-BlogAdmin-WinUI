using Microsoft.UI.Xaml;
using Newtonsoft.Json;
using System;

namespace Blog_Manager.Models
{
    /// <summary>
    /// 文章模型（匹配后端ArticleListResponse）
    /// </summary>
    public class Article
    {
        // 应用级别的缓存版本号，用于破坏图片缓存
        private static long _globalCacheVersion = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        /// <summary>
        /// 刷新全局缓存版本，强制所有图片重新加载
        /// </summary>
        public static void RefreshGlobalCache()
        {
            _globalCacheVersion = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; } = string.Empty;

        [JsonProperty("summary")]
        public string? Summary { get; set; }

        [JsonProperty("coverImage")]
        public string? CoverImage { get; set; }

        [JsonProperty("categoryId")]
        public long? CategoryId { get; set; }

        [JsonProperty("categoryName")]
        public string? CategoryName { get; set; }

        [JsonProperty("views")]
        public long Views { get; set; }

        [JsonProperty("likes")]
        public int Likes { get; set; }

        [JsonProperty("commentCount")]
        public int CommentCount { get; set; }

        [JsonProperty("isTop")]
        public int IsTop { get; set; }

        [JsonProperty("status")]
        public int Status { get; set; }

        [JsonProperty("publishedAt")]
        public DateTime? PublishedAt { get; set; }

        [JsonProperty("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [JsonProperty("deletedAt")]
        public DateTime? DeletedAt { get; set; }

        [JsonProperty("updatedAt")]
        public DateTime? UpdatedAt { get; set; }

        [JsonProperty("content")]
        public string? Content { get; set; }

        [JsonProperty("markdownContent")]
        public string? MarkdownContent { get; set; }

        [JsonProperty("toc")]
        public string? Toc { get; set; }

        // 辅助属性
        public string StatusText
        {
            get
            {
                try
                {
                    return Status switch
                    {
                        1 => "已发布",
                        2 => "草稿",
                        0 => "已删除",
                        _ => "未知"
                    };
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"StatusText异常: {ex.Message}");
                    return "未知";
                }
            }
        }

        public bool IsTopBool
        {
            get
            {
                try
                {
                    return IsTop == 1;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"IsTopBool异常: {ex.Message}");
                    return false;
                }
            }
        }

        public Visibility IsTopVisibility
        {
            get
            {
                try
                {
                    return IsTop == 1 ? Visibility.Visible : Visibility.Collapsed;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"IsTopVisibility异常: {ex.Message}");
                    return Visibility.Collapsed;
                }
            }
        }

        public string CreatedAtText
        {
            get
            {
                try
                {
                    return CreatedAt.ToString("yyyy-MM-dd HH:mm");
                }
                catch
                {
                    return string.Empty;
                }
            }
        }

        public string SafeSummary
        {
            get
            {
                try
                {
                    return Summary ?? string.Empty;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"SafeSummary异常: {ex.Message}");
                    return string.Empty;
                }
            }
        }

        public string ViewsText
        {
            get
            {
                try
                {
                    return Views.ToString();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"ViewsText异常: {ex.Message}");
                    return "0";
                }
            }
        }

        public string LikesText
        {
            get
            {
                try
                {
                    return Likes.ToString();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"LikesText异常: {ex.Message}");
                    return "0";
                }
            }
        }

        public string CommentCountText
        {
            get
            {
                try
                {
                    return CommentCount.ToString();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"CommentCountText异常: {ex.Message}");
                    return "0";
                }
            }
        }

        public string SafeCategoryName
        {
            get
            {
                try
                {
                    return CategoryName ?? "未分类";
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"SafeCategoryName异常: {ex.Message}");
                    return "未分类";
                }
            }
        }

        // 是否有封面图片
        public bool HasCoverImage
        {
            get
            {
                try
                {
                    return !string.IsNullOrWhiteSpace(CoverImage);
                }
                catch
                {
                    return false;
                }
            }
        }

        // 完整的封面图片 URL（用于显示）
        public string CoverImageUrl
        {
            get
            {
                try
                {
                    var baseUrl = Helpers.AppContext.GetFileUrl(CoverImage);

                    // 使用全局缓存版本号破坏缓存
                    if (!string.IsNullOrEmpty(baseUrl))
                    {
                        var urlWithTimestamp = baseUrl.Contains("?")
                            ? $"{baseUrl}&v={_globalCacheVersion}"
                            : $"{baseUrl}?v={_globalCacheVersion}";
                        return urlWithTimestamp;
                    }

                    return baseUrl;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"CoverImageUrl转换失败: {ex.Message}");
                    return string.Empty;
                }
            }
        }

        // 是否可编辑（已删除的文章不可编辑）
        public bool IsEditable
        {
            get
            {
                try
                {
                    return Status != 0; // status=0表示已删除，不可编辑
                }
                catch
                {
                    return true; // 出错时默认可编辑
                }
            }
        }

        // 是否是已删除状态
        public bool IsDeleted
        {
            get
            {
                try
                {
                    return Status == 0;
                }
                catch
                {
                    return false;
                }
            }
        }

        // 是否是已删除状态（Visibility版本）
        public Visibility IsDeletedVisibility
        {
            get
            {
                try
                {
                    return Status == 0 ? Visibility.Visible : Visibility.Collapsed;
                }
                catch
                {
                    return Visibility.Collapsed;
                }
            }
        }

        // 是否不是已删除状态（Visibility版本）
        public Visibility IsNotDeletedVisibility
        {
            get
            {
                try
                {
                    return Status != 0 ? Visibility.Visible : Visibility.Collapsed;
                }
                catch
                {
                    return Visibility.Visible;
                }
            }
        }

        // 距离永久删除的剩余天数
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

        // 永久删除倒计时文本
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

        // 是否显示倒计时（仅已删除文章且有 deletedAt 时显示）
        public Visibility DeletionCountdownVisibility
        {
            get
            {
                return (Status == 0 && DeletedAt != null) ? Visibility.Visible : Visibility.Collapsed;
            }
        }
    }

    /// <summary>
    /// 文章保存请求（用于创建和更新）
    /// </summary>
    public class ArticleSaveRequest
    {
        [JsonProperty("title")]
        public string Title { get; set; } = string.Empty;

        [JsonProperty("content")]
        public string Content { get; set; } = string.Empty;

        [JsonProperty("summary")]
        public string? Summary { get; set; }

        [JsonProperty("coverImage")]
        public string? CoverImage { get; set; }

        [JsonProperty("categoryId")]
        public long? CategoryId { get; set; }

        [JsonProperty("status")]
        public int Status { get; set; } = 2; // 默认草稿
    }
}
