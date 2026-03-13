using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Blog_Manager.Helpers;
using Blog_Manager.Models;
using Blog_Manager.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace Blog_Manager.Views
{
    public sealed partial class SettingsPage : Page, INotifyPropertyChanged
    {
        private readonly AuthService _authService;
        private readonly BackendConfigService _backendConfigService;
        private readonly IpLocationService _ipLocationService;
        private const string ThemeSettingKey = "AppTheme";
        private bool _isLoadingTheme = true;

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
            LoadGitHubToken();
            LoadVersionInfo();
            LoadImageStorageConfig();
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
                var themeSetting = SettingsHelper.GetString(ThemeSettingKey) ?? "Default";

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
            SettingsHelper.SetValue(ThemeSettingKey, theme);

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

                    App.ShowSuccess("主题已更新");
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
                var originalContent = button.Content;
                button.Content = new ProgressRing { Width = 14, Height = 14, IsActive = true };

                try
                {
                    var (success, message) = await _backendConfigService.TestBackendAsync(backendUrl);

                    if (success)
                    {
                        App.ShowSuccess(message);
                    }
                    else
                    {
                        App.ShowError(message);
                    }
                }
                catch (Exception ex)
                {
                    App.ShowError($"测试失败: {ex.Message}");
                }
                finally
                {
                    button.Content = originalContent;
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
                        App.ShowSuccess("后端删除成功");
                    }
                    catch (Exception ex)
                    {
                        App.ShowError($"删除失败: {ex.Message}");
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
                    App.ShowWarning("后端名称和地址不能为空");
                    return;
                }

                var (success, message) = await _backendConfigService.TestBackendAsync(url);

                if (success)
                {
                    try
                    {
                        _backendConfigService.AddCustomBackend(name, url);
                        App.ShowSuccess($"后端 \"{name}\" 添加成功");
                    }
                    catch (Exception ex)
                    {
                        App.ShowError($"添加失败: {ex.Message}");
                    }
                }
                else
                {
                    App.ShowError($"连接测试失败: {message}，未添加该后端");
                }
            }
        }

        private void LoadIpApiCredentials()
        {
            try
            {
                IpApiUserIdBox.Text = SettingsHelper.GetString("ip_api_userid") ?? string.Empty;
                var savedKey = SettingsHelper.GetString("ip_api_userkey") ?? string.Empty;
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
                App.ShowWarning("用户 ID 和密钥不能为空");
                return;
            }

            try
            {
                _ipLocationService.SetCredentials(userId, userKey);
                App.ShowSuccess("IP 属地 API 凭据已保存");
            }
            catch (Exception ex)
            {
                App.ShowError($"保存失败: {ex.Message}");
            }
        }

        #region 图片存储配置

        private bool _isLoadingStorageConfig = true;
        private ImageStorageConfig? _currentImageStorageConfig;

        private async void LoadImageStorageConfig()
        {
            _isLoadingStorageConfig = true;
            try
            {
                var app = Application.Current as App;
                var configApi = app?.ApiServiceFactory.CreateConfigApi();
                if (configApi == null) return;

                var result = await configApi.GetImageStorageConfigAsync();
                if (result?.Data != null)
                {
                    _currentImageStorageConfig = result.Data;
                    PopulateImageStorageUI(result.Data);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadImageStorageConfig error: {ex.Message}");
                _currentImageStorageConfig = new ImageStorageConfig();
                PopulateImageStorageUI(_currentImageStorageConfig);
            }
            finally
            {
                _isLoadingStorageConfig = false;
            }
        }

        private void PopulateImageStorageUI(ImageStorageConfig config)
        {
            if (config.Mode == "cdn")
            {
                CdnStorageRadio.IsChecked = true;
                CdnConfigPanel.Visibility = Visibility.Visible;
                TestCdnButton.Visibility = Visibility.Visible;
            }
            else
            {
                LocalStorageRadio.IsChecked = true;
                CdnConfigPanel.Visibility = Visibility.Collapsed;
                TestCdnButton.Visibility = Visibility.Collapsed;
            }

            var cdn = config.Cdn ?? new CdnConfig();
            CdnUploadUrlBox.Text = cdn.UploadUrl ?? string.Empty;
            CdnFileFieldBox.Text = string.IsNullOrEmpty(cdn.FileField) ? "file" : cdn.FileField;
            CdnResponseUrlFieldBox.Text = string.IsNullOrEmpty(cdn.ResponseUrlField) ? "data.url" : cdn.ResponseUrlField;
            CdnResponseDeleteFieldBox.Text = cdn.ResponseDeleteField ?? string.Empty;
            CdnDeleteUrlTemplateBox.Text = cdn.DeleteUrlTemplate ?? string.Empty;
            CdnTimeoutBox.Value = cdn.Timeout > 0 ? cdn.Timeout : 30;
            CdnMaxRetriesBox.Value = cdn.MaxRetries >= 0 ? cdn.MaxRetries : 2;

            // Set method combos
            SelectComboByContent(CdnMethodCombo, cdn.Method ?? "POST");
            SelectComboByContent(CdnDeleteMethodCombo, cdn.DeleteMethod ?? "GET");

            // Populate headers
            HeadersPanel.Children.Clear();
            if (cdn.Headers != null)
            {
                foreach (var kvp in cdn.Headers)
                {
                    AddKeyValueRow(HeadersPanel, kvp.Key, kvp.Value);
                }
            }

            // Populate extra params
            ExtraParamsPanel.Children.Clear();
            if (cdn.ExtraParams != null)
            {
                foreach (var kvp in cdn.ExtraParams)
                {
                    AddKeyValueRow(ExtraParamsPanel, kvp.Key, kvp.Value);
                }
            }
        }

        private void StorageMode_Checked(object sender, RoutedEventArgs e)
        {
            if (_isLoadingStorageConfig) return;

            if (sender is RadioButton rb && rb.Tag is string mode)
            {
                bool isCdn = mode == "cdn";
                CdnConfigPanel.Visibility = isCdn ? Visibility.Visible : Visibility.Collapsed;
                TestCdnButton.Visibility = isCdn ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private ImageStorageConfig BuildImageStorageConfig()
        {
            var config = new ImageStorageConfig
            {
                Mode = CdnStorageRadio.IsChecked == true ? "cdn" : "local",
                Cdn = new CdnConfig
                {
                    UploadUrl = CdnUploadUrlBox.Text?.Trim() ?? string.Empty,
                    Method = (CdnMethodCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "POST",
                    FileField = CdnFileFieldBox.Text?.Trim() ?? "file",
                    ResponseUrlField = CdnResponseUrlFieldBox.Text?.Trim() ?? "data.url",
                    ResponseDeleteField = CdnResponseDeleteFieldBox.Text?.Trim() ?? string.Empty,
                    DeleteUrlTemplate = CdnDeleteUrlTemplateBox.Text?.Trim() ?? string.Empty,
                    DeleteMethod = (CdnDeleteMethodCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "GET",
                    Timeout = (int)CdnTimeoutBox.Value,
                    MaxRetries = (int)CdnMaxRetriesBox.Value,
                    Headers = CollectKeyValuePairs(HeadersPanel),
                    ExtraParams = CollectKeyValuePairs(ExtraParamsPanel),
                }
            };
            return config;
        }

        private async void SaveImageStorageConfig_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var config = BuildImageStorageConfig();

                if (config.Mode == "cdn" && string.IsNullOrWhiteSpace(config.Cdn.UploadUrl))
                {
                    App.ShowWarning("图床模式下必须填写上传 API 地址");
                    return;
                }

                var app = Application.Current as App;
                var configApi = app?.ApiServiceFactory.CreateConfigApi();
                if (configApi == null) return;

                await configApi.UpdateImageStorageConfigAsync(config);
                _currentImageStorageConfig = config;
                App.ShowSuccess("图片存储配置已保存");
            }
            catch (Exception ex)
            {
                App.ShowError($"保存失败: {ex.Message}");
            }
        }

        private async void TestCdnConnection_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button == null) return;

            button.IsEnabled = false;
            var originalContent = button.Content;
            button.Content = new ProgressRing { Width = 14, Height = 14, IsActive = true };

            try
            {
                // First save the config
                var config = BuildImageStorageConfig();

                if (string.IsNullOrWhiteSpace(config.Cdn.UploadUrl))
                {
                    App.ShowWarning("请先填写上传 API 地址");
                    return;
                }

                var app = Application.Current as App;
                var configApi = app?.ApiServiceFactory.CreateConfigApi();
                if (configApi == null) return;

                await configApi.UpdateImageStorageConfigAsync(config);
                _currentImageStorageConfig = config;

                // Try uploading a test image through the existing upload API
                var fileApi = app?.ApiServiceFactory.CreateFileApi();
                if (fileApi == null) return;

                // Create a minimal 1x1 PNG test image
                byte[] testPng = Convert.FromBase64String(
                    "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8/5+hHgAHggJ/PchI7wAAAABJRU5ErkJggg==");

                using var stream = new System.IO.MemoryStream(testPng);
                var streamPart = new Refit.StreamPart(stream, "test.png", "image/png");
                var uploadResult = await fileApi.UploadCoverAsync(streamPart);

                if (uploadResult?.Data != null)
                {
                    var url = uploadResult.Data.ContainsKey("url") ? uploadResult.Data["url"] : uploadResult.Data.GetValueOrDefault("path", "");
                    App.ShowSuccess($"测试成功！图片地址: {url}");
                }
                else
                {
                    App.ShowSuccess("测试上传成功");
                }
            }
            catch (Exception ex)
            {
                App.ShowError($"测试失败: {ex.Message}");
            }
            finally
            {
                button.Content = originalContent;
                button.IsEnabled = true;
            }
        }

        private void AddHeader_Click(object sender, RoutedEventArgs e)
        {
            AddKeyValueRow(HeadersPanel, string.Empty, string.Empty);
        }

        private void AddExtraParam_Click(object sender, RoutedEventArgs e)
        {
            AddKeyValueRow(ExtraParamsPanel, string.Empty, string.Empty);
        }

        private void AddKeyValueRow(StackPanel panel, string key, string value)
        {
            var row = new Grid { ColumnSpacing = 8 };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var keyBox = new TextBox { PlaceholderText = "键", Text = key };
            Grid.SetColumn(keyBox, 0);

            var valueBox = new TextBox { PlaceholderText = "值", Text = value };
            Grid.SetColumn(valueBox, 1);

            var removeBtn = new Button
            {
                Content = new FontIcon { Glyph = "\xE74D", FontSize = 12 },
                Tag = row,
            };
            removeBtn.Click += (s, e) =>
            {
                if (s is Button btn && btn.Tag is Grid g)
                {
                    panel.Children.Remove(g);
                }
            };
            Grid.SetColumn(removeBtn, 2);

            row.Children.Add(keyBox);
            row.Children.Add(valueBox);
            row.Children.Add(removeBtn);

            panel.Children.Add(row);
        }

        private Dictionary<string, string> CollectKeyValuePairs(StackPanel panel)
        {
            var dict = new Dictionary<string, string>();
            foreach (var child in panel.Children)
            {
                if (child is Grid row && row.Children.Count >= 2)
                {
                    var keyBox = row.Children.OfType<TextBox>().FirstOrDefault();
                    var valueBox = row.Children.OfType<TextBox>().Skip(1).FirstOrDefault();
                    if (keyBox != null && valueBox != null)
                    {
                        var k = keyBox.Text?.Trim();
                        var v = valueBox.Text?.Trim();
                        if (!string.IsNullOrEmpty(k))
                        {
                            dict[k] = v ?? string.Empty;
                        }
                    }
                }
            }
            return dict;
        }

        private void SelectComboByContent(ComboBox combo, string content)
        {
            foreach (var item in combo.Items)
            {
                if (item is ComboBoxItem cbi && cbi.Content?.ToString() == content)
                {
                    combo.SelectedItem = cbi;
                    return;
                }
            }
            if (combo.Items.Count > 0) combo.SelectedIndex = 0;
        }

        #endregion

        #region 版本更新

        private void LoadVersionInfo()
        {
            VersionText.Text = $"版本 {AppVersion.Current}";
        }

        private void LoadGitHubToken()
        {
            var token = SettingsHelper.GetString("github_token") ?? string.Empty;
            GitHubTokenBox.Password = token;
        }

        private void SaveGitHubToken_Click(object sender, RoutedEventArgs e)
        {
            var token = GitHubTokenBox.Password?.Trim();
            if (string.IsNullOrEmpty(token))
            {
                App.ShowWarning("Token 不能为空，如需移除请点击\"清除\"");
                return;
            }

            SettingsHelper.SetValue("github_token", token);
            App.ShowSuccess("GitHub Token 已保存");
        }

        private void ClearGitHubToken_Click(object sender, RoutedEventArgs e)
        {
            GitHubTokenBox.Password = string.Empty;
            SettingsHelper.RemoveValue("github_token");
            App.ShowSuccess("GitHub Token 已清除");
        }

        private async void CheckUpdateButton_Click(object sender, RoutedEventArgs e)
        {
            var app = Application.Current as App;
            if (app?.UpdateService == null)
            {
                App.ShowError("更新服务不可用");
                return;
            }

            CheckUpdateButton.IsEnabled = false;
            UpdateStatusText.Text = "正在检查更新...";

            try
            {
                var result = await app.UpdateService.CheckForUpdateAsync();

                if (result == null)
                {
                    UpdateStatusText.Text = "无法获取版本信息，请检查 GitHub Token 配置";
                    App.ShowWarning("检查更新失败：无法获取版本信息");
                }
                else if (!result.HasUpdate)
                {
                    UpdateStatusText.Text = $"当前已是最新版本 ({AppVersion.Current})";
                    App.ShowSuccess("当前已是最新版本");
                }
                else if (result.IsMajorUpdate)
                {
                    UpdateStatusText.Text = $"发现重大更新: {result.LatestVersion}";
                    var app2 = Application.Current as App;
                    var mainWindow = app2?.Window?.Content as MainWindow;
                    mainWindow?.ShowForceUpdateOverlay(result);
                }
                else
                {
                    UpdateStatusText.Text = $"发现新版本: {result.LatestVersion}";
                    var dialog = new UpdateDialog(result) { XamlRoot = this.XamlRoot };
                    await dialog.ShowAsync();
                }
            }
            catch (Exception ex)
            {
                UpdateStatusText.Text = $"检查失败: {ex.Message}";
                App.ShowError($"检查更新失败: {ex.Message}");
            }
            finally
            {
                CheckUpdateButton.IsEnabled = true;
            }
        }

        private bool _changelogLoaded = false;

        private async void ChangelogExpander_Expanding(Expander sender, ExpanderExpandingEventArgs args)
        {
            if (_changelogLoaded) return;

            var app = Application.Current as App;
            if (app?.UpdateService == null) return;

            ChangelogLoadingRing.Visibility = Visibility.Visible;
            ChangelogLoadingRing.IsActive = true;

            try
            {
                var releases = await app.UpdateService.GetAllReleasesAsync();

                if (releases.Count == 0)
                {
                    ChangelogEmptyText.Visibility = Visibility.Visible;
                }
                else
                {
                    var items = new ObservableCollection<ChangelogItem>();
                    foreach (var release in releases)
                    {
                        items.Add(new ChangelogItem
                        {
                            Version = release.TagName,
                            Title = release.Name,
                            Body = string.IsNullOrWhiteSpace(release.Body) ? "暂无更新说明" : release.Body.Trim(),
                            IsMajor = release.IsMajor
                        });
                    }

                    ChangelogListView.ItemsSource = items;
                    ChangelogListView.ItemTemplate = (DataTemplate)CreateChangelogItemTemplate();
                }

                _changelogLoaded = true;
            }
            catch (Exception ex)
            {
                ChangelogEmptyText.Text = $"加载失败: {ex.Message}";
                ChangelogEmptyText.Visibility = Visibility.Visible;
            }
            finally
            {
                ChangelogLoadingRing.IsActive = false;
                ChangelogLoadingRing.Visibility = Visibility.Collapsed;
            }
        }

        private DataTemplate CreateChangelogItemTemplate()
        {
            // 使用代码创建 DataTemplate，因为 ChangelogItem 是内部类
            var xaml = @"
<DataTemplate xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
              xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
    <Border Background=""{ThemeResource LayerFillColorDefaultBrush}""
            BorderBrush=""{ThemeResource CardStrokeColorDefaultBrush}""
            BorderThickness=""1""
            CornerRadius=""6""
            Padding=""16"">
        <StackPanel Spacing=""8"">
            <StackPanel Orientation=""Horizontal"" Spacing=""8"">
                <TextBlock Text=""{Binding Version}""
                           FontWeight=""SemiBold""
                           FontSize=""14""/>
                <TextBlock Text=""{Binding Title}""
                           Foreground=""{ThemeResource TextFillColorSecondaryBrush}""
                           FontSize=""13""/>
            </StackPanel>
            <TextBlock Text=""{Binding Body}""
                       TextWrapping=""Wrap""
                       FontSize=""12""
                       Foreground=""{ThemeResource TextFillColorSecondaryBrush}""
                       IsTextSelectionEnabled=""True""/>
        </StackPanel>
    </Border>
</DataTemplate>";

            return (DataTemplate)Microsoft.UI.Xaml.Markup.XamlReader.Load(xaml);
        }

        #endregion

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// 更新日志展示项
    /// </summary>
    public class ChangelogItem
    {
        public string Version { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public bool IsMajor { get; set; }
    }
}
