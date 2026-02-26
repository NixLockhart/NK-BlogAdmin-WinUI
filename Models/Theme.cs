using System;

namespace Blog_Manager.Models
{
    /// <summary>
    /// 主题模型
    /// </summary>
    public class Theme
    {
        /// <summary>
        /// 主题ID
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// 主题名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 主题标识
        /// </summary>
        public string Slug { get; set; } = string.Empty;

        /// <summary>
        /// 主题描述
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// 作者
        /// </summary>
        public string? Author { get; set; }

        /// <summary>
        /// 版本号
        /// </summary>
        public string? Version { get; set; }

        /// <summary>
        /// 主题文件夹路径
        /// </summary>
        public string ThemePath { get; set; } = string.Empty;

        /// <summary>
        /// 封面图片URL
        /// </summary>
        public string? CoverUrl { get; set; }

        /// <summary>
        /// 是否应用到博客
        /// </summary>
        public bool IsApplied { get; set; }

        /// <summary>
        /// 是否为默认主题（不可删除）
        /// </summary>
        public bool IsDefault { get; set; }

        /// <summary>
        /// 显示顺序
        /// </summary>
        public int DisplayOrder { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// 更新时间
        /// </summary>
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// 主题创建请求
    /// </summary>
    public class ThemeCreateRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Author { get; set; }
        public string? Version { get; set; }
        public string? CoverImage { get; set; }
        public int? DisplayOrder { get; set; }
    }
}
