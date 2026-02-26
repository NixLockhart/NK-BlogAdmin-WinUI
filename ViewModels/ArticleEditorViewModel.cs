using CommunityToolkit.Mvvm.ComponentModel;
using Blog_Manager.Models;
using Blog_Manager.Services.Api;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Markdig;
using Windows.System.Threading;
using Microsoft.UI.Dispatching;

namespace Blog_Manager.ViewModels
{
    public partial class ArticleEditorViewModel : ObservableObject
    {
        private readonly IArticleApi _articleApi;
        private readonly ICategoryApi _categoryApi;
        private readonly MarkdownPipeline _markdownPipeline;
        private ThreadPoolTimer _autoSaveTimer;
        private string _lastSavedContent = string.Empty;
        private string _lastSavedTitle = string.Empty;
        private readonly DispatcherQueue _dispatcherQueue;

        // 预览更新防抖定时器
        private DispatcherQueueTimer _previewDebounceTimer;
        private const int PreviewDebounceDelayMs = 600; // 600毫秒防抖延迟

        // 原始内容（用于回滚）- 打开文章时的初始状态
        private string _originalContent = string.Empty;
        private string _originalTitle = string.Empty;
        private string _originalSummary = string.Empty;
        private string _originalCoverImage = string.Empty;
        private long? _originalCategoryId;
        private int _originalStatus;

        [ObservableProperty]
        private long? _articleId;

        /// <summary>
        /// 当 ArticleId 改变时，通知依赖属性更新
        /// </summary>
        partial void OnArticleIdChanged(long? value)
        {
            OnPropertyChanged(nameof(IsEditMode));
            OnPropertyChanged(nameof(PageTitle));
        }

        [ObservableProperty]
        private string _title = string.Empty;

        [ObservableProperty]
        private string _content = string.Empty;

        [ObservableProperty]
        private string _summary = string.Empty;

        [ObservableProperty]
        private string _coverImage = string.Empty;

        [ObservableProperty]
        private long? _categoryId;

        [ObservableProperty]
        private int _status = 2; // 默认草稿

        [ObservableProperty]
        private List<Category> _categories = new();

        [ObservableProperty]
        private string _previewHtml = string.Empty;

        // 用于增量更新的纯内容 HTML（不包含完整页面结构）
        [ObservableProperty]
        private string _previewContentHtml = string.Empty;

        // 标记是否为首次加载（需要完整HTML）
        private bool _isFirstLoad = true;

        [ObservableProperty]
        private string _autoSaveStatus = string.Empty;

        [ObservableProperty]
        private bool _isAutoSaving = false;

        public bool IsEditMode => ArticleId.HasValue;

        public string PageTitle => IsEditMode ? "编辑文章" : "新建文章";

        /// <summary>
        /// 检查是否有未保存的更改（相对于打开文章时的原始状态）
        /// </summary>
        public bool HasUnsavedChanges =>
            Content != _originalContent ||
            Title != _originalTitle ||
            Summary != _originalSummary ||
            CoverImage != _originalCoverImage ||
            CategoryId != _originalCategoryId ||
            Status != _originalStatus;

        public ArticleEditorViewModel()
        {
            var app = (Application.Current as App) ?? throw new InvalidOperationException("App instance not found");
            _articleApi = app.ApiServiceFactory.CreateArticleApi();
            _categoryApi = app.ApiServiceFactory.CreateCategoryApi();
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

            // 初始化预览防抖定时器
            _previewDebounceTimer = _dispatcherQueue.CreateTimer();
            _previewDebounceTimer.Interval = TimeSpan.FromMilliseconds(PreviewDebounceDelayMs);
            _previewDebounceTimer.IsRepeating = false;
            _previewDebounceTimer.Tick += (s, e) => UpdatePreview();

            // 不使用数学扩展，我们手动处理LaTeX
            _markdownPipeline = new MarkdownPipelineBuilder()
                .UseAutoLinks()
                .UseEmphasisExtras()
                .UseGridTables()
                .UsePipeTables()
                .UseListExtras()
                .UseTaskLists()
                .UseAutoIdentifiers()
                .Build();

            // 启动自动保存定时器（每5秒）
            StartAutoSaveTimer();
        }

        private void StartAutoSaveTimer()
        {
            _autoSaveTimer = ThreadPoolTimer.CreatePeriodicTimer(
                async (timer) =>
                {
                    await TryAutoSaveAsync();
                },
                TimeSpan.FromSeconds(5)
            );
        }

        public void StopAutoSaveTimer()
        {
            _autoSaveTimer?.Cancel();
            _previewDebounceTimer?.Stop();
        }

        private async Task TryAutoSaveAsync()
        {
            try
            {
                // 检查是否有内容需要保存
                bool hasContent = !string.IsNullOrWhiteSpace(Content);
                bool contentChanged = Content != _lastSavedContent || Title != _lastSavedTitle;

                if (!hasContent || !contentChanged || IsAutoSaving)
                {
                    return;
                }

                // 标记为保存中
                _dispatcherQueue.TryEnqueue(() =>
                {
                    IsAutoSaving = true;
                    AutoSaveStatus = "保存中...";
                });

                // 执行自动保存
                var request = new ArticleSaveRequest
                {
                    Title = string.IsNullOrWhiteSpace(Title) ? null : Title,
                    Content = Content,
                    Summary = Summary,
                    CoverImage = string.IsNullOrWhiteSpace(CoverImage) ? null : CoverImage,
                    CategoryId = CategoryId,
                    Status = 2 // 自动保存始终保存为草稿
                };

                if (ArticleId.HasValue)
                {
                    // 更新现有文章
                    await _articleApi.UpdateArticleAsync(ArticleId.Value, request);
                }
                else
                {
                    // 创建新文章
                    var response = await _articleApi.CreateArticleAsync(request);
                    if (response.Code == 200 && response.Data?.Id != null)
                    {
                        _dispatcherQueue.TryEnqueue(() =>
                        {
                            ArticleId = response.Data.Id;
                            OnPropertyChanged(nameof(IsEditMode));
                            OnPropertyChanged(nameof(PageTitle));
                        });
                    }
                }

                // 更新上次保存的内容
                _lastSavedContent = Content;
                _lastSavedTitle = Title;

                // 更新状态
                var currentTime = DateTime.Now.ToString("HH:mm:ss");
                _dispatcherQueue.TryEnqueue(() =>
                {
                    AutoSaveStatus = $"已自动保存 {currentTime}";
                });
            }
            catch (Exception ex)
            {
                // 保存失败，显示错误信息
                _dispatcherQueue.TryEnqueue(() =>
                {
                    AutoSaveStatus = $"自动保存失败: {ex.Message}";
                });
            }
            finally
            {
                _dispatcherQueue.TryEnqueue(() =>
                {
                    IsAutoSaving = false;
                });
            }
        }

        partial void OnContentChanged(string value)
        {
            // 使用防抖：重置定时器，等待用户停止输入后再更新预览
            _previewDebounceTimer.Stop();
            _previewDebounceTimer.Start();
        }

        public async Task LoadCategoriesAsync()
        {
            try
            {
                var response = await _categoryApi.GetCategoriesAsync();
                if (response.Code == 200 && response.Data != null)
                {
                    Categories = response.Data;
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"加载分类列表失败: {ex.Message}");
            }
        }

        public async Task LoadArticleAsync(long articleId)
        {
            try
            {
                ArticleId = articleId;
                var response = await _articleApi.GetArticleAsync(articleId);

                if (response.Code == 200 && response.Data != null)
                {
                    var article = response.Data;
                    Title = article.Title;
                    Summary = article.Summary ?? string.Empty;
                    CoverImage = article.CoverImage ?? string.Empty;
                    CategoryId = article.CategoryId;
                    Status = article.Status;
                    Content = article.MarkdownContent ?? article.Content ?? string.Empty;

                    // 记录已加载的内容，避免自动保存时误判为修改
                    _lastSavedContent = Content;
                    _lastSavedTitle = Title;

                    // 记录原始内容（用于回滚功能）
                    _originalContent = Content;
                    _originalTitle = Title;
                    _originalSummary = Summary;
                    _originalCoverImage = CoverImage;
                    _originalCategoryId = CategoryId;
                    _originalStatus = Status;
                }
                else
                {
                    throw new Exception(response.Message ?? "加载文章失败");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"加载文章失败: {ex.Message}");
            }
        }

        public async Task<bool> SaveArticleAsync()
        {
            try
            {
                var request = new ArticleSaveRequest
                {
                    Title = Title,
                    Content = Content,
                    Summary = Summary,
                    CoverImage = string.IsNullOrWhiteSpace(CoverImage) ? null : CoverImage,
                    CategoryId = CategoryId,
                    Status = Status
                };

                if (IsEditMode)
                {
                    var response = await _articleApi.UpdateArticleAsync(ArticleId!.Value, request);
                    if (response.Code != 200)
                    {
                        throw new Exception(response.Message ?? "更新文章失败");
                    }
                }
                else
                {
                    var response = await _articleApi.CreateArticleAsync(request);
                    if (response.Code != 200)
                    {
                        throw new Exception(response.Message ?? "创建文章失败");
                    }
                    ArticleId = response.Data?.Id;
                }

                // 保存成功后更新原始内容（防止再次回滚）
                _originalContent = Content;
                _originalTitle = Title;
                _originalSummary = Summary;
                _originalCoverImage = CoverImage;
                _originalCategoryId = CategoryId;
                _originalStatus = Status;

                // 同时更新自动保存的追踪字段
                _lastSavedContent = Content;
                _lastSavedTitle = Title;

                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"保存文章失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 回滚到打开文章时的原始状态
        /// </summary>
        public async Task<bool> RollbackToOriginalAsync()
        {
            try
            {
                // 如果不是编辑模式（新建文章），直接返回true，无需回滚
                if (!IsEditMode)
                {
                    return true;
                }

                // 恢复到原始内容并保存到服务器
                var request = new ArticleSaveRequest
                {
                    Title = _originalTitle,
                    Content = _originalContent,
                    Summary = _originalSummary,
                    CoverImage = string.IsNullOrWhiteSpace(_originalCoverImage) ? null : _originalCoverImage,
                    CategoryId = _originalCategoryId,
                    Status = _originalStatus
                };

                var response = await _articleApi.UpdateArticleAsync(ArticleId!.Value, request);
                if (response.Code != 200)
                {
                    throw new Exception(response.Message ?? "回滚失败");
                }

                // 恢复本地状态
                Title = _originalTitle;
                Content = _originalContent;
                Summary = _originalSummary;
                CoverImage = _originalCoverImage;
                CategoryId = _originalCategoryId;
                Status = _originalStatus;

                // 更新自动保存的追踪字段
                _lastSavedContent = Content;
                _lastSavedTitle = Title;

                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"回滚失败: {ex.Message}");
            }
        }

        private void UpdatePreview()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(Content))
                {
                    var emptyContent = "<p style='color: #999; text-align: center; padding: 40px;'>在左侧输入 Markdown 内容，这里将显示预览...</p>";
                    if (_isFirstLoad)
                    {
                        PreviewHtml = GetStyledHtml(emptyContent);
                        _isFirstLoad = false;
                    }
                    else
                    {
                        PreviewContentHtml = emptyContent;
                    }
                    return;
                }

                // 步骤1: 提取LaTeX公式，用占位符替换
                var (processedContent, formulaMap) = ExtractLatexFormulas(Content);

                // 步骤2: 渲染Markdown
                var html = Markdown.ToHtml(processedContent, _markdownPipeline);

                // 步骤3: 替换占位符为LaTeX公式
                html = ReplaceLatexPlaceholders(html, formulaMap);

                if (_isFirstLoad)
                {
                    // 首次加载，发送完整HTML
                    PreviewHtml = GetStyledHtml(html);
                    _isFirstLoad = false;
                }
                else
                {
                    // 后续更新，只发送内容部分
                    PreviewContentHtml = html;
                }
            }
            catch (Exception)
            {
                var errorContent = "<p style='color: #d73a49; text-align: center; padding: 40px;'>Markdown 解析错误</p>";
                if (_isFirstLoad)
                {
                    PreviewHtml = GetStyledHtml(errorContent);
                    _isFirstLoad = false;
                }
                else
                {
                    PreviewContentHtml = errorContent;
                }
            }
        }

        /// <summary>
        /// 重置预览状态，强制下次完整加载
        /// </summary>
        public void ResetPreviewState()
        {
            _isFirstLoad = true;
        }

        /// <summary>
        /// 提取LaTeX公式，用占位符替换
        /// </summary>
        private (string content, Dictionary<string, (string formula, bool isBlock)>) ExtractLatexFormulas(string content)
        {
            var formulaMap = new Dictionary<string, (string formula, bool isBlock)>();
            int counter = 0;

            // 先处理块级公式 $$...$$
            content = System.Text.RegularExpressions.Regex.Replace(
                content,
                @"\$\$([\s\S]+?)\$\$",
                match => {
                    var placeholder = $"KATEXBLOCKFORMULA{counter}ENDBLOCK";
                    formulaMap[placeholder] = (match.Groups[1].Value.Trim(), true);
                    counter++;
                    return "\n" + placeholder + "\n"; // 独立成行
                });

            // 再处理行内公式 $...$
            content = System.Text.RegularExpressions.Regex.Replace(
                content,
                @"\$([^\$\n]+?)\$",
                match => {
                    var placeholder = $"KATEXINLINEFORMULA{counter}ENDINLINE";
                    formulaMap[placeholder] = (match.Groups[1].Value.Trim(), false);
                    counter++;
                    return placeholder;
                });

            return (content, formulaMap);
        }

        /// <summary>
        /// 替换占位符为LaTeX公式
        /// </summary>
        private string ReplaceLatexPlaceholders(string html, Dictionary<string, (string formula, bool isBlock)> formulaMap)
        {
            foreach (var kvp in formulaMap)
            {
                var placeholder = kvp.Key;
                var (formula, isBlock) = kvp.Value;

                // 对公式中的特殊HTML字符进行转义
                var escapedFormula = System.Net.WebUtility.HtmlEncode(formula);

                if (isBlock)
                {
                    // 块级公式：输出为带data属性的div，JavaScript会处理
                    var replacement = $"<div class='latex-formula' data-formula='{escapedFormula}' data-display='true'></div>";
                    // 移除可能的<p>标签包裹
                    html = html.Replace($"<p>{placeholder}</p>", replacement);
                    html = html.Replace(placeholder, replacement);
                }
                else
                {
                    // 行内公式：输出为带data属性的span
                    var replacement = $"<span class='latex-formula' data-formula='{escapedFormula}' data-display='false'></span>";
                    html = html.Replace(placeholder, replacement);
                }
            }

            return html;
        }

        private string GetStyledHtml(string content)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <!-- KaTeX CSS for LaTeX rendering -->
    <link rel='stylesheet' href='https://cdn.jsdelivr.net/npm/katex@0.16.9/dist/katex.min.css'>
    <style>
        html, body {{
            margin: 0;
            padding: 0;
            height: 100%;
        }}
        body {{
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', 'Microsoft YaHei', sans-serif;
            line-height: 1.6;
            padding: 20px;
            color: #333;
            background: #fff;
            box-sizing: border-box;
        }}
        #content-container {{
            /* 内容容器 */
        }}
        h1, h2, h3, h4, h5, h6 {{
            margin-top: 24px;
            margin-bottom: 16px;
            font-weight: 600;
            line-height: 1.25;
        }}
        h1 {{ font-size: 2em; border-bottom: 1px solid #eee; padding-bottom: 0.3em; }}
        h2 {{ font-size: 1.5em; border-bottom: 1px solid #eee; padding-bottom: 0.3em; }}
        h3 {{ font-size: 1.25em; }}
        code {{
            background: #f6f8fa;
            padding: 0.2em 0.4em;
            border-radius: 3px;
            font-family: 'Consolas', 'Monaco', monospace;
            font-size: 85%;
        }}
        pre {{
            background: #f6f8fa;
            padding: 16px;
            overflow: auto;
            border-radius: 6px;
        }}
        pre code {{
            background: transparent;
            padding: 0;
        }}
        blockquote {{
            border-left: 4px solid #ddd;
            padding-left: 16px;
            color: #666;
            margin: 0;
        }}
        table {{
            border-collapse: collapse;
            width: 100%;
            margin: 16px 0;
        }}
        table th, table td {{
            border: 1px solid #ddd;
            padding: 8px 12px;
        }}
        table th {{
            background: #f6f8fa;
            font-weight: 600;
        }}
        img {{
            max-width: 100%;
            height: auto;
        }}
        a {{
            color: #0969da;
            text-decoration: none;
        }}
        a:hover {{
            text-decoration: underline;
        }}
        /* KaTeX display mode */
        .katex-display {{
            margin: 1em 0;
            overflow-x: auto;
            overflow-y: hidden;
            text-align: center;
        }}
        .latex-formula {{
            /* 公式容器 */
        }}
        .latex-formula[data-display='true'] {{
            display: block;
            margin: 1em 0;
            text-align: center;
            overflow-x: auto;
        }}
        .latex-formula[data-display='false'] {{
            display: inline;
        }}
        .latex-display {{
            margin: 1em 0;
            overflow-x: auto;
            overflow-y: hidden;
            text-align: center;
        }}
        .latex-inline {{
            /* 行内公式样式 */
        }}
        .katex-block {{
            margin: 1em 0;
            overflow-x: auto;
            overflow-y: hidden;
            text-align: center;
        }}
        .katex-error {{
            color: #d73a49;
            font-style: italic;
        }}
    </style>
</head>
<body>
<div id='content-container'>{content}</div>
<script src='https://cdn.jsdelivr.net/npm/katex@0.16.9/dist/katex.min.js'></script>
<script src='https://cdn.jsdelivr.net/npm/katex@0.16.9/dist/contrib/auto-render.min.js'></script>
<script>
    // 渲染自定义LaTeX公式元素
    function renderCustomLatexFormulas(container) {{
        container = container || document;
        const formulas = container.querySelectorAll('.latex-formula');

        formulas.forEach(element => {{
            const formula = element.getAttribute('data-formula');
            const displayMode = element.getAttribute('data-display') === 'true';

            if (formula && typeof katex !== 'undefined') {{
                try {{
                    katex.render(formula, element, {{
                        displayMode: displayMode,
                        throwOnError: false,
                        errorColor: '#d73a49'
                    }});
                }} catch (e) {{
                    element.textContent = 'Error: ' + e.message;
                    element.style.color = '#d73a49';
                }}
            }}
        }});
    }}

    // 等待KaTeX和auto-render加载完成
    function initMath() {{
        if (typeof katex !== 'undefined' && typeof renderMathInElement !== 'undefined') {{
            try {{
                renderCustomLatexFormulas();
                renderMathInElement(document.body, {{
                    delimiters: [
                        {{left: '$$', right: '$$', display: true}},
                        {{left: '$', right: '$', display: false}},
                        {{left: '\\\\[', right: '\\\\]', display: true}},
                        {{left: '\\\\(', right: '\\\\)', display: false}}
                    ],
                    throwOnError: false,
                    errorColor: '#d73a49'
                }});
            }} catch (e) {{
                console.error('KaTeX渲染错误:', e);
            }}
        }} else {{
            setTimeout(initMath, 100);
        }}
    }}

    // 增量更新内容（由C#调用）
    function updateContent(newHtml) {{
        const container = document.getElementById('content-container');
        if (container) {{
            container.innerHTML = newHtml;
            // 重新渲染LaTeX公式
            if (typeof katex !== 'undefined') {{
                renderCustomLatexFormulas(container);
                if (typeof renderMathInElement !== 'undefined') {{
                    renderMathInElement(container, {{
                        delimiters: [
                            {{left: '$$', right: '$$', display: true}},
                            {{left: '$', right: '$', display: false}},
                            {{left: '\\\\[', right: '\\\\]', display: true}},
                            {{left: '\\\\(', right: '\\\\)', display: false}}
                        ],
                        throwOnError: false,
                        errorColor: '#d73a49'
                    }});
                }}
            }}
        }}
    }}

    // 同步滚动（由C#调用）
    function syncScroll(scrollPercentage) {{
        const maxScroll = document.documentElement.scrollHeight - document.documentElement.clientHeight;
        const targetScroll = maxScroll * scrollPercentage;
        window.scrollTo({{
            top: targetScroll,
            behavior: 'auto'
        }});
    }}

    // 获取当前滚动百分比
    function getScrollPercentage() {{
        const maxScroll = document.documentElement.scrollHeight - document.documentElement.clientHeight;
        if (maxScroll <= 0) return 0;
        return window.scrollY / maxScroll;
    }}

    // 页面加载完成后渲染
    if (document.readyState === 'loading') {{
        document.addEventListener('DOMContentLoaded', initMath);
    }} else {{
        initMath();
    }}
</script>
</body>
</html>";
        }

        private string GetEmptyPreviewHtml()
        {
            return GetStyledHtml("<p style='color: #999; text-align: center; padding: 40px;'>在左侧输入 Markdown 内容，这里将显示预览...</p>");
        }

        private string GetErrorPreviewHtml()
        {
            return GetStyledHtml("<p style='color: #d73a49; text-align: center; padding: 40px;'>Markdown 解析错误</p>");
        }
    }
}
