using Newtonsoft.Json;
using System;

namespace Blog_Manager.Models
{
    /// <summary>
    /// 仪表板统计信息模型 (Dashboard Statistics)
    /// </summary>
    public class Statistics
    {
        /// <summary>
        /// 网站总访问量
        /// </summary>
        [JsonProperty("totalViews")]
        public long TotalViews { get; set; }

        /// <summary>
        /// 今日访问量
        /// </summary>
        [JsonProperty("todayViews")]
        public long TodayViews { get; set; }

        /// <summary>
        /// 文章总数
        /// </summary>
        [JsonProperty("totalArticles")]
        public long TotalArticles { get; set; }

        /// <summary>
        /// 文章总点赞量
        /// </summary>
        [JsonProperty("totalLikes")]
        public long TotalLikes { get; set; }

        /// <summary>
        /// 评论总数
        /// </summary>
        [JsonProperty("totalComments")]
        public long TotalComments { get; set; }

        /// <summary>
        /// 留言总数
        /// </summary>
        [JsonProperty("totalMessages")]
        public long TotalMessages { get; set; }

        /// <summary>
        /// 网站运行时长（天数）
        /// </summary>
        [JsonProperty("runningDays")]
        public long RunningDays { get; set; }

        /// <summary>
        /// 当前网站版本号
        /// </summary>
        [JsonProperty("version")]
        public string Version { get; set; } = string.Empty;

        /// <summary>
        /// 网站启动时间 (根据runningDays计算)
        /// </summary>
        public DateTime StartTime => DateTime.Now.AddDays(-RunningDays);
    }
}
