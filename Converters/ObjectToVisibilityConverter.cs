using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;

namespace Blog_Manager.Converters
{
    /// <summary>
    /// Converts an object to Visibility. Returns Visible if object is not null, Collapsed otherwise.
    /// </summary>
    public class ObjectToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return value != null ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
