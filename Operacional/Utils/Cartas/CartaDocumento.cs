using ClosedXML.Excel;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Xml.Linq;

namespace Operacional.Utils.Cartas;

internal static class CartaDocumento
{
    private static readonly SemaphoreSlim WordGate = new(1, 1);

    public static async Task GerarAsync(CartaDestinatario item, string modelo, string planilha)
    {
        if (item.Pdf != null && item.Html != null && File.Exists(item.Pdf) && File.Exists(item.Html))
            return;
        await WordGate.WaitAsync();
        try
        {
            var completion = new TaskCompletionSource<(string Pdf, string Html)>(TaskCreationOptions.RunContinuationsAsynchronously);
            var thread = new Thread(() =>
            {
                try { completion.SetResult(Gerar(item, modelo, planilha)); }
                catch (Exception ex) { completion.SetException(ex); }
            }) { IsBackground = true, Name = "Carta Word" };
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            (item.Pdf, item.Html) = await completion.Task;
        }
        finally { WordGate.Release(); }
    }

    private static (string Pdf, string Html) Gerar(CartaDestinatario item, string modelo, string planilha)
    {
        var pasta = Path.Combine(Path.GetDirectoryName(modelo)!, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(pasta);
        var excel = Path.Combine(pasta, "destinatario.xlsx");
        string nomePlanilha;
        using (var origem = new XLWorkbook(planilha))
        using (var destino = new XLWorkbook())
        {
            var source = origem.Worksheet(1);
            nomePlanilha = source.Name;
            var target = destino.AddWorksheet(nomePlanilha);
            var colunas = source.LastColumnUsed()!.ColumnNumber();
            source.Range(1, 1, 1, colunas).CopyTo(target.Cell(1, 1));
            source.Range(item.Linha, 1, item.Linha, colunas).CopyTo(target.Cell(2, 1));
            var emailCol = target.Row(1).CellsUsed().Single(c =>
                c.GetString().Equals("email", StringComparison.OrdinalIgnoreCase)).Address.ColumnNumber;
            target.Cell(2, emailCol).Value = item.Email;
            destino.SaveAs(excel);
        }
        var carta = Path.Combine(pasta, "carta.docx");
        File.Copy(modelo, carta);
        // Abre sem fontes externas antigas/prompts SQL; vincula somente a fonte individual abaixo.
        using (var zip = ZipFile.Open(carta, ZipArchiveMode.Update))
        {
            var entry = zip.GetEntry("word/settings.xml")
                ?? throw new InvalidDataException("Modelo sem configuracoes Word.");
            XDocument xml;
            using (var input = entry.Open()) xml = XDocument.Load(input);
            XNamespace w = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
            xml.Root?.Element(w + "mailMerge")?.Remove();
            using var output = entry.Open();
            output.SetLength(0);
            xml.Save(output);
        }
        var pdf = Path.Combine(pasta, "carta.pdf");
        var html = Path.Combine(pasta, "carta.html");
        dynamic? app = null, documentos = null, doc = null, merge = null, data = null,
            fields = null, emailField = null, final = null, web = null;
        try
        {
            var tipo = Type.GetTypeFromProgID("Word.Application")
                ?? throw new InvalidOperationException("Microsoft Word nao instalado. Contate o suporte.");
            app = Activator.CreateInstance(tipo)!;
            app.Visible = false;
            app.DisplayAlerts = 0;
            app.AutomationSecurity = 3; // Nunca executar macros dos documentos.
            documentos = app.Documents;
            doc = documentos.Open(FileName: carta, ReadOnly: true, AddToRecentFiles: false, Visible: false);
            merge = doc.MailMerge;
            merge.MainDocumentType = 0; // wdFormLetters.
            // Apenas uma linha na fonte; nao depender da ordem/selecoes da mala original.
            merge.OpenDataSource(Name: excel, ConfirmConversions: false, ReadOnly: true,
                LinkToSource: true, AddToRecentFiles: false,
                SQLStatement: $"SELECT * FROM [{nomePlanilha}$]");
            data = merge.DataSource;
            data.ActiveRecord = 1;
            fields = data.DataFields;
            emailField = fields.Item("email");
            if (!string.Equals(((string)emailField.Value).Trim(), item.Email, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("O destinatario da mala direta difere do destinatario selecionado.");
            data.FirstRecord = 1;
            data.LastRecord = 1;
            merge.Destination = 0; // wdSendToNewDocument (nunca email direto pelo Word).
            var quantidadeAntes = (int)documentos.Count;
            merge.Execute(Pause: false);
            if ((int)documentos.Count != quantidadeAntes + 1)
                throw new InvalidOperationException(
                    "O Word encontrou erros na mala direta. Confira os campos, datas e valores numericos da origem antes de enviar.");
            final = app.ActiveDocument;
            final.ExportAsFixedFormat(OutputFileName: pdf, ExportFormat: 17, OpenAfterExport: false);
            web = final.WebOptions;
            web.Encoding = 65001;
            final.SaveAs2(FileName: html, FileFormat: 10, AddToRecentFiles: false);
            if (!File.Exists(pdf) || !File.Exists(html))
                throw new InvalidOperationException("O Word nao gerou PDF e HTML da carta.");
            return (pdf, html);
        }
        finally
        {
            try { if (final != null) final.Close(SaveChanges: 0); } catch { }
            try { if (doc != null) doc.Close(SaveChanges: 0); } catch { }
            try { if (app != null) app.Quit(SaveChanges: 0); } catch { }
            foreach (object? com in new object?[] { web, final, emailField, fields, data, merge, doc, documentos, app })
                if (com != null && Marshal.IsComObject(com)) Marshal.FinalReleaseComObject(com);
        }
    }
}
