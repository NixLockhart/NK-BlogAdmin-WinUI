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
    }
}
