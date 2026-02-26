using Microsoft.UI;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using System;

namespace Blog_Manager.Converters
{
    /// <summary>
    /// Converts bool test result to color (green for success, red for failure)
    /// </summary>
    public class BoolToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool boolValue)
            {
                return new SolidColorBrush(boolValue ? Colors.Green : Colors.Red);
            }

            if (value != null && value.GetType() == typeof(bool?))
            {
                var nullableBool = (bool?)value;
                if (nullableBool.HasValue)
                {
                    return new SolidColorBrush(nullableBool.Value ? Colors.Green : Colors.Red);
                }
            }

            return new SolidColorBrush(Colors.Gray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
