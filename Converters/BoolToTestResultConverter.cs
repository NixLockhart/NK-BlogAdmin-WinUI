using Microsoft.UI.Xaml.Data;
using System;

namespace Blog_Manager.Converters
{
    /// <summary>
    /// Converts bool test result to Chinese readable string
    /// </summary>
    public class BoolToTestResultConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool boolValue)
            {
                return boolValue ? "成功" : "失败";
            }

            if (value != null && value.GetType() == typeof(bool?))
            {
                var nullableBool = (bool?)value;
                if (nullableBool.HasValue)
                {
                    return nullableBool.Value ? "成功" : "失败";
                }
            }

            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
