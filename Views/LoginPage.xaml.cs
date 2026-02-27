using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Blog_Manager.ViewModels;
using Blog_Manager.Services;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Blog_Manager.Views
{
    public sealed partial class LoginPage : Page, INotifyPropertyChanged
    {
        public LoginViewModel ViewModel { get; }
        private readonly BackendConfigService _backendConfigService;

        /// <summary>
        /// 暴露 TitleBar 元素供 App.xaml.cs 调用 SetTitleBar
        /// </summary>
        public Microsoft.UI.Xaml.Controls.TitleBar TitleBar => AppTitleBar;

        public ObservableCollection<BackendServer> BackendServers => _backendConfigService.Backends;

        private BackendServer? _selectedBackend;
        public BackendServer? SelectedBackend
        {
            get => _selectedBackend;
            set
            {
                if (_selectedBackend != value)
                {
                    _selectedBackend = value;
                    OnPropertyChanged();
                }
            }
        }

        public LoginPage()
        {
            this.InitializeComponent();

            var app = Application.Current as App;
            ViewModel = app?.LoginViewModel ?? throw new InvalidOperationException("LoginViewModel not found");
            _backendConfigService = app?.BackendConfigService ?? throw new InvalidOperationException("BackendConfigService not found");

            // 设置当前选中的后端
            var currentBackend = _backendConfigService.GetCurrentBackend();
            SelectedBackend = currentBackend;
        }

        private void PasswordBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter && ViewModel.LoginCommand.CanExecute(null))
            {
                ViewModel.LoginCommand.Execute(null);
            }
        }

        private void BackendComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SelectedBackend != null)
            {
                try
                {
                    _backendConfigService.SetCurrentBackend(SelectedBackend.Url);
                    ShowBackendStatus($"已切换到: {SelectedBackend.Name}", InfoBarSeverity.Informational);
                }
                catch (Exception ex)
                {
                    ShowBackendStatus($"切换失败: {ex.Message}", InfoBarSeverity.Error);
                }
            }
        }

        private async void TestBackendButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedBackend == null)
            {
                ShowBackendStatus("请先选择后端服务器", InfoBarSeverity.Warning);
                return;
            }

            TestBackendButton.IsEnabled = false;
            ShowBackendStatus("正在测试连接...", InfoBarSeverity.Informational);

            try
            {
                var (success, message) = await _backendConfigService.TestBackendAsync(SelectedBackend.Url);

                if (success)
                {
                    ShowBackendStatus($"✓ {message}", InfoBarSeverity.Success);
                }
                else
                {
                    ShowBackendStatus($"✗ {message}", InfoBarSeverity.Error);
                }
            }
            catch (Exception ex)
            {
                ShowBackendStatus($"测试失败: {ex.Message}", InfoBarSeverity.Error);
            }
            finally
            {
                TestBackendButton.IsEnabled = true;
            }
        }

        private async void AddCustomBackendButton_Click(object sender, RoutedEventArgs e)
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
                    ShowBackendStatus("后端名称和地址不能为空", InfoBarSeverity.Warning);
                    return;
                }

                // 先测试连接
                ShowBackendStatus("正在测试连接...", InfoBarSeverity.Informational);

                var (success, message) = await _backendConfigService.TestBackendAsync(url);

                if (success)
                {
                    try
                    {
                        _backendConfigService.AddCustomBackend(name, url);
                        ShowBackendStatus($"后端 \"{name}\" 添加成功", InfoBarSeverity.Success);

                        // 自动选中新添加的后端
                        SelectedBackend = _backendConfigService.Backends[^1];
                    }
                    catch (Exception ex)
                    {
                        ShowBackendStatus($"添加失败: {ex.Message}", InfoBarSeverity.Error);
                    }
                }
                else
                {
                    ShowBackendStatus($"连接测试失败: {message}，未添加该后端", InfoBarSeverity.Error);
                }
            }
        }

        private void ShowBackendStatus(string message, InfoBarSeverity severity)
        {
            BackendStatusInfoBar.Message = message;
            BackendStatusInfoBar.Severity = severity;
            BackendStatusInfoBar.Visibility = Visibility.Visible;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// 添加自定义后端对话框
    /// </summary>
    public sealed class AddBackendDialog : ContentDialog
    {
        private TextBox? _nameTextBox;
        private TextBox? _urlTextBox;

        public string BackendName => _nameTextBox?.Text ?? string.Empty;
        public string BackendUrl => _urlTextBox?.Text ?? string.Empty;

        public AddBackendDialog()
        {
            Title = "添加自定义后端";
            PrimaryButtonText = "添加";
            CloseButtonText = "取消";
            DefaultButton = ContentDialogButton.Primary;

            var panel = new StackPanel
            {
                Spacing = 12,
                Width = 480
            };

            _nameTextBox = new TextBox
            {
                Header = "后端名称",
                PlaceholderText = "例如：生产环境",
            };
            panel.Children.Add(_nameTextBox);

            _urlTextBox = new TextBox
            {
                Header = "后端地址",
                PlaceholderText = "例如：https://api.yourdomain.com",
            };
            panel.Children.Add(_urlTextBox);

            var hint = new TextBlock
            {
                Text = "提示：将自动测试连接，只有测试通过才能添加",
                Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Gray),
                TextWrapping = TextWrapping.Wrap,
                FontSize = 12,
                Margin = new Thickness(0, 8, 0, 0)
            };
            panel.Children.Add(hint);

            Content = panel;

            // 验证输入
            PrimaryButtonClick += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(BackendName) || string.IsNullOrWhiteSpace(BackendUrl))
                {
                    e.Cancel = true;
                }
            };
        }
    }
}
