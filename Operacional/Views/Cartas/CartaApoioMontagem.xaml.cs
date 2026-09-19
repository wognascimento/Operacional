using Operacional.Utils;
using System.Windows;
using System.Windows.Controls;

namespace Operacional.Views.Cartas;

public partial class CartaApoioMontagem : UserControl
{
    private CancellationTokenSource? geracao;
    private string? pastaGerada;

    public CartaApoioMontagem() => InitializeComponent();

    private async void OnGerarClick(object sender, RoutedEventArgs e)
    {
        if (geracao != null)
            return;

        using var cancelamento = new CancellationTokenSource();
        geracao = cancelamento;
        pastaGerada = null;
        Gerar.IsEnabled = false;
        AbrirPasta.IsEnabled = false;
        Cancelar.Visibility = Visibility.Visible;
        Resultado.Text = "Consultando dados...";

        try
        {
            var resultado = await CartaApoioMontagemService.GerarAsync(cancelamento.Token);
            if (resultado == null)
            {
                Resultado.Text = "Nenhum registro encontrado.";
                return;
            }

            pastaGerada = System.IO.Path.GetDirectoryName(resultado.Carta);
            Resultado.Text = $"{resultado.Registros} registro(s) exportado(s).\n{pastaGerada}";
            try { SistemaPathResolver.OpenFile(resultado.Carta); }
            catch (Exception ex) { ErrorDialog.Show(ex, "Arquivos gerados, mas nao foi possivel abrir o Word."); }
        }
        catch (OperationCanceledException)
        {
            Resultado.Text = "Geracao cancelada.";
        }
        catch (Exception ex)
        {
            Resultado.Text = "Nao foi possivel gerar a carta.";
            ErrorDialog.Show(ex, "Apoio Montagem");
        }
        finally
        {
            geracao = null;
            Gerar.IsEnabled = true;
            AbrirPasta.IsEnabled = pastaGerada != null;
            Cancelar.Visibility = Visibility.Collapsed;
        }
    }

    private void OnCancelarClick(object sender, RoutedEventArgs e) => geracao?.Cancel();

    private void OnAbrirPastaClick(object sender, RoutedEventArgs e)
    {
        try { if (pastaGerada != null) SistemaPathResolver.OpenInExplorer(pastaGerada); }
        catch (Exception ex) { ErrorDialog.Show(ex, "Apoio Montagem"); }
    }
}
