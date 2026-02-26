namespace Blog_Manager.Models
{
    /// <summary>
    /// 更新主题请求DTO
    /// </summary>
    public class ThemeUpdateRequest
    {
        /// <summary>
        /// 主题名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

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
        /// 封面图片（Base64编码，如果为null则不更新）
        /// </summary>
        public string? CoverImage { get; set; }

        /// <summary>
        /// 显示顺序
        /// </summary>
        public int? DisplayOrder { get; set; }
    }
}
