using System.Globalization;
using System.Windows.Data;

namespace ClashWidget.Converters;

public class SpeedConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not double bytesPerSec)
            return "—";

        return bytesPerSec switch
        {
            < 0 => "—",
            < 1024 => $"{bytesPerSec:F0} B/s",
            < 1024 * 1024 => $"{bytesPerSec / 1024:F1} KB/s",
            < 1024 * 1024 * 1024 => $"{bytesPerSec / (1024 * 1024):F1} MB/s",
            _ => $"{bytesPerSec / (1024 * 1024 * 1024):F2} GB/s"
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
