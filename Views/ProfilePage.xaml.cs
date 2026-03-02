using Blog_Manager.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Threading.Tasks;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace Blog_Manager.Views
{
    /// <summary>
    /// 管理员个人信息页面
    /// </summary>
    public sealed partial class ProfilePage : Page
    {
        public ProfileViewModel ViewModel { get; }
        private const double NarrowBreakpoint = 700; // 窄屏断点

        public ProfilePage()
        {
            this.InitializeComponent();
            ViewModel = new ProfileViewModel();
        }

        /// <summary>
        /// 页面尺寸变化时调整布局
        /// </summary>
        private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateLayout(e.NewSize.Width);
        }

        /// <summary>
        /// 根据宽度更新布局
        /// </summary>
        private void UpdateLayout(double width)
        {
            if (width < NarrowBreakpoint)
            {
                // 窄屏模式：单列布局
                SetSingleColumnLayout();
            }
            else
            {
                // 宽屏模式：双列布局
                SetTwoColumnLayout();
            }
        }

        /// <summary>
        /// 设置单列布局（窄屏）
        /// </summary>
        private void SetSingleColumnLayout()
        {
            // 隐藏间隔列和右列
            SpacerColumn.Width = new GridLength(0);
            RightColumn.Width = new GridLength(0);
            LeftColumn.Width = new GridLength(1, GridUnitType.Star);

            // 将密码卡片移到第二行
            Grid.SetColumn(EditProfileCard, 0);
            Grid.SetRow(EditProfileCard, 0);
            Grid.SetColumnSpan(EditProfileCard, 3);

            Grid.SetColumn(PasswordCard, 0);
            Grid.SetRow(PasswordCard, 1);
            Grid.SetColumnSpan(PasswordCard, 3);
            PasswordCard.Margin = new Thickness(0, 16, 0, 0);
        }

        /// <summary>
        /// 设置双列布局（宽屏）
        /// </summary>
        private void SetTwoColumnLayout()
        {
            // 显示间隔列和右列
            SpacerColumn.Width = new GridLength(16);
            RightColumn.Width = new GridLength(1, GridUnitType.Star);
            LeftColumn.Width = new GridLength(1, GridUnitType.Star);

            // 恢复卡片位置
            Grid.SetColumn(EditProfileCard, 0);
            Grid.SetRow(EditProfileCard, 0);
            Grid.SetColumnSpan(EditProfileCard, 1);

            Grid.SetColumn(PasswordCard, 2);
            Grid.SetRow(PasswordCard, 0);
            Grid.SetColumnSpan(PasswordCard, 1);
            PasswordCard.Margin = new Thickness(0);
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // Ensure XamlRoot is available before loading
            await Task.Yield();

            // 初始化布局
            UpdateLayout(this.ActualWidth > 0 ? this.ActualWidth : 800);

            try
            {
                await LoadProfileAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ProfilePage: Failed to load data - {ex.Message}");
            }
        }

        private async Task LoadProfileAsync()
        {
            LoadingPanel.Visibility = Visibility.Visible;

            try
            {
                await ViewModel.LoadProfileAsync();
                UpdateDisplayInfo();
            }
            finally
            {
                LoadingPanel.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// 更新显示信息
        /// </summary>
        private void UpdateDisplayInfo()
        {
            if (ViewModel.Profile != null)
            {
                // 最后登录时间
                if (ViewModel.Profile.LastLoginAt.HasValue)
                {
                    LastLoginText.Text = ViewModel.Profile.LastLoginAt.Value.ToString("MM-dd HH:mm");
                    if (!string.IsNullOrEmpty(ViewModel.Profile.LastLoginIp))
                    {
                        LastLoginText.Text += $"\n{ViewModel.Profile.LastLoginIp}";
                    }
                }
                else
                {
                    LastLoginText.Text = "从未登录";
                }

                // 注册时间
                CreatedAtText.Text = ViewModel.Profile.CreatedAt.ToString("yyyy-MM-dd");
            }
        }

        /// <summary>
        /// 更换头像按钮点击
        /// </summary>
        private async void ChangeAvatarButton_Click(object sender, RoutedEventArgs e)
        {
            var picker = new FileOpenPicker();
            picker.ViewMode = PickerViewMode.Thumbnail;
            picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".jpeg");
            picker.FileTypeFilter.Add(".png");
            picker.FileTypeFilter.Add(".gif");
            picker.FileTypeFilter.Add(".webp");

            // Get the window handle from App
            var app = Application.Current as App;
            if (app?.Window != null)
            {
                var hwnd = WindowNative.GetWindowHandle(app.Window);
                InitializeWithWindow.Initialize(picker, hwnd);
            }

            var file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                LoadingPanel.Visibility = Visibility.Visible;

                try
                {
                    var (success, message) = await ViewModel.UploadAvatarAsync(file);

                    if (success)
                    {
                        App.ShowInfo("头像已上传，请点击「保存更改」以应用新头像");
                    }
                    else
                    {
                        ShowStatusMessage(message, InfoBarSeverity.Error);
                    }
                }
                finally
                {
                    LoadingPanel.Visibility = Visibility.Collapsed;
                }
            }
        }

        /// <summary>
        /// 保存信息按钮点击
        /// </summary>
        private async void SaveProfileButton_Click(object sender, RoutedEventArgs e)
        {
            LoadingPanel.Visibility = Visibility.Visible;

            try
            {
                var (success, message) = await ViewModel.SaveProfileAsync();

                if (success)
                {
                    // 刷新页面数据
                    await ViewModel.LoadProfileAsync();
                    UpdateDisplayInfo();

                    App.ShowSuccess("个人信息已更新");
                }
                else
                {
                    ShowStatusMessage(message, InfoBarSeverity.Error);
                }
            }
            finally
            {
                LoadingPanel.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// 重置表单按钮点击
        /// </summary>
        private void ResetFormButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.ResetForm();
            // 清空密码框
            OldPasswordBox.Password = string.Empty;
            NewPasswordBox.Password = string.Empty;
            ConfirmPasswordBox.Password = string.Empty;
            ShowStatusMessage("表单已重置", InfoBarSeverity.Informational);
        }

        /// <summary>
        /// 密码框内容变更事件
        /// </summary>
        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (sender is PasswordBox passwordBox)
            {
                if (passwordBox == OldPasswordBox)
                {
                    ViewModel.OldPassword = passwordBox.Password;
                }
                else if (passwordBox == NewPasswordBox)
                {
                    ViewModel.NewPassword = passwordBox.Password;
                }
                else if (passwordBox == ConfirmPasswordBox)
                {
                    ViewModel.ConfirmPassword = passwordBox.Password;
                }
            }
        }

        /// <summary>
        /// 修改密码按钮点击
        /// </summary>
        private async void ChangePasswordButton_Click(object sender, RoutedEventArgs e)
        {
            // 前端验证
            if (string.IsNullOrWhiteSpace(OldPasswordBox.Password))
            {
                App.ShowWarning("请输入当前密码");
                return;
            }

            if (string.IsNullOrWhiteSpace(NewPasswordBox.Password))
            {
                App.ShowWarning("请输入新密码");
                return;
            }

            if (NewPasswordBox.Password.Length < 8)
            {
                App.ShowWarning("新密码长度至少8位");
                return;
            }

            if (NewPasswordBox.Password != ConfirmPasswordBox.Password)
            {
                App.ShowWarning("新密码与确认密码不一致");
                return;
            }

            // 二次确认
            var dialog = new ContentDialog
            {
                Title = "确认修改密码",
                Content = "确定要修改密码吗？修改后建议重新登录。",
                PrimaryButtonText = "确定修改",
                CloseButtonText = "取消",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                LoadingPanel.Visibility = Visibility.Visible;

                try
                {
                    var (success, message) = await ViewModel.ChangePasswordAsync();

                    if (success)
                    {
                        // 清空密码框
                        OldPasswordBox.Password = string.Empty;
                        NewPasswordBox.Password = string.Empty;
                        ConfirmPasswordBox.Password = string.Empty;

                        App.ShowSuccess("密码已成功修改，建议重新登录以确保安全");
                    }
                    else
                    {
                        App.ShowError(message);
                    }
                }
                finally
                {
                    LoadingPanel.Visibility = Visibility.Collapsed;
                }
            }
        }

        /// <summary>
        /// 显示状态消息
        /// </summary>
        private void ShowStatusMessage(string message, InfoBarSeverity severity)
        {
            StatusInfoBar.Message = message;
            StatusInfoBar.Severity = severity;
            StatusInfoBar.Visibility = Visibility.Visible;

            // 自动隐藏成功消息
            if (severity == InfoBarSeverity.Success || severity == InfoBarSeverity.Informational)
            {
                _ = AutoHideStatusMessageAsync();
            }
        }

        private async Task AutoHideStatusMessageAsync()
        {
            await Task.Delay(3000);
            StatusInfoBar.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// 公共方法供 MainWindow 调用刷新
        /// </summary>
        public async void OnRefreshClick()
        {
            await LoadProfileAsync();
        }
    }
}
