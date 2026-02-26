using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Blog_Manager.Services
{
    /// <summary>
    /// HTTP客户端处理器 - 自动添加Authorization头
    /// </summary>
    public class AuthorizingHttpClientHandler : DelegatingHandler
    {
        private readonly Func<string?> _getAuthToken;

        public AuthorizingHttpClientHandler(Func<string?> getAuthToken)
        {
            _getAuthToken = getAuthToken;
            InnerHandler = new HttpClientHandler();
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            // 如果已认证，自动添加Authorization头
            var token = _getAuthToken();
            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }

            return await base.SendAsync(request, cancellationToken);
        }
    }
}
