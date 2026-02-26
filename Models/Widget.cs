using System;

namespace Blog_Manager.Models
{
    /// <summary>
    /// 小工具模型
    /// </summary>
    public class Widget
    {
        /// <summary>
        /// 小工具ID
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// 小工具名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 代码文件路径
        /// </summary>
        public string CodePath { get; set; } = string.Empty;

        /// <summary>
        /// 封面图片URL
        /// </summary>
        public string? CoverUrl { get; set; }

        /// <summary>
        /// 是否应用到博客
        /// </summary>
        public bool IsApplied { get; set; }

        /// <summary>
        /// 是否系统自带（不可删除）
        /// </summary>
        public bool IsSystem { get; set; }

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
    /// 小工具代码模型（用于编辑）
    /// </summary>
    public class WidgetCode
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string? CoverUrl { get; set; }
    }

    /// <summary>
    /// 小工具创建请求
    /// </summary>
    public class WidgetCreateRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string? CoverImage { get; set; }
        public int? DisplayOrder { get; set; }
    }

    /// <summary>
    /// 小工具更新请求
    /// </summary>
    public class WidgetUpdateRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string? CoverImage { get; set; }
        public int? DisplayOrder { get; set; }
    }
}
