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
}
