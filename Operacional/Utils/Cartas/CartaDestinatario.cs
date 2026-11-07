using ClosedXML.Excel;
using CommunityToolkit.Mvvm.ComponentModel;
using MimeKit;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Operacional.Utils.Cartas;

internal sealed class CartaDestinatario : ObservableObject
{
    // Homologacao concluida; usar os destinatarios reais da origem.
    public static readonly bool ModoTeste = false;
    public const string EmailTeste = "helpdesk@cipolatti.com.br";
    public string EmailEnvio => ModoTeste ? EmailTeste : Email;
    private bool selecionado;
    private string status = "pendente", copia = "", detalhe = "";
    public bool Selecionado { get => selecionado; set => SetProperty(ref selecionado, value); }
    public string Status { get => status; set => SetProperty(ref status, value); }
    public string Copia { get => copia; set => SetProperty(ref copia, value); }
    public string Detalhe { get => detalhe; set => SetProperty(ref detalhe, value); }
    public string Sigla { get; init; } = "";
    public string Contato { get; init; } = "";
    public string Email { get; init; } = "";
    public string Chave { get; init; } = "";
    public string Tipo { get; init; } = "";
    public string Ano { get; init; } = "";
    public int Linha { get; init; }
    public string Problema { get; set; } = "";
    public bool Valido => Problema.Length == 0;
    public bool JaEnviado { get; set; }
    public bool RequerConfirmacaoReenvio => JaEnviado || !Pendente(Status);
    public string UltimoEnvio { get; set; } = "";
    public string Usuario { get; set; } = "";
    public Guid? Tentativa { get; set; }
    public string? Pdf { get; set; }
    public string? Html { get; set; }

    public static bool Pendente(string status) => status is "pendente" or "falha";
    public static bool SelecionarPorPadrao(string status, bool jaEnviado, bool valido) =>
        valido && !jaEnviado && Pendente(status);

    public static string NormalizarEmail(string texto)
    {
        if (texto.Contains('\r') || texto.Contains('\n') ||
            !MailboxAddress.TryParse(texto.Trim(), out var email) ||
            !email.Address.Contains('@'))
            throw new InvalidOperationException("Informe um unico endereco de email valido.");
        return email.Address.ToLowerInvariant();
    }

    public static string CriarChave(string tipo, string ano, string sigla, string email) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(
            new[] { tipo, ano, sigla.Trim().ToUpperInvariant(), email.Trim().ToLowerInvariant() }))));

    public static List<CartaDestinatario> Ler(string planilha, string tipo, string ano)
    {
        if (ModoTeste) tipo = "TESTE_" + tipo;
        using var book = new XLWorkbook(planilha);
        var sheet = book.Worksheet(1);
        var headers = sheet.Row(1).CellsUsed().ToDictionary(
            c => c.GetString().Trim(), c => c.Address.ColumnNumber, StringComparer.OrdinalIgnoreCase);
        if (!headers.TryGetValue("sigla", out var siglaCol) &&
            !headers.TryGetValue("sigla_serv", out siglaCol))
            throw new InvalidOperationException("A origem nao possui a coluna sigla.");
        if (!headers.TryGetValue("email", out var emailCol))
            throw new InvalidOperationException("A origem nao possui a coluna email.");
        headers.TryGetValue("contato", out var contatoCol);
        var encontrados = new Dictionary<string, (CartaDestinatario Item, string Conteudo)>();
        var itens = new List<CartaDestinatario>();
        foreach (var row in sheet.RowsUsed().Skip(1))
        {
            var sigla = row.Cell(siglaCol).GetString().Trim().ToUpperInvariant();
            var rawEmail = row.Cell(emailCol).GetString().Trim();
            // Cada caixa postal recebe sua propria carta e tem seu proprio historico.
            var emails = rawEmail.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (emails.Length == 0) emails = new[] { "" };
            foreach (var raw in emails)
            {
                var problema = "";
                var email = raw;
                try { email = NormalizarEmail(raw); }
                catch (InvalidOperationException) { problema = "Email ausente ou invalido."; }
                if (sigla.Length == 0) problema = "Sigla ausente.";
                var chave = CriarChave(tipo, ano, sigla, email);
                var conteudo = JsonSerializer.Serialize(headers.OrderBy(p => p.Key).Select(p =>
                    new[] { p.Key, p.Value == emailCol ? email : row.Cell(p.Value).GetFormattedString(CultureInfo.InvariantCulture) }));
                if (encontrados.TryGetValue(chave, out var anterior))
                {
                    if (anterior.Conteudo != conteudo)
                    {
                        anterior.Item.Problema = "Origem com conteudos diferentes para a mesma sigla/email. Revise a consulta.";
                        anterior.Item.Detalhe = anterior.Item.Problema;
                        anterior.Item.Selecionado = false;
                    }
                    continue;
                }
                var item = new CartaDestinatario
                {
                    Sigla = sigla, Email = email, Tipo = tipo, Ano = ano, Chave = chave,
                    Contato = contatoCol > 0 ? row.Cell(contatoCol).GetString() : "",
                    Linha = row.RowNumber(), Problema = problema, Detalhe = problema,
                    Selecionado = problema.Length == 0
                };
                encontrados.Add(chave, (item, conteudo));
                itens.Add(item);
            }
        }
        return itens;
    }
}
