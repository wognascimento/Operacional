using System.Globalization;
using System.Windows.Data;

namespace Operacional.Converter;

public class DiaSemanaConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is DateTime date)
        {
            return date.ToString("dddd", new CultureInfo("pt-BR")); // Exibir o nome do dia em português
        }
        return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
