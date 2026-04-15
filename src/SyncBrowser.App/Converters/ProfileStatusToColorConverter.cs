using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using SyncBrowser.Core.Enums;

namespace SyncBrowser.App.Converters;

public class ProfileStatusToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is ProfileStatus status)
        {
            return status switch
            {
                ProfileStatus.Active => new SolidColorBrush(Color.FromRgb(67, 233, 123)),
                ProfileStatus.Loading => new SolidColorBrush(Color.FromRgb(245, 166, 35)),
                ProfileStatus.Error => new SolidColorBrush(Color.FromRgb(255, 71, 87)),
                _ => new SolidColorBrush(Color.FromRgb(128, 128, 128))
            };
        }
        return new SolidColorBrush(Colors.Gray);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
