using MimeKit;
using Operacional.DataBase;
using Operacional.Utils;
using Operacional.Utils.Cartas;
using System.ComponentModel;
using System.IO;
using System.Windows;
using Telerik.Windows.Controls;

namespace Operacional.Views.Cartas;

public partial class EnvioCartasWindow : Window
{
    private readonly string carta;
    private readonly string planilha;
    private readonly string tipo;
    private readonly string ano;
    private readonly CartaEnvioRepository repository;
    private List<CartaDestinatario> itens = [];
    private bool ocupado, parar, historicoCarregado;

    public EnvioCartasWindow(string cartaOriginal, string planilhaOriginal, string tipoCarta)
    {
        InitializeComponent();
        tipo = tipoCarta;
        var settings = DataBaseSettings.Instance;
        ano = settings.Database ?? throw new InvalidOperationException("Ano nao configurado.");
        repository = new CartaEnvioRepository(
            settings.ConnectionString ?? throw new InvalidOperationException("Conexao nao configurada."),
            settings.Username ?? throw new InvalidOperationException("Usuario nao configurado."));
        // Congela a lista e o modelo deste lote para conferir/enviar exatamente os mesmos dados.
        var pasta = Path.Combine(Path.GetDirectoryName(cartaOriginal)!, "Envio_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(pasta);
        carta = Path.Combine(pasta, Path.GetFileName(cartaOriginal));
        planilha = Path.Combine(pasta, Path.GetFileName(planilhaOriginal));
        File.Copy(cartaOriginal, carta);
        File.Copy(planilhaOriginal, planilha);
        Title = "Envio de cartas - " + tipo.Replace('_', ' ');
        if (CartaDestinatario.ModoTeste) Title += " - TESTE: " + CartaDestinatario.EmailTeste;
        Assunto.Text = tipo switch
        {
            "INICIO_MONTAGEM" => $"INICIO MONTAGEM DECORAÇÃO DE NATAL {ano}",
            "TRANSPORTE_MONTAGEM" => $"TRANSPORTE PARA A MONTAGEM DECORAÇÃO DE NATAL {ano}",
            "APOIO_MONTAGEM" => $"EQUIPAMENTO DE APOIO PARA A MONTAGEM DECORAÇÃO DE NATAL {ano}",
            "MANUTENCAO" => $"MANUTENÇÃO DECORAÇÃO DE NATAL {ano}",
            _ => throw new ArgumentException("Tipo de carta desconhecido.")
        };
        Width = Math.Min(Width, SystemParameters.WorkArea.Width);
        Height = Math.Min(Height, SystemParameters.WorkArea.Height);
    }

    private async void OnLoaded(object sender, RoutedEventArgs e) => await ExecutarAsync(async () =>
    {
        itens = await Task.Run(() => CartaDestinatario.Ler(planilha, tipo, ano));
        Destinatarios.ItemsSource = itens;
        await AtualizarAsync();
    });

    private async Task AtualizarAsync()
    {
        historicoCarregado = false;
        foreach (var item in itens) item.Selecionado = false;
        await repository.AtualizarAsync(itens);
        historicoCarregado = true;
        Destinatarios.Rebind();
        Situacao.Text = $"{itens.Count} destinatario(s). {itens.Count(i => i.Selecionado)} pendente(s) selecionado(s).";
    }

    private async Task ExecutarAsync(Func<Task> acao)
    {
        if (ocupado) return;
        ocupado = true;
        parar = false;
        Configuracao.IsEnabled = Comandos.IsEnabled = Destinatarios.IsEnabled = false;
        Parar.IsEnabled = true;
        try { await acao(); }
        catch (Exception ex)
        {
            var mensagem = ex.Message;
            if (Senha.Password.Length > 0) mensagem = mensagem.Replace(Senha.Password, "***");
            Situacao.Text = mensagem;
            MessageBox.Show(this, mensagem, "Envio de cartas", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        finally
        {
            ocupado = false;
            Configuracao.IsEnabled = Comandos.IsEnabled = Destinatarios.IsEnabled = true;
            Parar.IsEnabled = false;
        }
    }

    private CartaDestinatario Atual() => Destinatarios.SelectedItem as CartaDestinatario
        ?? throw new InvalidOperationException("Selecione uma linha para conferir a carta ou o historico.");

    private IEnumerable<CartaDestinatario> Visiveis() => Destinatarios.Items.OfType<CartaDestinatario>();
    private void OnSelecionarPendentes(object sender, RoutedEventArgs e)
    {
        if (!historicoCarregado) return;
        foreach (var item in itens) item.Selecionado = false;
        foreach (var item in Visiveis())
            item.Selecionado = CartaDestinatario.SelecionarPorPadrao(item.Status, item.JaEnviado, item.Valido);
    }
    private void OnDesmarcar(object sender, RoutedEventArgs e)
    {
        foreach (var item in itens) item.Selecionado = false;
    }
    private async void OnAtualizar(object sender, RoutedEventArgs e) => await ExecutarAsync(AtualizarAsync);
    private void OnParar(object sender, RoutedEventArgs e)
    {
        parar = true;
        Situacao.Text = "Parando apos concluir o item atual...";
    }
    private void OnRemetenteLostFocus(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(Resposta.Text)) Resposta.Text = Remetente.Text.Trim();
    }
    private async void OnPdf(object sender, RoutedEventArgs e) => await ConferirAsync(false);
    private async void OnHtml(object sender, RoutedEventArgs e) => await ConferirAsync(true);
    private async Task ConferirAsync(bool html) => await ExecutarAsync(async () =>
    {
        var item = Atual();
        Situacao.Text = $"Gerando carta de {item.Sigla} para conferencia...";
        await CartaDocumento.GerarAsync(item, carta, planilha);
        SistemaPathResolver.OpenFile(html ? item.Html! : item.Pdf!);
        Situacao.Text = $"Carta de {item.Sigla} aberta para conferencia.";
    });
    private async void OnEnviar(object sender, RoutedEventArgs e) => await EnviarAsync();

    private async Task EnviarAsync() => await ExecutarAsync(async () =>
    {
        if (!historicoCarregado) throw new InvalidOperationException("Atualize o historico antes de enviar.");
        Destinatarios.CommitEdit();
        var selecionados = Visiveis().Where(i => i.Selecionado).ToList();
        if (selecionados.Count == 0) throw new InvalidOperationException("Marque os destinatarios que deseja enviar.");
        if (selecionados.Any(i => !i.Valido))
            throw new InvalidOperationException("Ha registros selecionados com problemas na origem. Confira a coluna Detalhes.");
        var reenvios = selecionados.Where(i => i.RequerConfirmacaoReenvio).ToHashSet();
        var remetente = CartaDestinatario.NormalizarEmail(Remetente.Text);
        var resposta = CartaDestinatario.NormalizarEmail(Resposta.Text);
        var assunto = Assunto.Text.Trim();
        if (assunto.Length == 0 || assunto.Contains('\r') || assunto.Contains('\n'))
            throw new InvalidOperationException("Informe o assunto.");
        if (string.IsNullOrWhiteSpace(Senha.Password)) throw new InvalidOperationException("Informe a senha do email.");
        var aviso = reenvios.Count > 0
            ? $"ATENCAO: {reenvios.Count} carta(s) marcada(s) ja enviada(s) ou com resultado incerto. " +
              "Os destinatarios podem receber novamente. Confirma o reenvio?\n\n" +
              string.Join("\n", reenvios.Take(10).Select(i => $"{i.Sigla} - {i.Email}")) +
              (reenvios.Count > 10 ? $"\nE mais {reenvios.Count - 10} destinatario(s) marcado(s)." : "")
            : "Enviar cartas individuais com HTML no corpo e PDF anexado?";
        if (MessageBox.Show(this,
            $"{aviso}\n\n" +
            (CartaDestinatario.ModoTeste
                ? $"MODO TESTE: todas as {selecionados.Count} mensagens serao enviadas somente para {CartaDestinatario.EmailTeste}.\nOs destinatarios reais nao serao marcados como enviados.\n\n"
                : "") +
            $"Destinatarios visiveis selecionados: {selecionados.Count}\nDe: {remetente}\nResponder para: {resposta}\nAssunto: {assunto}",
            reenvios.Count > 0 ? "Confirmar envio e reenvio" : "Confirmar envio",
            MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) != MessageBoxResult.Yes) return;
        var enviados = 0;
        try
        {
            foreach (var item in selecionados)
            {
                if (parar) return;
                Situacao.Text = $"Preparando HTML/PDF: {item.Sigla} - {item.Email}";
                await CartaDocumento.GerarAsync(item, carta, planilha);
            }
            if (parar) return;
            using var session = new CartaEmailSession();
            Situacao.Text = "Conectando ao SMTP e a pasta Enviados...";
            await session.ConectarAsync(remetente, Senha.Password, PastaEnviados.Text);
            foreach (var item in selecionados)
            {
                if (parar) break;
                Situacao.Text = $"Enviando {enviados + 1}/{selecionados.Count}: {item.Sigla} - {item.EmailEnvio}";
                // Qualquer falha interrompe o lote. Nao tentar automaticamente a proxima mensagem.
                await CartaEnvioService.EnviarAsync(item, repository, session, remetente, resposta, assunto, reenvios.Contains(item));
                enviados++;
            }
            await AtualizarAsync();
            Situacao.Text = $"{enviados} mensagem(ns) aceita(s) pelo servidor SMTP." +
                (parar ? " Lote interrompido pelo usuario." : "") +
                (itens.Any(i => i.Copia == "falha") ? " Existem copias pendentes em Enviados." : "");
        }
        finally { Senha.Clear(); }
    });

    private async void OnSalvarCopia(object sender, RoutedEventArgs e) => await ExecutarAsync(async () =>
    {
        var item = Atual();
        if (item.Tentativa is not Guid id || item.Status != "enviado")
            throw new InvalidOperationException("Selecione uma carta com envio confirmado.");
        var remetente = CartaDestinatario.NormalizarEmail(Remetente.Text);
        try
        {
            using var session = new CartaEmailSession();
            await session.ConectarAsync(remetente, Senha.Password, PastaEnviados.Text, somenteCopia: true);
            await repository.RecuperarCopiaAsync(id, async copia =>
            {
                if (!string.Equals(copia.Remetente, remetente, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Entre com a conta remetente usada no envio original.");
                using var stream = new MemoryStream(copia.Mensagem);
                using var mensagem = await MimeMessage.LoadAsync(stream);
                await session.SalvarCopiaAsync(mensagem);
            });
            await AtualizarAsync();
            Situacao.Text = "Copia confirmada em Enviados. Nenhum email foi reenviado.";
        }
        finally { Senha.Clear(); }
    });

    private async void OnHistorico(object sender, RoutedEventArgs e) => await ExecutarAsync(async () =>
    {
        var item = Atual();
        var dados = await repository.HistoricoAsync(item.Chave);
        new Window
        {
            Owner = this, Title = $"Historico - {item.Sigla} - {item.Email} (horarios UTC)",
            Width = Math.Min(1100, SystemParameters.WorkArea.Width),
            Height = Math.Min(500, SystemParameters.WorkArea.Height), WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = new RadGridView
            {
                ItemsSource = dados.DefaultView, AutoGenerateColumns = true, IsReadOnly = true,
                CanUserInsertRows = false, CanUserDeleteRows = false, ShowGroupPanel = false
            }
        }.ShowDialog();
    });
    private void OnClosing(object? sender, CancelEventArgs e)
    {
        if (!ocupado) return;
        e.Cancel = true;
        parar = true;
        Situacao.Text = "Aguarde a conclusao do item atual antes de fechar.";
    }
    private void OnClosed(object? sender, EventArgs e) => Senha.Clear();
}
