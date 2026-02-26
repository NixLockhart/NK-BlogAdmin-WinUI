using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media.Imaging;
using System;

namespace Blog_Manager.Converters
{
    /// <summary>
    /// 将字符串URL转换为ImageSource（用于PersonPicture等控件）
    /// </summary>
    public class StringToImageSourceConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is string urlString && !string.IsNullOrWhiteSpace(urlString))
            {
                try
                {
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
