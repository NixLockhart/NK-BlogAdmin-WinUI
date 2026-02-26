using Microsoft.UI.Xaml.Data;
using System;

namespace Blog_Manager.Converters
{
    public class StringToUriConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is string urlString && !string.IsNullOrWhiteSpace(urlString))
            {
                try
                {
                    return new Uri(urlString);
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
