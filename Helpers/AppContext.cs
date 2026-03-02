using Microsoft.UI.Xaml;

namespace Blog_Manager.Helpers
{
    /// <summary>
    /// 应用上下文辅助类
    /// 提供全局访问应用服务和配置的便捷方法
    /// </summary>
    public static class AppContext
    {
        /// <summary>
        /// 获取当前应用实例
        /// </summary>
        private static App? CurrentApp => Application.Current as App;

        /// <summary>
        /// 获取当前后端服务器URL
        /// </summary>
        public static string GetCurrentBackendUrl()
        {
            return CurrentApp?.ApiServiceFactory?.CurrentBaseUrl ?? "http://localhost:8080";
        }

        /// <summary>
        /// 获取完整的文件URL
        /// </summary>
        /// <param name="path">相对路径</param>
        /// <returns>完整URL</returns>
        public static string GetFileUrl(string? path)
        {
            if (string.IsNullOrEmpty(path))
                return string.Empty;

            // 如果已经是完整URL，直接返回
            if (path.StartsWith("http://", System.StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("https://", System.StringComparison.OrdinalIgnoreCase))
                return path;

            var baseUrl = GetCurrentBackendUrl();

            // 确保路径以 / 开头
            var normalizedPath = path.StartsWith("/") ? path : "/" + path;

            // 如果已经包含 /files 前缀，直接拼接
            if (normalizedPath.StartsWith("/files", System.StringComparison.OrdinalIgnoreCase))
                return baseUrl + normalizedPath;

            // 否则添加 /files 前缀
            return baseUrl + "/files" + normalizedPath;
        }

        /// <summary>
        /// 获取完整的API URL
        /// </summary>
        /// <param name="apiPath">API路径</param>
        /// <returns>完整URL</returns>
        public static string GetApiUrl(string apiPath)
        {
            var baseUrl = GetCurrentBackendUrl();
            var normalizedPath = apiPath.StartsWith("/") ? apiPath : "/" + apiPath;
            return baseUrl + normalizedPath;
        }

        /// <summary>
        /// 从 URL 路径中提取相对路径（去掉 /files/ 前缀）。
        /// 与后端 ImageUrlService.toRelativePath 逻辑对应。
        /// 例如："/files/images/covers/1.jpg" → "images/covers/1.jpg"
        ///       "images/covers/1.jpg" → "images/covers/1.jpg"（已是相对路径，原样返回）
        ///       "http://host/files/images/covers/1.jpg" → "images/covers/1.jpg"
        /// </summary>
        public static string ToRelativePath(string? pathOrUrl)
        {
            if (string.IsNullOrEmpty(pathOrUrl))
                return string.Empty;

            var path = pathOrUrl;

            // 完整 URL → 提取路径部分
            if (path.StartsWith("http://", System.StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("https://", System.StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var uri = new System.Uri(path);
                    path = uri.AbsolutePath;
                }
                catch
                {
                    return path;
                }
            }

            // 去掉查询参数
            var queryIndex = path.IndexOf('?');
            if (queryIndex >= 0)
                path = path.Substring(0, queryIndex);

            // 去掉 /files/ 前缀
            if (path.StartsWith("/files/", System.StringComparison.OrdinalIgnoreCase))
                path = path.Substring("/files/".Length);
            else if (path.StartsWith("/files", System.StringComparison.OrdinalIgnoreCase))
                path = path.Substring("/files".Length).TrimStart('/');

            // 去掉前导斜杠
            return path.TrimStart('/');
        }
    }
}
