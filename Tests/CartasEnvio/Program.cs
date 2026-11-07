using ClosedXML.Excel;
using MimeKit;
using Operacional.Utils.Cartas;
using System.IO.Compression;
using System.Text.RegularExpressions;
using System.Xml.Linq;

var pasta = Path.Combine(Path.GetTempPath(), "SIG-Cartas-Test-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(pasta);
var planilha = Path.Combine(pasta, "dados.xlsx");
using (var book = new XLWorkbook())
{
    var sheet = book.AddWorksheet("mala");
    string[][] linhas =
    [
        ["sigla", "contato", "email", "nome"],
        ["ABC", "Contato", "cliente@example.com", "Cliente"],
        ["ABC", "Contato", "cliente@example.com", "Cliente"],
        ["DEF", "Outro", "", "Sem email"],
        ["GHI", "Pessoa", "a@example.com; b@example.com", "Multiplos"],
        ["JKL", "Contato", "igual@example.com", "Versao 1"],
        ["JKL", "Contato", "igual@example.com", "Versao 2"],
        ["MNO", "Contato", "nao e email", "Invalido"]
    ];
    for (var r = 0; r < linhas.Length; r++)
        for (var c = 0; c < linhas[r].Length; c++)
            sheet.Cell(r + 1, c + 1).Value = linhas[r][c];
    book.SaveAs(planilha);
}
var itens = CartaDestinatario.Ler(planilha, "INICIO_MONTAGEM", "2026");
Check(itens.Count == 6, "Deduplicacao e separacao por destinatario");
Check(itens.Count(i => i.Selecionado) == 3, "Somente validos selecionados");
Check(!itens.Single(i => i.Sigla == "JKL").Valido, "Conteudos conflitantes bloqueados");
Check(itens.Single(i => i.Sigla == "ABC").Linha == 2, "Linha original preservada");
Check(CartaDestinatario.CriarChave("A", "2026", " abc ", "A@EXAMPLE.COM") ==
    CartaDestinatario.CriarChave("A", "2026", "ABC", "a@example.com"), "Chave normalizada");
Check(CartaDestinatario.CriarChave("A", "2026", "ABC", "a@example.com") !=
    CartaDestinatario.CriarChave("B", "2026", "ABC", "a@example.com"), "Tipos independentes");
Check(CartaDestinatario.CriarChave("A", "2026", "ABC", "a@example.com") !=
    CartaDestinatario.CriarChave("A", "2027", "ABC", "a@example.com"), "Anos independentes");
foreach (var status in new[] { "enviado", "enviando", "incerto" })
    Check(!CartaDestinatario.SelecionarPorPadrao(status, false, true), "Nao selecionar " + status);
Check(!CartaDestinatario.SelecionarPorPadrao("falha", true, true), "Reenvio falho nao apaga sucesso anterior");
Check(!CartaDestinatario.SelecionarPorPadrao("pendente", false, false), "Invalido nao selecionado");
Check(CartaDestinatario.SelecionarPorPadrao("falha", false, true), "Rejeicao sem envio anterior pode repetir");
var novo = new CartaDestinatario { Status = "pendente" };
var repetido = new CartaDestinatario { Status = "enviado", JaEnviado = true };
var incerto = new CartaDestinatario { Status = "incerto" };
var falhaReenvio = new CartaDestinatario { Status = "falha", JaEnviado = true };
Check(!novo.RequerConfirmacaoReenvio && repetido.RequerConfirmacaoReenvio &&
    incerto.RequerConfirmacaoReenvio && falhaReenvio.RequerConfirmacaoReenvio,
    "Botao unico distingue envios novos de reenvios");
var lote = new[] { novo, repetido, incerto };
var reenvios = lote.Where(i => i.RequerConfirmacaoReenvio).ToHashSet();
Check(!reenvios.Contains(novo) && reenvios.Count == 2, "Lote misto autoriza reenvio somente dos confirmados");
Throws(() => CartaDestinatario.NormalizarEmail("a@example.com\r\nBcc: b@example.com"), "Rejeita injecao de cabecalho");

var item = itens.Single(i => i.Sigla == "ABC");
item.Html = Path.Combine(pasta, "carta.html");
item.Pdf = Path.Combine(pasta, "carta.pdf");
File.WriteAllText(item.Pdf, "%PDF-1.4 teste");
File.WriteAllBytes(Path.Combine(pasta, "logo.png"), Convert.FromBase64String(
    "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+jRZkAAAAASUVORK5CYII="));
File.WriteAllText(item.Html, """
    <html><head><link href="file:///confidencial.xml"/></head>
    <body><script>alert(1)</script><p>Cliente ABC</p><img src="logo.png" onerror="alert(1)"/></body></html>
    """);
using (var message = CartaMensagem.Criar(item, "usuario@example.com", "resposta@example.com", "Assunto teste"))
{
    Check(!CartaDestinatario.ModoTeste &&
        message.To.Mailboxes.Single().Address == item.Email &&
        message.Cc.Count == 0 && message.Bcc.Count == 0, "Envio para o destinatario do grid, sem copias");
    Check(message.Subject == "Assunto teste", "Assunto sem prefixo de homologacao");
    Check(item.Tipo == "INICIO_MONTAGEM" &&
        item.Chave != CartaDestinatario.CriarChave("TESTE_INICIO_MONTAGEM", item.Ano, item.Sigla, item.Email),
        "Envio real nao reutiliza historico de teste");
    Check(message.ReplyTo.Mailboxes.Single().Address == "resposta@example.com", "Reply-To independente");
    Check(message.Attachments.Single().ContentDisposition?.FileName == "carta.pdf", "PDF anexado");
    Check(message.HtmlBody != null && message.HtmlBody.Contains("cid:") && !message.HtmlBody.Contains("onerror") &&
        !message.HtmlBody.Contains("<script") && !message.HtmlBody.Contains("<link"), "HTML e imagem incorporada");
    Check(message.BodyParts.OfType<MimePart>().Any(p => p.ContentId != null), "Recurso MIME presente");
    using var buffer = new MemoryStream();
    message.WriteTo(buffer);
    buffer.Position = 0;
    using var restored = MimeMessage.Load(buffer);
    Check(restored.MessageId == message.MessageId && restored.Attachments.Count() == 1,
        "MIME persistido preserva ID e PDF para copia IMAP");
}
Throws(() => CartaMensagem.Criar(item, "usuario@example.com", "resposta@example.com", "A\r\nBcc: a@example.com"),
    "Assunto sem injecao");
File.WriteAllText(item.Html, "<html><body><img src='https://externo.example.com/logo.png'></body></html>");
Throws(() => CartaMensagem.Criar(item, "usuario@example.com", "resposta@example.com", "A"),
    "Nao carregar imagem remota");
var fora = Path.Combine(pasta, "sub");
Directory.CreateDirectory(fora);
item.Html = Path.Combine(fora, "carta.html");
File.WriteAllText(item.Html, "<html><body><img src='../logo.png'></body></html>");
Throws(() => CartaMensagem.Criar(item, "usuario@example.com", "resposta@example.com", "A"),
    "Imagem fora da pasta bloqueada");
Console.WriteLine("Todos os testes passaram. Nenhum banco, SMTP ou IMAP acessado.");
Console.WriteLine("Artefatos sinteticos: " + pasta);

if (args.Length == 2 && args[0] == "--word")
{
    foreach (var nome in new[] { "MODELO_CARTA_INICIO_MONTAGEM.docx", "MODELO_CARTA_TRANSPORTE_MONTAGEM.docx",
        "MODELO_CARTA_APOIO_MONTAGEM.docx", "MODELO_MANUTENÇÃO.docx" })
    {
        var modelo = Path.GetFullPath(Path.Combine(args[1], nome));
        var campos = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "sigla", "email", "contato" };
        using (var zip = ZipFile.OpenRead(modelo))
        {
            XNamespace w = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
            foreach (var part in zip.Entries.Where(e => e.FullName.StartsWith("word/") && e.FullName.EndsWith(".xml")))
            {
                using var input = part.Open();
                var xml = XDocument.Load(input);
                var instrucoes = string.Concat(xml.Descendants(w + "instrText").Select(e => e.Value)) + " " +
                    string.Join(" ", xml.Descendants(w + "fldSimple").Select(e => (string?)e.Attribute(w + "instr")));
                foreach (Match match in Regex.Matches(instrucoes, "MERGEFIELD\\s+(?:\"([^\"]+)\"|([^\\s\\\\]+))"))
                    campos.Add(match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value);
            }
        }
        var testeDir = Path.Combine(pasta, Path.GetFileNameWithoutExtension(nome));
        Directory.CreateDirectory(testeDir);
        var template = Path.Combine(testeDir, nome);
        File.Copy(modelo, template);
        var dados = Path.Combine(testeDir, "teste.xlsx");
        using (var book = new XLWorkbook())
        {
            var sheet = book.AddWorksheet("mala");
            int c = 0;
            foreach (var campo in campos)
            {
                c++;
                sheet.Cell(1, c).Value = campo;
                if (campo.Contains("data", StringComparison.OrdinalIgnoreCase))
                {
                    sheet.Cell(2, c).Value = new DateTime(2026, 10, 30);
                    sheet.Cell(2, c).Style.DateFormat.Format = "dd/MM/yyyy";
                }
                else if (campo.Contains("volume") || campo.Contains("m3"))
                    sheet.Cell(2, c).Value = 10;
                else
                    sheet.Cell(2, c).Value = campo == "email" ? "teste@example.com" :
                        campo == "sigla" ? "SIG_TESTE" : "TESTE " + campo;
            }
            book.SaveAs(dados);
        }
        var destinatario = CartaDestinatario.Ler(dados, "TESTE", "2026").Single();
        Console.WriteLine("WORD: " + nome);
        await CartaDocumento.GerarAsync(destinatario, template, dados);
        Check(File.ReadAllBytes(destinatario.Pdf!).Length > 1000, "PDF gerado " + nome);
        using var mime = CartaMensagem.Criar(destinatario, "origem@example.com", "resposta@example.com", "Teste sem envio");
        Check(mime.HtmlBody?.Contains("TESTE") == true, "Mala direta preenchida " + nome);
        Check(mime.HtmlBody?.Contains("MERGEFIELD") != true, "HTML sem campos nao mesclados");
        Check(!mime.HtmlBody!.Contains("src=\"carta_arquivos/") &&
            mime.BodyParts.OfType<MimePart>().Any(p => p.ContentId != null), "Imagens do modelo incorporadas");
        Console.WriteLine("PDF: " + destinatario.Pdf);
        if (nome == "MODELO_CARTA_INICIO_MONTAGEM.docx")
        {
            using (var erro = new XLWorkbook(dados))
            {
                var folha = erro.Worksheet(1);
                foreach (var header in folha.Row(1).CellsUsed().Where(c =>
                    c.GetString().Contains("volume") || c.GetString().Contains("m3")))
                    folha.Cell(2, header.Address.ColumnNumber).Value = "NAO NUMERICO";
                erro.Save();
            }
            var invalido = CartaDestinatario.Ler(dados, "TESTE", "2026").Single();
            var bloqueou = false;
            try { await CartaDocumento.GerarAsync(invalido, template, dados); }
            catch (InvalidOperationException) { bloqueou = true; }
            Check(bloqueou, "Nao enviar documento de erros de mala direta");
        }
    }
}

static void Check(bool value, string nome)
{
    if (!value) throw new Exception("FALHOU: " + nome);
    Console.WriteLine("OK: " + nome);
}
static void Throws(Action action, string nome)
{
    try { action(); }
    catch (InvalidOperationException) { Console.WriteLine("OK: " + nome); return; }
    throw new Exception("FALHOU: " + nome);
}
