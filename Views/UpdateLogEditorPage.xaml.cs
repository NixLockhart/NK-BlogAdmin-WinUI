using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Blog_Manager.Models;
using Blog_Manager.Services.Api;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Blog_Manager.Views
{
    public sealed partial class UpdateLogEditorPage : Page
    {
        private readonly IUpdateLogApi _updateLogApi;
        private long? _updateLogId;
        private UpdateLog? _originalUpdateLog;

        // 原始值（用于检测未保存更改）
        private string _originalVersion = "";
        private string _originalTitle = "";
        private string _originalContent = "";
        private bool _originalIsMajor = false;
        private DateTime _originalReleaseDate = DateTime.Now;

        public bool IsEditMode => _updateLogId.HasValue;
        public string PageTitle => IsEditMode ? "编辑更新日志" : "新建更新日志";

        /// <summary>
        /// 检查是否有未保存的更改
        /// </summary>
        public bool HasUnsavedChanges
        {
            get
            {
                return VersionTextBox.Text != _originalVersion ||
                       TitleTextBox.Text != _originalTitle ||
                       ContentTextBox.Text != _originalContent ||
                       IsMajorToggle.IsOn != _originalIsMajor ||
                       (ReleaseDatePicker.Date?.Date ?? DateTime.Now.Date) != _originalReleaseDate.Date;
            }
        }

        public UpdateLogEditorPage()
        {
            this.InitializeComponent();

            var app = (Application.Current as App) ?? throw new InvalidOperationException("App instance not found");
            _updateLogApi = app.ApiServiceFactory.CreateUpdateLogApi();

            // 设置默认日期
            ReleaseDatePicker.Date = DateTimeOffset.Now;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            LoadingPanel.Visibility = Visibility.Visible;
            ContentPanel.Visibility = Visibility.Collapsed;
            ErrorInfoBar.Visibility = Visibility.Collapsed;

            try
            {
                if (e.Parameter is long updateLogId)
                {
                    _updateLogId = updateLogId;
                    await LoadUpdateLogAsync(updateLogId);
                }
                else
                {
                    // 新建模式 - 保存原始值
                    SaveOriginalValues();
                }

                ContentPanel.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                ShowError($"加载失败: {ex.Message}");
            }
            finally
            {
                LoadingPanel.Visibility = Visibility.Collapsed;
            }
        }

        private async Task LoadUpdateLogAsync(long id)
        {
            var response = await _updateLogApi.GetUpdateLogsAsync();

            if (response.Code != 200 || response.Data == null)
            {
                throw new Exception(response.Message ?? "加载更新日志失败");
            }

            _originalUpdateLog = response.Data.FirstOrDefault(u => u.Id == id);

            if (_originalUpdateLog == null)
            {
                throw new Exception("未找到更新日志");
            }

            // 填充表单
            VersionTextBox.Text = _originalUpdateLog.Version;
            TitleTextBox.Text = _originalUpdateLog.Title ?? "";
            ContentTextBox.Text = _originalUpdateLog.Content;
            IsMajorToggle.IsOn = _originalUpdateLog.IsMajorUpdate;
            ReleaseDatePicker.Date = new DateTimeOffset(_originalUpdateLog.ReleaseDate);

            // 保存原始值
            SaveOriginalValues();
        }

        private void SaveOriginalValues()
        {
            _originalVersion = VersionTextBox.Text;
            _originalTitle = TitleTextBox.Text;
            _originalContent = ContentTextBox.Text;
            _originalIsMajor = IsMajorToggle.IsOn;
            _originalReleaseDate = ReleaseDatePicker.Date?.DateTime ?? DateTime.Now;
        }

        /// <summary>
        /// 保存更新日志 - 供 MainWindow 调用
        /// </summary>
        public async void OnSaveClick()
        {
            await SaveUpdateLogAsync();
        }

        /// <summary>
        /// 返回按钮点击 - 供 MainWindow 调用
        /// </summary>
        public void OnBackClick()
        {
            _ = HandleBackNavigationAsync();
        }

        private async Task SaveUpdateLogAsync()
        {
            // 验证
            if (string.IsNullOrWhiteSpace(VersionTextBox.Text))
            {
                ShowError("请输入版本号");
                return;
            }

            if (string.IsNullOrWhiteSpace(ContentTextBox.Text))
            {
                ShowError("请输入更新内容");
                return;
            }

            LoadingPanel.Visibility = Visibility.Visible;
            ContentPanel.Visibility = Visibility.Collapsed;
            ErrorInfoBar.Visibility = Visibility.Collapsed;

            try
            {
                var request = new UpdateLogRequest
                {
                    Version = VersionTextBox.Text.Trim(),
                    Title = string.IsNullOrWhiteSpace(TitleTextBox.Text) ? null : TitleTextBox.Text.Trim(),
                    Content = ContentTextBox.Text.Trim(),
                    IsMajor = IsMajorToggle.IsOn ? 1 : 0,
                    ReleaseDate = ReleaseDatePicker.Date?.DateTime ?? DateTime.Now
                };

                if (IsEditMode)
                {
                    var response = await _updateLogApi.UpdateUpdateLogAsync(_updateLogId!.Value, request);
                    if (response.Code != 200)
                    {
                        throw new Exception(response.Message ?? "保存失败");
                    }
                }
                else
                {
                    var response = await _updateLogApi.CreateUpdateLogAsync(request);
                    if (response.Code != 200)
                    {
                        throw new Exception(response.Message ?? "创建失败");
                    }
                }

                // 更新原始值（保存成功后）
                SaveOriginalValues();

                // 显示成功消息
                var dialog = new ContentDialog
                {
                    Title = "成功",
                    Content = IsEditMode ? "更新日志已保存" : "更新日志已创建",
                    CloseButtonText = "确定",
                    XamlRoot = this.XamlRoot
                };
                await dialog.ShowAsync();

                // 返回列表页
                NavigateBack();
            }
            catch (Exception ex)
            {
                ShowError($"保存失败: {ex.Message}");
                ContentPanel.Visibility = Visibility.Visible;
            }
            finally
            {
                LoadingPanel.Visibility = Visibility.Collapsed;
            }
        }

        private async Task HandleBackNavigationAsync()
        {
            if (!HasUnsavedChanges)
            {
                NavigateBack();
                return;
            }

            // 显示确认对话框
            var dialog = new ContentDialog
            {
                Title = "未保存的更改",
                Content = "您有未保存的更改。是否保存？",
                PrimaryButtonText = "保存",
                SecondaryButtonText = "不保存",
                CloseButtonText = "取消",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();

            switch (result)
            {
                case ContentDialogResult.Primary:
                    await SaveUpdateLogAsync();
                    break;
                case ContentDialogResult.Secondary:
                    NavigateBack();
                    break;
                // 取消 - 什么都不做
            }
        }

        private void NavigateBack()
        {
            var mainWindow = GetMainWindow();
            if (mainWindow != null)
            {
                mainWindow.NavigateBackFromSubPage();
            }
            else
            {
                Frame.GoBack();
            }
        }

        private MainWindow? GetMainWindow()
        {
            var window = (Application.Current as App)?.Window;
            return window?.Content as MainWindow;
        }

        private void ShowError(string message)
        {
            ErrorInfoBar.Message = message;
            ErrorInfoBar.Visibility = Visibility.Visible;
        }
    }
}
