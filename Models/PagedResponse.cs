using Newtonsoft.Json;
using System.Collections.Generic;

namespace Blog_Manager.Models
{
    /// <summary>
    /// 分页响应模型
    /// </summary>
    public class PagedResponse<T>
    {
        [JsonProperty("content")]
        public List<T> Content { get; set; } = new();

        [JsonProperty("total")]
        public long TotalElements { get; set; }

        [JsonProperty("totalPages")]
        public int TotalPages { get; set; }

        [JsonProperty("size")]
        public int Size { get; set; }

        [JsonProperty("page")]
        public int Number { get; set; }

        [JsonProperty("first")]
        public bool First { get; set; }

        [JsonProperty("last")]
        public bool Last { get; set; }

        [JsonProperty("hasPrevious")]
        public bool HasPrevious { get; set; }

        [JsonProperty("hasNext")]
        public bool HasNext { get; set; }
    }
}
