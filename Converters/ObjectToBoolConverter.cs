using Microsoft.UI.Xaml.Data;
using System;

namespace Blog_Manager.Converters
{
    /// <summary>
    /// Converts an object to bool. Returns true if object is not null, false otherwise.
    /// </summary>
    public class ObjectToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return value != null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
