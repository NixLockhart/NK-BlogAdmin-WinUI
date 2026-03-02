using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Blog_Manager.Services
{
    /// <summary>
    /// Toast 风格通知服务 - 在应用顶部居中显示 InfoBar 通知
    /// 支持自动消失、堆叠、最多同时显示 3 个
    /// </summary>
    public class NotificationService
    {
        private Panel? _container;
        private DispatcherQueue? _dispatcher;
        private readonly List<FrameworkElement> _activeNotifications = new();
        private const int MaxVisible = 3;
        private const int AutoDismissMs = 3000;
        private const double NotificationWidth = 450;
        private const int EnterDurationMs = 300;
        private const int ExitDurationMs = 300;

        /// <summary>
        /// 初始化通知服务
        /// </summary>
        public void Initialize(Panel container, DispatcherQueue dispatcher)
        {
            _container = container;
            _dispatcher = dispatcher;
        }

        public bool IsInitialized => _container != null && _dispatcher != null;

        public void ShowSuccess(string message) => Show(message, InfoBarSeverity.Success);
        public void ShowError(string message) => Show(message, InfoBarSeverity.Error);
        public void ShowWarning(string message) => Show(message, InfoBarSeverity.Warning);
        public void ShowInfo(string message) => Show(message, InfoBarSeverity.Informational);

        private void Show(string message, InfoBarSeverity severity)
        {
            if (_container == null || _dispatcher == null) return;

            if (!_dispatcher.HasThreadAccess)
            {
                _dispatcher.TryEnqueue(() => Show(message, severity));
                return;
            }

            // 创建 InfoBar
            var infoBar = new InfoBar
            {
                Message = message,
                Severity = severity,
                IsOpen = true,
                IsClosable = true,
                Width = NotificationWidth,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            // 包裹在 Grid 中用于动画控制
            var wrapper = new Grid
            {
                Opacity = 0,
                RenderTransform = new TranslateTransform { Y = -20 }
            };
            wrapper.Children.Add(infoBar);

            // 手动关闭时移除
            infoBar.CloseButtonClick += (s, e) =>
            {
                DismissNotification(wrapper, fast: true);
            };

            // 超过最大数量时，立即淡出最旧的
            while (_activeNotifications.Count >= MaxVisible)
            {
                var oldest = _activeNotifications[0];
                DismissNotification(oldest, fast: true);
            }

            // 添加到容器和列表
            _activeNotifications.Add(wrapper);
            _container.Children.Add(wrapper);

            // 入场动画
            PlayEnterAnimation(wrapper);

            // 自动消失定时器
            _ = AutoDismissAsync(wrapper);
        }

        private void PlayEnterAnimation(FrameworkElement wrapper)
        {
            var transform = wrapper.RenderTransform as TranslateTransform;
            if (transform == null) return;

            // Opacity 动画
            var fadeIn = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = new Duration(TimeSpan.FromMilliseconds(EnterDurationMs)),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            Storyboard.SetTarget(fadeIn, wrapper);
            Storyboard.SetTargetProperty(fadeIn, "Opacity");

            // TranslateY 动画
            var slideIn = new DoubleAnimation
            {
                From = -20,
                To = 0,
                Duration = new Duration(TimeSpan.FromMilliseconds(EnterDurationMs)),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            Storyboard.SetTarget(slideIn, wrapper);
            Storyboard.SetTargetProperty(slideIn, "(UIElement.RenderTransform).(TranslateTransform.Y)");

            var storyboard = new Storyboard();
            storyboard.Children.Add(fadeIn);
            storyboard.Children.Add(slideIn);
            storyboard.Begin();
        }

        private void DismissNotification(FrameworkElement wrapper, bool fast = false)
        {
            if (_container == null) return;

            // 已经被移除了
            if (!_activeNotifications.Contains(wrapper))
                return;

            // 先从列表移除，防止重复触发
            _activeNotifications.Remove(wrapper);

            if (fast)
            {
                // 快速移除（超出上限时）- 播放短动画
                PlayExitAnimation(wrapper, ExitDurationMs / 2);
            }
            else
            {
                // 正常自动消失 - 播放完整动画
                PlayExitAnimation(wrapper, ExitDurationMs);
            }
        }

        private void PlayExitAnimation(FrameworkElement wrapper, int durationMs)
        {
            if (_container == null) return;

            var transform = wrapper.RenderTransform as TranslateTransform;
            if (transform == null)
            {
                // 无法动画，直接移除
                _container.Children.Remove(wrapper);
                return;
            }

            // Opacity 动画
            var fadeOut = new DoubleAnimation
            {
                To = 0,
                Duration = new Duration(TimeSpan.FromMilliseconds(durationMs)),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
            };
            Storyboard.SetTarget(fadeOut, wrapper);
            Storyboard.SetTargetProperty(fadeOut, "Opacity");

            // TranslateY 动画（向上滑出）
            var slideOut = new DoubleAnimation
            {
                To = -30,
                Duration = new Duration(TimeSpan.FromMilliseconds(durationMs)),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
            };
            Storyboard.SetTarget(slideOut, wrapper);
            Storyboard.SetTargetProperty(slideOut, "(UIElement.RenderTransform).(TranslateTransform.Y)");

            var storyboard = new Storyboard();
            storyboard.Children.Add(fadeOut);
            storyboard.Children.Add(slideOut);

            storyboard.Completed += (s, e) =>
            {
                _container?.Children.Remove(wrapper);
            };

            storyboard.Begin();
        }

        private async Task AutoDismissAsync(FrameworkElement wrapper)
        {
            await Task.Delay(AutoDismissMs);

            _dispatcher?.TryEnqueue(() =>
            {
                // 检查是否仍在活跃列表（可能已被手动关闭或因超出上限被移除）
                if (_activeNotifications.Contains(wrapper))
                {
                    DismissNotification(wrapper, fast: false);
                }
            });
        }
    }
}
