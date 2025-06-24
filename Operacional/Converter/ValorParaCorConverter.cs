using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Operacional.Converter;

class ValorParaCorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        // Trata null, vazio ou tipos diferentes de int
        if (value == null || string.IsNullOrEmpty(value.ToString()))
            return new SolidColorBrush(Colors.White);

        if (int.TryParse(value.ToString(), out int i))
        {
            if (i == 0)
                return new SolidColorBrush(Colors.LightBlue);
            else if (i > 0)
                return new SolidColorBrush(Colors.DarkBlue);
        }

        // Retorno padrão: branco, se não for um número válido
        return new SolidColorBrush(Colors.White);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class ValorParaFonteCorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        // Se o valor for nulo ou vazio, fonte preta
        if (value == null || string.IsNullOrEmpty(value.ToString()))
            return new SolidColorBrush(Colors.Black);

        if (int.TryParse(value.ToString(), out int i))
        {
            if (i > 0)
                return new SolidColorBrush(Colors.White); // Maior que zero = fonte branca
            else // Zero ou menor
                return new SolidColorBrush(Colors.Black); // Fonte preta
        }

        // Caso não seja reconhecido
        return new SolidColorBrush(Colors.Black);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class ValueToBackgroundConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null || value == DBNull.Value)
            return new SolidColorBrush(Colors.White);

        if (decimal.TryParse(value.ToString(), out decimal numValue))
        {
            if (numValue > 0)
                return new SolidColorBrush(Colors.Blue);
            if (numValue == 0)
                return new SolidColorBrush(Colors.LightBlue);
        }

        return new SolidColorBrush(Colors.Transparent);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class ValueToForegroundConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        // Se for um valor maior que 0, retorna branco (para fundo azul escuro)
        if (value != null && value != DBNull.Value)
        {
            if (decimal.TryParse(value.ToString(), out decimal numValue) && numValue > 0)
            {
                return Brushes.White; // Texto branco para fundo azul escuro
            }
        }

        return Brushes.Black; // Texto preto para todos os outros casos
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}