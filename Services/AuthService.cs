using Blog_Manager.Models;
using Blog_Manager.Services.Api;
using System;
using System.Threading.Tasks;
using Windows.Storage;

namespace Blog_Manager.Services
{
    /// <summary>
    /// 认证服务
    /// </summary>
    public class AuthService
    {
        private const string TOKEN_KEY = "auth_token";
        private const string USERNAME_KEY = "auth_username";
        private const string NICKNAME_KEY = "auth_nickname";

        private readonly ApplicationDataContainer _localSettings;
        private readonly ApiServiceFactory _apiServiceFactory;

        public string? CurrentToken { get; private set; }
        public string? CurrentUsername { get; private set; }
        public string? CurrentNickname { get; private set; }
        public bool IsAuthenticated => !string.IsNullOrEmpty(CurrentToken);

        public event EventHandler<bool>? AuthenticationStateChanged;

        public AuthService(ApiServiceFactory apiServiceFactory)
        {
            _apiServiceFactory = apiServiceFactory;
            _localSettings = ApplicationData.Current.LocalSettings;
            LoadStoredCredentials();
        }

        /// <summary>
        /// 登录
        /// </summary>
        public async Task<(bool Success, string Message)> LoginAsync(string username, string password)
        {
            try
            {
                var request = new LoginRequest
                {
                    Username = username,
                    Password = password
                };

                var authApi = _apiServiceFactory.CreateAuthApi();
                var result = await authApi.LoginAsync(request);

                if (result.IsSuccess && result.Data != null)
                {
                    CurrentToken = result.Data.AccessToken;
                    CurrentUsername = result.Data.Username;
                    CurrentNickname = result.Data.Nickname;

                    // 保存到本地存储
                    _localSettings.Values[TOKEN_KEY] = CurrentToken;
                    _localSettings.Values[USERNAME_KEY] = CurrentUsername;
                    _localSettings.Values[NICKNAME_KEY] = CurrentNickname;

                    AuthenticationStateChanged?.Invoke(this, true);
                    return (true, "登录成功");
                }

                return (false, result.Message ?? "登录失败");
            }
            catch (Exception ex)
            {
                return (false, $"登录失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 登出
        /// </summary>
        public async Task LogoutAsync()
        {
            try
            {
                if (IsAuthenticated)
                {
                    var authApi = _apiServiceFactory.CreateAuthApi();
                    await authApi.LogoutAsync();
                }
            }
            catch
            {
                // 忽略登出错误
            }
            finally
            {
                ClearCredentials();
                AuthenticationStateChanged?.Invoke(this, false);
            }
        }

        /// <summary>
        /// 获取当前用户信息
        /// </summary>
        public async Task<User?> GetCurrentUserAsync()
        {
            if (!IsAuthenticated)
                return null;

            try
            {
                var authApi = _apiServiceFactory.CreateAuthApi();
                var result = await authApi.GetCurrentUserAsync();
                return result.IsSuccess ? result.Data : null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 加载存储的凭证
        /// </summary>
        private void LoadStoredCredentials()
        {
            if (_localSettings.Values.TryGetValue(TOKEN_KEY, out var token))
            {
                CurrentToken = token as string;
            }

            if (_localSettings.Values.TryGetValue(USERNAME_KEY, out var username))
            {
                CurrentUsername = username as string;
            }

            if (_localSettings.Values.TryGetValue(NICKNAME_KEY, out var nickname))
            {
                CurrentNickname = nickname as string;
            }
        }

        /// <summary>
        /// 清除凭证
        /// </summary>
        private void ClearCredentials()
        {
            CurrentToken = null;
            CurrentUsername = null;
            CurrentNickname = null;

            _localSettings.Values.Remove(TOKEN_KEY);
            _localSettings.Values.Remove(USERNAME_KEY);
            _localSettings.Values.Remove(NICKNAME_KEY);
        }
    }
}
