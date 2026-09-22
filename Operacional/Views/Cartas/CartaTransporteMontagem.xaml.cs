using Operacional.Utils;
using System.Windows;
using System.Windows.Controls;

namespace Operacional.Views.Cartas;

public partial class CartaTransporteMontagem : UserControl
{
    private CancellationTokenSource? geracao;
    private string? pastaGerada;

    public CartaTransporteMontagem() => InitializeComponent();

    private async void OnGerarClick(object sender, RoutedEventArgs e)
    {
        if (geracao != null)
            return;

        if (DataInicial.SelectedDate is not DateTime inicio || DataFinal.SelectedDate is not DateTime fim)
        {
            MessageBox.Show(
                "Informe a data inicial e a data final.",
                "Transporte Montagem",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        if (inicio.Date > fim.Date || fim.Date == DateTime.MaxValue.Date)
        {
            MessageBox.Show(
                "Informe um periodo valido, com a data final igual ou posterior a inicial.",
                "Transporte Montagem",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        using var cancelamento = new CancellationTokenSource();
        geracao = cancelamento;
        pastaGerada = null;
        Gerar.IsEnabled = CamposPeriodo.IsEnabled = false;
        AbrirPasta.IsEnabled = false;
        Cancelar.Visibility = Visibility.Visible;
        Resultado.Text = "Consultando dados...";

        try
        {
            var resultado = await CartaTransporteMontagemService.GerarAsync(inicio, fim, cancelamento.Token);
            if (resultado == null)
            {
                Resultado.Text = "Nenhum registro encontrado no periodo informado.";
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
            ErrorDialog.Show(ex, "Transporte Montagem");
        }
        finally
        {
            geracao = null;
            Gerar.IsEnabled = CamposPeriodo.IsEnabled = true;
            AbrirPasta.IsEnabled = pastaGerada != null;
            Cancelar.Visibility = Visibility.Collapsed;
        }
    }

    private void OnCancelarClick(object sender, RoutedEventArgs e) => geracao?.Cancel();

    private void OnAbrirPastaClick(object sender, RoutedEventArgs e)
    {
        try { if (pastaGerada != null) SistemaPathResolver.OpenInExplorer(pastaGerada); }
        catch (Exception ex) { ErrorDialog.Show(ex, "Transporte Montagem"); }
    }
}
