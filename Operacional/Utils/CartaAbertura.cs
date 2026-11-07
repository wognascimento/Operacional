using Operacional.Views.Cartas;
using System.Windows;

namespace Operacional.Utils;

internal static class CartaAbertura
{
    public static void Abrir(string carta, string planilha, string tipo)
    {
        var window = new EnvioCartasWindow(carta, planilha, tipo)
        {
            Owner = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive)
        };
        window.ShowDialog();
    }
}
