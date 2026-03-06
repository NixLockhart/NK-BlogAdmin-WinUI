using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Media.Animation;
using Blog_Manager.Models;
using Blog_Manager.Services;
using Blog_Manager.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Blog_Manager
{
    /// <summary>
    /// Main application window with navigation
    /// </summary>
    public sealed partial class MainWindow : Page
    {
        private readonly AuthService _authService;
        private UpdateCheckResult? _forceUpdateResult;

        /// <summary>
        /// 暴露 TitleBar 元素供 App.xaml.cs 调用 SetTitleBar
        /// </summary>
        public Microsoft.UI.Xaml.Controls.TitleBar TitleBar => AppTitleBar;

        private readonly Dictionary<string, string> _pageTitles = new()
        {
            { "Dashboard", "仪表板" },
            { "Articles", "文章管理" },
            { "Categories", "分类管理" },
            { "Comments", "评论管理" },
            { "Messages", "留言管理" },
            { "Widgets", "小工具管理" },
            { "Themes", "主题管理" },
            { "Config", "网站配置" },
            { "Announcements", "公告管理" },
            { "UpdateLogs", "更新日志" },
            { "Profile", "个人信息" },
            { "Settings", "设置" }
        };

        // 面包屑数据源
        public ObservableCollection<string> BreadcrumbItems { get; } = new();

        // 当前是否在子页面（如文章编辑器）
        private bool _isInSubPage = false;
        private string _parentPageTag = string.Empty;

        // 侧边栏点击时暂存目标页面 tag（供 NavigateBackFromSubPage 使用）
        private string? _pendingNavigationTag = null;

        public MainWindow()
        {
            this.InitializeComponent();
            _authService = (Application.Current as App)?.AuthService
                ?? throw new InvalidOperationException("AuthService not found");

            // 设置面包屑数据源
            PageBreadcrumb.ItemsSource = BreadcrumbItems;

            // 监听Frame导航事件
            ContentFrame.Navigated += ContentFrame_Navigated;

            // 初始化通知服务
            var app = Application.Current as App;
            if (app?.NotificationService != null)
            {
                app.NotificationService.Initialize(NotificationContainer, this.DispatcherQueue);
            }
        }

        /// <summary>
        /// Frame导航完成事件 - 处理面包屑和工具栏
        /// </summary>
        private void ContentFrame_Navigated(object sender, NavigationEventArgs e)
        {
            if (e.Content is ArticleEditorPage editorPage)
            {
                // 进入文章编辑器 - 显示面包屑
                _isInSubPage = true;
                _parentPageTag = "Articles";

                // 根据导航参数判断是编辑还是新建（参数是文章ID表示编辑模式）
                bool isEditMode = e.Parameter is long;
                string subPageTitle = isEditMode ? "编辑文章" : "新建文章";
                UpdateBreadcrumb("文章管理", subPageTitle);

                // 监听ViewModel变化以更新子标题（例如加载完成后可能有更详细的标题）
                editorPage.ViewModel.PropertyChanged += (s, args) =>
                {
                    if (args.PropertyName == nameof(editorPage.ViewModel.PageTitle) ||
                        args.PropertyName == nameof(editorPage.ViewModel.IsEditMode))
                    {
                        UpdateBreadcrumb("文章管理", editorPage.ViewModel.PageTitle);
                    }
                };

                // 更新工具栏为编辑器模式
                UpdateToolbarForEditor(editorPage);
            }
            else if (e.Content is UpdateLogEditorPage updateLogEditorPage)
            {
                // 进入更新日志编辑器 - 显示面包屑
                _isInSubPage = true;
                _parentPageTag = "UpdateLogs";

                // 根据导航参数判断是编辑还是新建
                bool isEditMode = e.Parameter is long;
                string subPageTitle = isEditMode ? "编辑更新日志" : "新建更新日志";
                UpdateBreadcrumb("更新日志", subPageTitle);

                // 更新工具栏为更新日志编辑器模式
                UpdateToolbarForUpdateLogEditor(updateLogEditorPage);
            }
        }

        /// <summary>
        /// 更新面包屑导航
        /// </summary>
        private void UpdateBreadcrumb(params string[] items)
        {
            BreadcrumbItems.Clear();
            foreach (var item in items)
            {
                BreadcrumbItems.Add(item);
            }
        }

        /// <summary>
        /// 面包屑项点击事件
        /// </summary>
        private void PageBreadcrumb_ItemClicked(BreadcrumbBar sender, BreadcrumbBarItemClickedEventArgs args)
        {
            // 只有点击非最后一项才处理（点击父级导航）
            if (args.Index < BreadcrumbItems.Count - 1)
            {
                if (_isInSubPage)
                {
                    // 面包屑返回始终回到父页面，清除侧边栏暂存的目标
                    _pendingNavigationTag = null;

                    if (ContentFrame.Content is ArticleEditorPage editorPage)
                    {
                        // 调用编辑器的返回处理（包含未保存更改检查）
                        editorPage.OnBackClick();
                    }
                    else if (ContentFrame.Content is UpdateLogEditorPage updateLogEditorPage)
                    {
                        // 调用更新日志编辑器的返回处理
                        updateLogEditorPage.OnBackClick();
                    }
                }
            }
        }

        /// <summary>
        /// 更新编辑器页面的工具栏
        /// </summary>
        private void UpdateToolbarForEditor(ArticleEditorPage editorPage)
        {
            ToolbarContainer.Children.Clear();

            // 检查是否为只读模式
            bool isReadOnly = editorPage.IsReadOnlyMode;

            // 自动保存状态（只读模式下不显示）
            if (!isReadOnly)
            {
                var autoSavePanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4 };
                var autoSaveRing = new ProgressRing { Width = 16, Height = 16 };
                autoSaveRing.SetBinding(ProgressRing.IsActiveProperty,
                    new Microsoft.UI.Xaml.Data.Binding { Source = editorPage.ViewModel, Path = new PropertyPath("IsAutoSaving"), Mode = Microsoft.UI.Xaml.Data.BindingMode.OneWay });
                autoSaveRing.SetBinding(UIElement.VisibilityProperty,
                    new Microsoft.UI.Xaml.Data.Binding { Source = editorPage.ViewModel, Path = new PropertyPath("IsAutoSaving"), Mode = Microsoft.UI.Xaml.Data.BindingMode.OneWay });

                var autoSaveText = new TextBlock
                {
                    Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
                    VerticalAlignment = VerticalAlignment.Center,
                    FontSize = 12
                };
                autoSaveText.SetBinding(TextBlock.TextProperty,
                    new Microsoft.UI.Xaml.Data.Binding { Source = editorPage.ViewModel, Path = new PropertyPath("AutoSaveStatus"), Mode = Microsoft.UI.Xaml.Data.BindingMode.OneWay });

                autoSavePanel.Children.Add(autoSaveRing);
                autoSavePanel.Children.Add(autoSaveText);
                ToolbarContainer.Children.Add(autoSavePanel);
            }

            // 只读模式下不显示保存和发布按钮
            if (!isReadOnly)
            {
                // 保存草稿按钮
                var saveDraftBtn = new Button();
                var saveDraftPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
                saveDraftPanel.Children.Add(new SymbolIcon(Symbol.Save));
                saveDraftPanel.Children.Add(new TextBlock { Text = "保存草稿", VerticalAlignment = VerticalAlignment.Center });
                saveDraftBtn.Content = saveDraftPanel;
                saveDraftBtn.Click += (s, e) => editorPage.OnSaveDraftClick();
                ToolbarContainer.Children.Add(saveDraftBtn);

                // 发布按钮
                var publishBtn = new Button { Style = (Style)Application.Current.Resources["AccentButtonStyle"] };
                var publishPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
                publishPanel.Children.Add(new SymbolIcon(Symbol.Globe));
                publishPanel.Children.Add(new TextBlock { Text = editorPage.ViewModel.IsEditMode ? "更新发布" : "发布文章", VerticalAlignment = VerticalAlignment.Center });
                publishBtn.Content = publishPanel;
                publishBtn.Click += (s, e) => editorPage.OnPublishClick();
                ToolbarContainer.Children.Add(publishBtn);
            }
        }

        /// <summary>
        /// 更新更新日志编辑器页面的工具栏
        /// </summary>
        private void UpdateToolbarForUpdateLogEditor(UpdateLogEditorPage editorPage)
        {
            ToolbarContainer.Children.Clear();

            // 保存按钮
            var saveBtn = new Button { Style = (Style)Application.Current.Resources["AccentButtonStyle"] };
            var savePanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
            savePanel.Children.Add(new SymbolIcon(Symbol.Save));
            savePanel.Children.Add(new TextBlock { Text = "保存", VerticalAlignment = VerticalAlignment.Center });
            saveBtn.Content = savePanel;
            saveBtn.Click += (s, e) => editorPage.OnSaveClick();
            ToolbarContainer.Children.Add(saveBtn);
        }

        /// <summary>
        /// 从子页面返回（供子页面调用）
        /// </summary>
        public void NavigateBackFromSubPage()
        {
            if (_isInSubPage)
            {
                _isInSubPage = false;

                // 优先使用侧边栏暂存的目标页面，否则回到父页面
                var targetTag = _pendingNavigationTag ?? _parentPageTag;
                _pendingNavigationTag = null;
                _parentPageTag = string.Empty;

                if (!string.IsNullOrEmpty(targetTag))
                {
                    NavigateToPage(targetTag);

                    // 同步侧边栏选中状态
                    SelectNavItem(targetTag);
                }
            }
        }

        /// <summary>
        /// 同步侧边栏选中项
        /// </summary>
        private void SelectNavItem(string tag)
        {
            if (tag == "Settings")
            {
                NavView.SelectedItem = NavView.SettingsItem;
                return;
            }

            foreach (NavigationViewItemBase item in NavView.MenuItems)
            {
                if (item is NavigationViewItem navItem && navItem.Tag?.ToString() == tag)
                {
                    NavView.SelectedItem = navItem;
                    return;
                }
            }
            foreach (NavigationViewItemBase item in NavView.FooterMenuItems)
            {
                if (item is NavigationViewItem navItem && navItem.Tag?.ToString() == tag)
                {
                    NavView.SelectedItem = navItem;
                    return;
                }
            }
        }

        /// <summary>
        /// 刷新编辑器工具栏（供 ArticleEditorPage 在确定只读模式后调用）
        /// </summary>
        public void RefreshEditorToolbar()
        {
            if (_isInSubPage && ContentFrame.Content is ArticleEditorPage editorPage)
            {
                UpdateToolbarForEditor(editorPage);
            }
        }

        /// <summary>
        /// 判断当前是否在编辑器子页面且有未保存的更改
        /// </summary>
        public bool HasUnsavedEditorChanges()
        {
            if (!_isInSubPage) return false;

            if (ContentFrame.Content is ArticleEditorPage articleEditor)
                return articleEditor.ViewModel.HasUnsavedChanges;

            if (ContentFrame.Content is UpdateLogEditorPage updateLogEditor)
                return updateLogEditor.HasUnsavedChanges;

            return false;
        }

        /// <summary>
        /// 窗口关闭前的编辑器确认对话框，返回 true 表示可以关闭
        /// </summary>
        public async Task<bool> ConfirmCloseAsync()
        {
            if (!_isInSubPage) return true;

            if (ContentFrame.Content is ArticleEditorPage articleEditor && articleEditor.ViewModel.HasUnsavedChanges)
            {
                var dialog = new ContentDialog
                {
                    Title = "未保存的更改",
                    Content = "您有未保存的更改。关闭窗口将丢失这些修改。\n\n选择\"保存\"将保存当前修改后关闭\n选择\"不保存\"将直接关闭",
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
                        try { await articleEditor.ViewModel.SaveArticleAsync(); }
                        catch { return false; }
                        return true;
                    case ContentDialogResult.Secondary:
                        try { await articleEditor.ViewModel.RollbackToOriginalAsync(); }
                        catch { /* 关闭窗口不阻塞 */ }
                        return true;
                    default:
                        return false;
                }
            }

            if (ContentFrame.Content is UpdateLogEditorPage updateLogEditor && updateLogEditor.HasUnsavedChanges)
            {
                var dialog = new ContentDialog
                {
                    Title = "未保存的更改",
                    Content = "您有未保存的更改。关闭窗口将丢失这些修改。\n\n选择\"保存\"将保存后关闭\n选择\"不保存\"将直接关闭",
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
                        try { await updateLogEditor.TrySaveAsync(); }
                        catch { return false; }
                        return true;
                    case ContentDialogResult.Secondary:
                        return true;
                    default:
                        return false;
                }
            }

            return true;
        }

        private void NavView_Loaded(object sender, RoutedEventArgs e)
        {
            // Navigate to Dashboard by default
            ContentFrame.Navigate(typeof(DashboardPage));
            UpdateBreadcrumb("仪表板");

            // Select the first item
            foreach (NavigationViewItemBase item in NavView.MenuItems)
            {
                if (item is NavigationViewItem navItem && navItem.Tag?.ToString() == "Dashboard")
                {
                    NavView.SelectedItem = navItem;
                    break;
                }
            }

            // 启动后台版本检查
            _ = CheckForUpdatesAsync();
        }

        private void NavView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            string? tag = null;
            if (args.InvokedItemContainer != null)
                tag = args.InvokedItemContainer.Tag?.ToString();
            else if (args.IsSettingsInvoked)
                tag = "Settings";

            if (string.IsNullOrEmpty(tag)) return;

            if (_isInSubPage)
            {
                // 正在编辑子页面，需先走编辑器的返回检查
                _pendingNavigationTag = tag;

                if (ContentFrame.Content is ArticleEditorPage editorPage)
                    editorPage.OnBackClick();
                else if (ContentFrame.Content is UpdateLogEditorPage updateLogEditorPage)
                    updateLogEditorPage.OnBackClick();
                else
                    NavigateToPage(tag);
            }
            else
            {
                NavigateToPage(tag);
            }
        }

        private void NavigateToPage(string tag)
        {
            if (string.IsNullOrEmpty(tag)) return;

            Type pageType = tag switch
            {
                "Dashboard" => typeof(DashboardPage),
                "Articles" => typeof(ArticlesPage),
                "Categories" => typeof(CategoriesPage),
                "Comments" => typeof(CommentsPage),
                "Messages" => typeof(MessagesPage),
                "Widgets" => typeof(WidgetsPage),
                "Themes" => typeof(ThemesPage),
                "Config" => typeof(ConfigPage),
                "Announcements" => typeof(AnnouncementsPage),
                "UpdateLogs" => typeof(UpdateLogsPage),
                "Profile" => typeof(ProfilePage),
                "Settings" => typeof(SettingsPage),
                _ => null
            };

            if (pageType != null)
            {
                // 更新面包屑（单层页面只显示当前标题）
                if (_pageTitles.TryGetValue(tag, out var title))
                {
                    UpdateBreadcrumb(title);
                }

                // 更新工具栏
                UpdateToolbar(tag);

                // 导航到新页面
                ContentFrame.Navigate(pageType);

                // 创建淡入动画（通过Opacity）
                var fadeInAnimation = new Microsoft.UI.Xaml.Media.Animation.DoubleAnimation
                {
                    From = 0.0,
                    To = 1.0,
                    Duration = new Duration(TimeSpan.FromMilliseconds(300)),
                    EasingFunction = new Microsoft.UI.Xaml.Media.Animation.QuadraticEase { EasingMode = Microsoft.UI.Xaml.Media.Animation.EasingMode.EaseIn }
                };

                Storyboard.SetTarget(fadeInAnimation, ContentContainer);
                Storyboard.SetTargetProperty(fadeInAnimation, "Opacity");

                var storyboardIn = new Storyboard();
                storyboardIn.Children.Add(fadeInAnimation);
                storyboardIn.Begin();
            }
        }

        private void UpdateToolbar(string pageTag)
        {
            // 清空工具栏
            ToolbarContainer.Children.Clear();

            switch (pageTag)
            {
                case "Articles":
                    // 搜索框
                    var searchBox = new TextBox
                    {
                        PlaceholderText = "搜索文章标题...",
                        Width = 250
                    };
                    searchBox.TextChanged += (s, e) =>
                    {
                        if (ContentFrame.Content is ArticlesPage page)
                            page.OnSearchTextChanged(searchBox.Text);
                    };
                    ToolbarContainer.Children.Add(searchBox);

                    // 新建文章按钮
                    var createArticleBtn = new Button
                    {
                        Style = (Style)Application.Current.Resources["AccentButtonStyle"]
                    };
                    var articleBtnPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
                    articleBtnPanel.Children.Add(new SymbolIcon(Symbol.Add));
                    articleBtnPanel.Children.Add(new TextBlock { Text = "新建文章", VerticalAlignment = VerticalAlignment.Center });
                    createArticleBtn.Content = articleBtnPanel;
                    createArticleBtn.Click += (s, e) =>
                    {
                        if (ContentFrame.Content is ArticlesPage page)
                            page.OnCreateArticleClick();
                    };
                    ToolbarContainer.Children.Add(createArticleBtn);
                    break;

                case "Categories":
                    var createCategoryBtn = new Button
                    {
                        Style = (Style)Application.Current.Resources["AccentButtonStyle"]
                    };
                    var categoryBtnPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
                    categoryBtnPanel.Children.Add(new SymbolIcon(Symbol.Add));
                    categoryBtnPanel.Children.Add(new TextBlock { Text = "新建分类", VerticalAlignment = VerticalAlignment.Center });
                    createCategoryBtn.Content = categoryBtnPanel;
                    createCategoryBtn.Click += (s, e) =>
                    {
                        if (ContentFrame.Content is CategoriesPage page)
                            page.OnCreateCategoryClick();
                    };
                    ToolbarContainer.Children.Add(createCategoryBtn);
                    break;

                case "Comments":
                    var refreshCommentsBtn = CreateRefreshButton();
                    refreshCommentsBtn.Click += (s, e) =>
                    {
                        if (ContentFrame.Content is CommentsPage page)
                            page.OnRefreshClick();
                    };
                    ToolbarContainer.Children.Add(refreshCommentsBtn);
                    break;

                case "Messages":
                    var refreshMessagesBtn = CreateRefreshButton();
                    refreshMessagesBtn.Click += (s, e) =>
                    {
                        if (ContentFrame.Content is MessagesPage page)
                            page.OnRefreshClick();
                    };
                    ToolbarContainer.Children.Add(refreshMessagesBtn);
                    break;

                case "Widgets":
                    var refreshWidgetsBtn = CreateRefreshButton();
                    refreshWidgetsBtn.Click += (s, e) =>
                    {
                        if (ContentFrame.Content is WidgetsPage page)
                            page.OnRefreshClick();
                    };
                    ToolbarContainer.Children.Add(refreshWidgetsBtn);

                    var addWidgetBtn = new Button
                    {
                        Style = (Style)Application.Current.Resources["AccentButtonStyle"]
                    };
                    var widgetBtnPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
                    widgetBtnPanel.Children.Add(new SymbolIcon(Symbol.Add));
                    widgetBtnPanel.Children.Add(new TextBlock { Text = "添加小工具", VerticalAlignment = VerticalAlignment.Center });
                    addWidgetBtn.Content = widgetBtnPanel;
                    addWidgetBtn.Click += (s, e) =>
                    {
                        if (ContentFrame.Content is WidgetsPage page)
                            page.OnAddWidgetClick();
                    };
                    ToolbarContainer.Children.Add(addWidgetBtn);
                    break;

                case "Config":
                    var refreshConfigBtn = CreateRefreshButton();
                    refreshConfigBtn.Click += (s, e) =>
                    {
                        if (ContentFrame.Content is ConfigPage page)
                            page.OnRefreshClick();
                    };
                    ToolbarContainer.Children.Add(refreshConfigBtn);

                    var createConfigBtn = new Button
                    {
                        Style = (Style)Application.Current.Resources["AccentButtonStyle"]
                    };
                    var configBtnPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
                    configBtnPanel.Children.Add(new SymbolIcon(Symbol.Add));
                    configBtnPanel.Children.Add(new TextBlock { Text = "新建配置", VerticalAlignment = VerticalAlignment.Center });
                    createConfigBtn.Content = configBtnPanel;
                    createConfigBtn.Click += (s, e) =>
                    {
                        if (ContentFrame.Content is ConfigPage page)
                            page.OnCreateConfigClick();
                    };
                    ToolbarContainer.Children.Add(createConfigBtn);
                    break;

                case "Themes":
                    var addThemeBtn = new Button
                    {
                        Style = (Style)Application.Current.Resources["AccentButtonStyle"]
                    };
                    var themeBtnPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
                    themeBtnPanel.Children.Add(new SymbolIcon(Symbol.Add));
                    themeBtnPanel.Children.Add(new TextBlock { Text = "添加主题", VerticalAlignment = VerticalAlignment.Center });
                    addThemeBtn.Content = themeBtnPanel;
                    addThemeBtn.Click += (s, e) =>
                    {
                        if (ContentFrame.Content is ThemesPage page)
                            page.OnAddThemeClick();
                    };
                    ToolbarContainer.Children.Add(addThemeBtn);
                    break;

                case "Announcements":
                    var refreshAnnouncementsBtn = CreateRefreshButton();
                    refreshAnnouncementsBtn.Click += (s, e) =>
                    {
                        if (ContentFrame.Content is AnnouncementsPage page)
                            page.OnRefreshClick();
                    };
                    ToolbarContainer.Children.Add(refreshAnnouncementsBtn);

                    var createAnnouncementBtn = new Button
                    {
                        Style = (Style)Application.Current.Resources["AccentButtonStyle"]
                    };
                    var announcementBtnPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
                    announcementBtnPanel.Children.Add(new SymbolIcon(Symbol.Add));
                    announcementBtnPanel.Children.Add(new TextBlock { Text = "新建公告", VerticalAlignment = VerticalAlignment.Center });
                    createAnnouncementBtn.Content = announcementBtnPanel;
                    createAnnouncementBtn.Click += (s, e) =>
                    {
                        if (ContentFrame.Content is AnnouncementsPage page)
                            page.OnCreateClick();
                    };
                    ToolbarContainer.Children.Add(createAnnouncementBtn);
                    break;

                case "UpdateLogs":
                    var refreshLogsBtn = CreateRefreshButton();
                    refreshLogsBtn.Click += (s, e) =>
                    {
                        if (ContentFrame.Content is UpdateLogsPage page)
                            page.OnRefreshClick();
                    };
                    ToolbarContainer.Children.Add(refreshLogsBtn);

                    var createLogBtn = new Button
                    {
                        Style = (Style)Application.Current.Resources["AccentButtonStyle"]
                    };
                    var logBtnPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
                    logBtnPanel.Children.Add(new SymbolIcon(Symbol.Add));
                    logBtnPanel.Children.Add(new TextBlock { Text = "新建日志", VerticalAlignment = VerticalAlignment.Center });
                    createLogBtn.Content = logBtnPanel;
                    createLogBtn.Click += (s, e) =>
                    {
                        // 导航到更新日志编辑器（新建模式）
                        ContentFrame.Navigate(typeof(UpdateLogEditorPage));
                    };
                    ToolbarContainer.Children.Add(createLogBtn);
                    break;

                case "Dashboard":
                    var refreshDashboardBtn = CreateRefreshButton();
                    refreshDashboardBtn.Click += (s, e) =>
                    {
                        if (ContentFrame.Content is DashboardPage page)
                            page.OnRefreshClick();
                    };
                    ToolbarContainer.Children.Add(refreshDashboardBtn);
                    break;

                case "Profile":
                    var refreshProfileBtn = CreateRefreshButton();
                    refreshProfileBtn.Click += (s, e) =>
                    {
                        if (ContentFrame.Content is ProfilePage page)
                            page.OnRefreshClick();
                    };
                    ToolbarContainer.Children.Add(refreshProfileBtn);
                    break;

                case "Settings":
                default:
                    // 无工具栏
                    break;
            }
        }

        private Button CreateRefreshButton()
        {
            var btn = new Button();
            var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
            panel.Children.Add(new SymbolIcon(Symbol.Refresh));
            panel.Children.Add(new TextBlock { Text = "刷新", VerticalAlignment = VerticalAlignment.Center });
            btn.Content = panel;
            return btn;
        }

        /// <summary>
        /// TitleBar 窗格切换按钮点击事件
        /// </summary>
        private void TitleBar_PaneToggleRequested(Microsoft.UI.Xaml.Controls.TitleBar sender, object args)
        {
            NavView.IsPaneOpen = !NavView.IsPaneOpen;
        }

        #region 版本更新检查

        private async Task CheckForUpdatesAsync()
        {
            try
            {
                var app = Application.Current as App;
                if (app?.UpdateService == null) return;

                var result = await app.UpdateService.CheckForUpdateAsync();
                if (result == null || !result.HasUpdate) return;

                if (result.IsMajorUpdate)
                {
                    ShowForceUpdateOverlay(result);
                }
                else
                {
                    var skipped = app.UpdateService.GetSkippedVersion();
                    if (skipped == result.LatestVersion) return;

                    var dialog = new UpdateDialog(result) { XamlRoot = this.XamlRoot };
                    await dialog.ShowAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Update check failed: {ex.Message}");
            }
        }

        public void ShowForceUpdateOverlay(UpdateCheckResult result)
        {
            _forceUpdateResult = result;

            var app = Application.Current as App;
            var currentVersion = app?.UpdateService?.CurrentVersion ?? "unknown";

            ForceUpdateMessage.Text = $"检测到重大版本更新，当前版本 {currentVersion}，最新版本 {result.LatestVersion}。\n请更新后继续使用。";

            // 构建更新日志
            var sb = new StringBuilder();
            foreach (var release in result.NewReleases)
            {
                if (sb.Length > 0) sb.AppendLine();
                sb.AppendLine($"── {release.TagName} {release.Name} ──");
                if (!string.IsNullOrWhiteSpace(release.Body))
                    sb.AppendLine(release.Body.Trim());
            }
            ForceUpdateChangelog.Text = sb.Length > 0 ? sb.ToString() : "暂无更新说明";

            // 没有下载链接时禁用更新按钮，显示浏览器链接
            if (string.IsNullOrEmpty(result.DownloadUrl))
            {
                ForceUpdateBtn.IsEnabled = false;
                if (!string.IsNullOrEmpty(result.ReleasePageUrl))
                    ForceUpdateBrowserBtn.Visibility = Visibility.Visible;
            }

            ForceUpdateOverlay.Visibility = Visibility.Visible;
        }

        private async void ForceUpdateBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_forceUpdateResult?.DownloadUrl == null) return;

            var app = Application.Current as App;
            if (app?.UpdateService == null) return;

            ForceUpdateBtn.IsEnabled = false;
            ForceUpdateProgress.Visibility = Visibility.Visible;
            ForceUpdateProgressText.Visibility = Visibility.Visible;

            var cts = new CancellationTokenSource();
            var progress = new Progress<double>(percent =>
            {
                DispatcherQueue.TryEnqueue(() =>
                {
                    ForceUpdateProgress.Value = percent;
                    ForceUpdateProgressText.Text = $"{percent:F0}%";
                });
            });

            try
            {
                var msixPath = await app.UpdateService.DownloadUpdateAsync(
                    _forceUpdateResult.DownloadUrl, progress, cts.Token);
                await app.UpdateService.LaunchInstallerAsync(msixPath);
            }
            catch (Exception ex)
            {
                ForceUpdateProgressText.Text = $"下载失败: {ex.Message}";
                ForceUpdateBtn.IsEnabled = true;

                if (!string.IsNullOrEmpty(_forceUpdateResult.ReleasePageUrl))
                    ForceUpdateBrowserBtn.Visibility = Visibility.Visible;
            }
        }

        private async void ForceUpdateBrowserBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(_forceUpdateResult?.ReleasePageUrl))
            {
                await Windows.System.Launcher.LaunchUriAsync(new Uri(_forceUpdateResult.ReleasePageUrl));
            }
        }

        private void ExitAppBtn_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Exit();
        }

        #endregion
    }
}
