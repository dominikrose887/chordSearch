using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace ChordSearch.App.Converters;

public sealed class BoolToThicknessConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
        => value is true ? new Thickness(2) : new Thickness(0);

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}

public sealed class StringPrefixConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
        => $"String {value}";

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}
