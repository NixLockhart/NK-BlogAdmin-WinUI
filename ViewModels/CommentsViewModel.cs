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
    public partial class CommentsViewModel : ObservableObject
    {
        private readonly ICommentApi _commentApi;
        private readonly IArticleApi _articleApi;
        private readonly IpLocationService _ipLocationService;

        [ObservableProperty]
        private ObservableCollection<Comment> _comments = new();

        [ObservableProperty]
        private ObservableCollection<Article> _articles = new();

        [ObservableProperty]
        private long? _filterArticleId = null;

        [ObservableProperty]
        private int? _filterStatus = null;

        [ObservableProperty]
        private string _sortOrder = "desc";

        // 用于API调用的固定值
        private const int PageSize = 10000;

        public CommentsViewModel()
        {
            var app = (Application.Current as App) ?? throw new InvalidOperationException("App instance not found");
            _commentApi = app.ApiServiceFactory.CreateCommentApi();
            _articleApi = app.ApiServiceFactory.CreateArticleApi();
            _ipLocationService = new IpLocationService();
        }

        public async Task LoadCommentsAsync()
        {
            try
            {
                // 获取所有评论，不分页
                var response = await _commentApi.GetCommentsAsync(
                    0,
                    PageSize,
                    FilterArticleId,
                    FilterStatus,
                    SortOrder);

                if (response.Code == 200 && response.Data != null)
                {
                    // Flatten the tree structure for display
                    var flatList = FlattenComments(response.Data.Content ?? new System.Collections.Generic.List<Comment>());
                    Comments = new ObservableCollection<Comment>(flatList);

                    // 异步加载IP属地（不阻塞UI）
                    _ = LoadIpLocationsAsync();
                }
                else
                {
                    throw new Exception(response.Message ?? "加载评论列表失败");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"加载评论列表失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 异步加载所有评论的IP属地
        /// </summary>
        private async Task LoadIpLocationsAsync()
        {
            // 获取所有需要查询的IP地址（去重）
            var ips = Comments
                .Where(c => !string.IsNullOrWhiteSpace(c.IpAddress))
                .Select(c => c.IpAddress!)
                .Distinct()
                .ToArray();

            if (ips.Length == 0)
                return;

            // 批量查询IP属地
            var locations = await _ipLocationService.GetLocationsAsync(ips);

            // 更新评论的IP属地显示
            foreach (var comment in Comments)
            {
                if (!string.IsNullOrWhiteSpace(comment.IpAddress) &&
                    locations.TryGetValue(comment.IpAddress, out var location))
                {
                    comment.IpLocation = location;
                }
                else
                {
                    comment.IpLocation = "未知";
                }
            }
        }

        public async Task LoadArticlesAsync()
        {
            try
            {
                var response = await _articleApi.GetArticlesAsync(page: 0, size: 1000);
                if (response.Code == 200 && response.Data != null)
                {
                    Articles = new ObservableCollection<Article>(response.Data.Content ?? new System.Collections.Generic.List<Article>());
                }
            }
            catch (Exception)
            {
                // Silently fail if articles can't be loaded
                Articles = new ObservableCollection<Article>();
            }
        }

        private System.Collections.Generic.List<Comment> FlattenComments(System.Collections.Generic.List<Comment> comments, int level = 0)
        {
            var result = new System.Collections.Generic.List<Comment>();

            if (comments == null || comments.Count == 0)
            {
                return result;
            }

            foreach (var comment in comments)
            {
                // Create a new comment for display without children reference
                var displayComment = new Comment
                {
                    Id = comment.Id,
                    ArticleId = comment.ArticleId,
                    ArticleTitle = comment.ArticleTitle,
                    ParentId = comment.ParentId,
                    ReplyToNickname = comment.ReplyToNickname,
                    Nickname = new string('　', level) + comment.Nickname,
                    Email = comment.Email,
                    Website = comment.Website,
                    Avatar = comment.Avatar,
                    Content = comment.Content,
                    IpAddress = comment.IpAddress,
                    UserAgent = comment.UserAgent,
                    Status = comment.Status,
                    CreatedAt = comment.CreatedAt,
                    DeletedAt = comment.DeletedAt,
                    Children = new System.Collections.Generic.List<Comment>() // Empty list, not null
                };
                result.Add(displayComment);

                // Recursively flatten children
                if (comment.Children != null && comment.Children.Count > 0)
                {
                    result.AddRange(FlattenComments(comment.Children, level + 1));
                }
            }
            return result;
        }

        public async Task ApproveCommentAsync(long commentId)
        {
            try
            {
                var response = await _commentApi.UpdateCommentStatusAsync(commentId, 1);
                if (response.Code != 200)
                {
                    throw new Exception(response.Message ?? "审核评论失败");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"审核评论失败: {ex.Message}");
            }
        }

        public async Task RejectCommentAsync(long commentId)
        {
            try
            {
                var response = await _commentApi.UpdateCommentStatusAsync(commentId, 2);
                if (response.Code != 200)
                {
                    throw new Exception(response.Message ?? "标记待审核失败");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"标记待审核失败: {ex.Message}");
            }
        }

        public async Task DeleteCommentAsync(long commentId)
        {
            try
            {
                var response = await _commentApi.DeleteCommentAsync(commentId);
                if (response.Code != 200)
                {
                    throw new Exception(response.Message ?? "删除评论失败");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"删除评论失败: {ex.Message}");
            }
        }

        public async Task PermanentlyDeleteCommentAsync(long commentId)
        {
            try
            {
                var response = await _commentApi.PermanentlyDeleteCommentAsync(commentId);
                if (response.Code != 200)
                {
                    throw new Exception(response.Message ?? "永久删除评论失败");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"永久删除评论失败: {ex.Message}");
            }
        }

        public async Task RestoreCommentAsync(long commentId)
        {
            try
            {
                var response = await _commentApi.RestoreCommentAsync(commentId);
                if (response.Code != 200)
                {
                    throw new Exception(response.Message ?? "恢复评论失败");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"恢复评论失败: {ex.Message}");
            }
        }
    }
}
