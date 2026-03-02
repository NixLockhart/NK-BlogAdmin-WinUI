using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Blog_Manager.Helpers;
using Blog_Manager.Services;
using Blog_Manager.Services.Api;
using Blog_Manager.ViewModels;
using Blog_Manager.Views;
using System;

namespace Blog_Manager
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        private Window? m_window;
        private const string ThemeSettingKey = "AppTheme";

        // Public property to access the main window
        public Window? Window => m_window;

        // Services
        public AuthService AuthService { get; private set; }
        public ApiServiceFactory ApiServiceFactory { get; private set; }
        public BackendConfigService BackendConfigService { get; private set; }
        public UpdateService UpdateService { get; private set; }
        public NotificationService NotificationService { get; private set; }

        // ViewModels
        public LoginViewModel LoginViewModel { get; private set; }

        public App()
        {
            this.InitializeComponent();
            InitializeServices();

            // 添加全局异常处理
            this.UnhandledException += App_UnhandledException;
        }

        private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            // 记录异常信息到日志文件
            try
            {
                var logDir = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "BlogManager");
                System.IO.Directory.CreateDirectory(logDir);
                var logFile = System.IO.Path.Combine(logDir, "crash.log");
                var msg = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 未处理的异常: {e.Exception}\n\n";
                System.IO.File.AppendAllText(logFile, msg);
            }
            catch { }

            System.Diagnostics.Debug.WriteLine($"未处理的异常: {e.Exception.Message}");
            e.Handled = true;
        }

        /// <summary>
        /// Initialize all services and dependency injection
        /// </summary>
        private void InitializeServices()
        {
            // 1. 创建后端配置服务
            BackendConfigService = new BackendConfigService();

            // 2. 创建API服务工厂（使用委托来获取 token，避免循环依赖）
            ApiServiceFactory = new ApiServiceFactory(
                () => AuthService?.CurrentToken,
                BackendConfigService
            );

            // 4. 创建真正的AuthService（使用ApiServiceFactory）
            AuthService = new AuthService(ApiServiceFactory);

            // 5. 创建ViewModels
            LoginViewModel = new LoginViewModel(AuthService);

            // 6. 创建更新服务（独立于后端 API，使用 GitHub Releases）
            UpdateService = new UpdateService();

            // 7. 创建通知服务
            NotificationService = new NotificationService();

            // 订阅登录成功事件
            LoginViewModel.LoginSuccessful += OnLoginSuccessful;
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            try
            {
                m_window = new Window();

                // 监听窗口关闭事件，释放资源
                m_window.Closed += OnWindowClosed;

                // 自定义标题栏设置
                m_window.ExtendsContentIntoTitleBar = true;
                m_window.SystemBackdrop = new MicaBackdrop();
                m_window.AppWindow.TitleBar.PreferredHeightOption = Microsoft.UI.Windowing.TitleBarHeightOption.Tall;
                m_window.AppWindow.SetIcon("Assets/Square44x44Logo.targetsize-24_altform-unplated.png");

                // Load saved theme or use default
                var themeSetting = LoadThemeSetting();

                // Check if already authenticated
                if (AuthService.IsAuthenticated)
                {
                    var mainWindow = new MainWindow();
                    SetContent(mainWindow, "博客管理系统 - Blog Manager", themeSetting);
                }
                else
                {
                    var loginPage = new LoginPage();
                    SetContent(loginPage, "登录 - Blog Manager", themeSetting);
                }

                m_window.Activate();
            }
            catch (Exception ex)
            {
                try
                {
                    var logDir = System.IO.Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "BlogManager");
                    System.IO.Directory.CreateDirectory(logDir);
                    var logFile = System.IO.Path.Combine(logDir, "crash.log");
                    var msg = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] OnLaunched 异常: {ex}\n\n";
                    System.IO.File.AppendAllText(logFile, msg);
                }
                catch { }
                throw;
            }
        }

        /// <summary>
        /// 设置窗口内容、标题栏和主题
        /// </summary>
        public void SetContent(FrameworkElement page, string windowTitle, string? theme = null)
        {
            if (m_window == null) return;

            m_window.Content = page;
            m_window.Title = windowTitle;

            // 注册 TitleBar 拖拽区域
            Microsoft.UI.Xaml.Controls.TitleBar? titleBar = null;
            if (page is MainWindow mainWin)
            {
                titleBar = mainWin.TitleBar;
            }
            else if (page is LoginPage loginPg)
            {
                titleBar = loginPg.TitleBar;
            }

            if (titleBar != null)
            {
                m_window.SetTitleBar(titleBar);
            }

            // 应用主题
            theme ??= LoadThemeSetting();
            ApplyTheme(page, theme);

            // 订阅主题变更事件，修正 caption button 颜色
            page.ActualThemeChanged += (_, _) =>
            {
                TitleBarHelper.ApplySystemThemeToCaptionButtons(m_window, page.ActualTheme);
            };

            // 立即应用一次 caption button 颜色
            TitleBarHelper.ApplySystemThemeToCaptionButtons(m_window, page.ActualTheme);
        }

        private string LoadThemeSetting()
        {
            return SettingsHelper.GetString(ThemeSettingKey) ?? "Default";
        }

        private void ApplyTheme(FrameworkElement rootElement, string theme)
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
        }

        /// <summary>
        /// Handle successful login
        /// </summary>
        private void OnLoginSuccessful(object? sender, EventArgs e)
        {
            if (m_window != null)
            {
                var mainWindow = new MainWindow();
                SetContent(mainWindow, "博客管理系统 - Blog Manager");
            }
        }

        /// <summary>
        /// 窗口关闭时释放资源
        /// </summary>
        private void OnWindowClosed(object sender, WindowEventArgs args)
        {
            // 释放 ApiServiceFactory 中的 HttpClient 资源
            ApiServiceFactory?.Dispose();
        }

        // 静态通知辅助方法，供各页面调用
        public static void ShowSuccess(string message) =>
            (Current as App)?.NotificationService?.ShowSuccess(message);

        public static void ShowError(string message) =>
            (Current as App)?.NotificationService?.ShowError(message);

        public static void ShowWarning(string message) =>
            (Current as App)?.NotificationService?.ShowWarning(message);

        public static void ShowInfo(string message) =>
            (Current as App)?.NotificationService?.ShowInfo(message);
    }
}
