using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Blog_Manager.Services;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Windows.Storage;

namespace Blog_Manager.Views
{
    public sealed partial class SettingsPage : Page, INotifyPropertyChanged
    {
        private readonly AuthService _authService;
        private readonly BackendConfigService _backendConfigService;
        private readonly IpLocationService _ipLocationService;
        private const string ThemeSettingKey = "AppTheme";
        private bool _isLoadingTheme = false;

        public ObservableCollection<BackendServer> BackendServers => _backendConfigService.Backends;

        private string _currentUsername = string.Empty;
        public string CurrentUsername
        {
            get => _currentUsername;
            set
            {
                if (_currentUsername != value)
                {
                    _currentUsername = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _currentNickname = string.Empty;
        public string CurrentNickname
        {
            get => _currentNickname;
            set
            {
                if (_currentNickname != value)
                {
                    _currentNickname = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _currentBackendUrl = string.Empty;
        public string CurrentBackendUrl
        {
            get => _currentBackendUrl;
            set
            {
                if (_currentBackendUrl != value)
                {
                    _currentBackendUrl = value;
                    OnPropertyChanged();
                }
            }
        }

        public SettingsPage()
        {
            this.InitializeComponent();

            try
            {
                var app = Application.Current as App;
                _authService = app?.AuthService ?? throw new InvalidOperationException("AuthService not found");
                _backendConfigService = app?.BackendConfigService ?? throw new InvalidOperationException("BackendConfigService not found");

                _ipLocationService = new IpLocationService();

                LoadUserInfo();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SettingsPage init error: {ex.Message}");
            }

            this.Loaded += SettingsPage_Loaded;
        }

        private void SettingsPage_Loaded(object sender, RoutedEventArgs e)
        {
            LoadThemeSetting();
            LoadIpApiCredentials();
        }

        private void LoadUserInfo()
        {
            try
            {
                // 从 AuthService 加载用户信息
                CurrentUsername = _authService?.CurrentUsername ?? "未知";
                CurrentNickname = _authService?.CurrentNickname ?? "未知";
                CurrentBackendUrl = _backendConfigService?.CurrentBackendUrl ?? "未设置";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadUserInfo error: {ex.Message}");
                CurrentUsername = "未知";
                CurrentNickname = "未知";
                CurrentBackendUrl = "未设置";
            }
        }

        private void LoadThemeSetting()
        {
            _isLoadingTheme = true;

            try
            {
                var localSettings = ApplicationData.Current.LocalSettings;
                var themeSetting = localSettings.Values[ThemeSettingKey] as string ?? "Default";

                // 设置对应的 RadioButton (确保控件已初始化)
                if (LightThemeRadio != null && DarkThemeRadio != null && DefaultThemeRadio != null)
                {
                    switch (themeSetting)
                    {
                        case "Light":
                            LightThemeRadio.IsChecked = true;
                            break;
                        case "Dark":
                            DarkThemeRadio.IsChecked = true;
                            break;
                        case "Default":
                        default:
                            DefaultThemeRadio.IsChecked = true;
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadThemeSetting error: {ex.Message}");
            }
            finally
            {
                _isLoadingTheme = false;
            }
        }

        private void ThemeRadio_Checked(object sender, RoutedEventArgs e)
        {
            // 避免在加载主题时触发保存
            if (_isLoadingTheme) return;

            try
            {
                if (sender is RadioButton radioButton && radioButton.Tag is string theme)
                {
                    SaveAndApplyTheme(theme);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ThemeRadio_Checked error: {ex.Message}");
            }
        }

        private void SaveAndApplyTheme(string theme)
        {
            // 保存到本地存储
            var localSettings = ApplicationData.Current.LocalSettings;
            localSettings.Values[ThemeSettingKey] = theme;

            // 应用主题
            var app = Application.Current as App;
            if (app?.Window != null)
            {
                var window = app.Window;
                var rootElement = window.Content as FrameworkElement;

                if (rootElement != null)
                {
                    switch (theme)
                    {
                        case "Light":
                            rootElement.RequestedTheme = ElementTheme.Light;
                            break;
                        case "Dark":
                            rootElement.RequestedTheme = ElementTheme.Dark;
                            break;
                        case "Default":
                        default:
                            rootElement.RequestedTheme = ElementTheme.Default;
                            break;
                    }

                    ShowStatus("主题已更新", InfoBarSeverity.Success);
                }
            }
            // 使用原生标题栏，无需手动设置颜色
        }

        private async void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            // 显示确认对话框
            var dialog = new ContentDialog
            {
                Title = "确认退出",
                Content = "确定要退出当前账号吗？",
                PrimaryButtonText = "退出",
                CloseButtonText = "取消",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                // 清除认证信息
                await _authService.LogoutAsync();

                // 导航到登录页（通过 App.SetContent 确保 TitleBar 正确注册）
                var app = Application.Current as App;
                if (app?.Window != null)
                {
                    var loginPage = new LoginPage();
                    app.SetContent(loginPage, "登录 - Blog Manager");
                }
            }
        }

        private async void TestBackendInList_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string backendUrl)
            {
                button.IsEnabled = false;
                ShowStatus("正在测试连接...", InfoBarSeverity.Informational);

                try
                {
                    var (success, message) = await _backendConfigService.TestBackendAsync(backendUrl);

                    if (success)
                    {
                        ShowStatus($"✓ {message}", InfoBarSeverity.Success);
                    }
                    else
                    {
                        ShowStatus($"✗ {message}", InfoBarSeverity.Error);
                    }
                }
                catch (Exception ex)
                {
                    ShowStatus($"测试失败: {ex.Message}", InfoBarSeverity.Error);
                }
                finally
                {
                    button.IsEnabled = true;
                }
            }
        }

        private async void DeleteBackend_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string backendUrl)
            {
                // 显示确认对话框
                var dialog = new ContentDialog
                {
                    Title = "确认删除",
                    Content = $"确定要删除后端 \"{backendUrl}\" 吗？",
                    PrimaryButtonText = "删除",
                    CloseButtonText = "取消",
                    DefaultButton = ContentDialogButton.Close,
                    XamlRoot = this.XamlRoot
                };

                var result = await dialog.ShowAsync();

                if (result == ContentDialogResult.Primary)
                {
                    try
                    {
                        _backendConfigService.RemoveCustomBackend(backendUrl);
                        ShowStatus("后端删除成功", InfoBarSeverity.Success);
                    }
                    catch (Exception ex)
                    {
                        ShowStatus($"删除失败: {ex.Message}", InfoBarSeverity.Error);
                    }
                }
            }
        }

        private async void AddCustomBackendInSettings_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AddBackendDialog
            {
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                var name = dialog.BackendName;
                var url = dialog.BackendUrl;

                if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(url))
                {
                    ShowStatus("后端名称和地址不能为空", InfoBarSeverity.Warning);
                    return;
                }

                // 先测试连接
                ShowStatus("正在测试连接...", InfoBarSeverity.Informational);

                var (success, message) = await _backendConfigService.TestBackendAsync(url);

                if (success)
                {
                    try
                    {
                        _backendConfigService.AddCustomBackend(name, url);
                        ShowStatus($"后端 \"{name}\" 添加成功", InfoBarSeverity.Success);
                    }
                    catch (Exception ex)
                    {
                        ShowStatus($"添加失败: {ex.Message}", InfoBarSeverity.Error);
                    }
                }
                else
                {
                    ShowStatus($"连接测试失败: {message}，未添加该后端", InfoBarSeverity.Error);
                }
            }
        }

        private void LoadIpApiCredentials()
        {
            try
            {
                var localSettings = ApplicationData.Current.LocalSettings;
                IpApiUserIdBox.Text = localSettings.Values["ip_api_userid"] as string ?? string.Empty;
                var savedKey = localSettings.Values["ip_api_userkey"] as string ?? string.Empty;
                IpApiUserKeyBox.Password = savedKey;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadIpApiCredentials error: {ex.Message}");
            }
        }

        private void SaveIpApiCredentials_Click(object sender, RoutedEventArgs e)
        {
            var userId = IpApiUserIdBox.Text?.Trim();
            var userKey = IpApiUserKeyBox.Password?.Trim();

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(userKey))
            {
                ShowStatus("用户 ID 和密钥不能为空", InfoBarSeverity.Warning);
                return;
            }

            try
            {
                _ipLocationService.SetCredentials(userId, userKey);
                ShowStatus("IP 属地 API 凭据已保存", InfoBarSeverity.Success);
            }
            catch (Exception ex)
            {
                ShowStatus($"保存失败: {ex.Message}", InfoBarSeverity.Error);
            }
        }

        private void ShowStatus(string message, InfoBarSeverity severity)
        {
            StatusInfoBar.Message = message;
            StatusInfoBar.Severity = severity;
            StatusInfoBar.Visibility = Visibility.Visible;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
