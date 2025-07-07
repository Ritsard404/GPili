using GPili.Presentation.Features.Cashiering;
using System.Globalization;

namespace GPili.Converters
{
    public class InverseBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is bool b ? !b : value;
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => value is bool b ? !b : value;
    }
    public class GreaterThanZeroToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is IConvertible)
            {
                try
                {
                    var number = System.Convert.ToDecimal(value, culture);
                    return number > 0;
                }
                catch
                {
                    // Ignore conversion errors and fall through
                }
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
    public class ItemIndexConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var item = value;
            var collectionView = parameter as CollectionView;
            if (item == null || collectionView == null)
                return null;

            var items = collectionView.ItemsSource as System.Collections.IList;
            if (items == null)
                return null;

            int index = items.IndexOf(item);
            return (index >= 0 ? (index + 1).ToString() + ")" : string.Empty); // 1-based index
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
    public class HasValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return false;

            if (value is string str)
                return !string.IsNullOrWhiteSpace(str);

            return true;
        }


        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
