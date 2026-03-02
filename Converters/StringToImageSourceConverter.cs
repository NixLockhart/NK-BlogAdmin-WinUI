using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media.Imaging;
using System;

namespace Blog_Manager.Converters
{
    /// <summary>
    /// 将字符串URL或路径转换为ImageSource。
    /// 支持：完整URL（http/https）、URL路径（/files/...）、相对路径（images/...）
    /// </summary>
    public class StringToImageSourceConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is string urlString && !string.IsNullOrWhiteSpace(urlString))
            {
                try
                {
                    // 如果不是完整URL，通过 AppContext.GetFileUrl 拼接后端地址
                    if (!urlString.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                        !urlString.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                    {
                        urlString = Helpers.AppContext.GetFileUrl(urlString);
                    }

                    if (string.IsNullOrEmpty(urlString))
                        return null;

                    return new BitmapImage(new Uri(urlString));
                }
                catch
                {
                    return null;
                }
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
