using CommunityToolkit.Mvvm.ComponentModel;
using Blog_Manager.Models;
using Blog_Manager.Services;
using Blog_Manager.Services.Api;
using Microsoft.UI.Xaml;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Blog_Manager.ViewModels
{
    public partial class MessagesViewModel : ObservableObject
    {
        private readonly IMessageApi _messageApi;
        private readonly IpLocationService _ipLocationService;

        [ObservableProperty]
        private ObservableCollection<Message> _messages = new();

        // 用于API调用的固定值
        private const int PageSize = 10000;

        public MessagesViewModel()
        {
            var app = (Application.Current as App) ?? throw new InvalidOperationException("App instance not found");
            _messageApi = app.ApiServiceFactory.CreateMessageApi();
            _ipLocationService = new IpLocationService();
        }

        public async Task LoadMessagesAsync()
        {
            try
            {
                // 获取所有留言，不分页
                var response = await _messageApi.GetMessagesAsync(0, PageSize);

                if (response.Code == 200 && response.Data != null)
                {
                    Messages = new ObservableCollection<Message>(response.Data.Content);

                    // 异步加载IP属地（不阻塞UI）
                    _ = LoadIpLocationsAsync();
                }
                else
                {
                    throw new Exception(response.Message ?? "加载留言列表失败");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"加载留言列表失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 异步加载所有留言的IP属地
        /// </summary>
        private async Task LoadIpLocationsAsync()
        {
            // 获取所有需要查询的IP地址（去重）
            var ips = Messages
                .Where(m => !string.IsNullOrWhiteSpace(m.IpAddress))
                .Select(m => m.IpAddress!)
                .Distinct()
                .ToArray();

            // 如果没有需要查询的IP，直接将所有消息标记为"未知"
            if (ips.Length == 0)
            {
                foreach (var message in Messages)
                {
                    message.IpLocation = "未知";
                }
                return;
            }

            // 批量查询IP属地
            var locations = await _ipLocationService.GetLocationsAsync(ips);

            // 更新留言的IP属地显示
            foreach (var message in Messages)
            {
                if (!string.IsNullOrWhiteSpace(message.IpAddress) &&
                    locations.TryGetValue(message.IpAddress, out var location))
                {
                    message.IpLocation = location;
                }
                else
                {
                    message.IpLocation = "未知";
                }
            }
        }

        public async Task ToggleFriendLinkAsync(long messageId)
        {
            try
            {
                var response = await _messageApi.ToggleFriendLinkAsync(messageId);
                if (response.Code != 200)
                {
                    throw new Exception(response.Message ?? "切换友链标记失败");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"切换友链标记失败: {ex.Message}");
            }
        }

        public async Task DeleteMessageAsync(long messageId)
        {
            try
            {
                var response = await _messageApi.DeleteMessageAsync(messageId);
                if (response.Code != 200)
                {
                    throw new Exception(response.Message ?? "删除留言失败");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"删除留言失败: {ex.Message}");
            }
        }

        public async Task PermanentlyDeleteMessageAsync(long messageId)
        {
            try
            {
                var response = await _messageApi.PermanentlyDeleteMessageAsync(messageId);
                if (response.Code != 200)
                {
                    throw new Exception(response.Message ?? "永久删除留言失败");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"永久删除留言失败: {ex.Message}");
            }
        }

        public async Task RestoreMessageAsync(long messageId)
        {
            try
            {
                var response = await _messageApi.RestoreMessageAsync(messageId);
                if (response.Code != 200)
                {
                    throw new Exception(response.Message ?? "恢复留言失败");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"恢复留言失败: {ex.Message}");
            }
        }
    }
}
