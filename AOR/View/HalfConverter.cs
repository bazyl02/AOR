using System;
using System.Globalization;
using System.Windows.Data;

namespace AOR.View
{
    public class HalfConverter : IValueConverter
    {
        public object Convert(object value, Type  targetType,
            object parameter, CultureInfo culture)
        {
            return (double)value/2.0d;
        }

        public object ConvertBack(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            return null;
        }
    }
    
    public class HalfValueConverter1 : IMultiValueConverter
    {
        #region IMultiValueConverter Members

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        { 
            double totalWidth = (double)values[0];
            double width = (double)values[1];
            return totalWidth / 2.0d - width - (0.25d * (totalWidth - 2.0d * width));
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
    
    public class HalfValueConverter2 : IMultiValueConverter
    {
        #region IMultiValueConverter Members

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        { 
            double totalWidth = (double)values[0];
            double width = (double)values[1];
            return totalWidth / 2.0d - (0.25d * (totalWidth - 2.0d * width));
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}