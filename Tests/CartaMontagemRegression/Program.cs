using ClosedXML.Excel;
using Operacional.Utils;
using System.Data;
using System.Data.Common;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Xml.Linq;

internal static class Program
{
    private static int Main(string[] args)
    {
        if (args.Length != 2)
        {
            Console.Error.WriteLine("Usage: CartaMontagemRegression <template.docx> <test-output-directory>");
            return 1;
        }
        try
        {
            var template = Path.GetFullPath(args[0]);
            var output = Path.GetFullPath(args[1]);
            Directory.CreateDirectory(output);
            var carta = Path.Combine(output, "MODELO_CARTA_INICIO_MONTAGEM.docx");
            var excel = Path.Combine(output, "MALA_MONTAGEM.xlsx");
            var original = File.ReadAllBytes(template);
            using var dados = new DataTable();
            dados.Columns.Add("nome", typeof(string));
            dados.Columns.Add("mindedata_de_expedicao", typeof(DateOnly));
            dados.Columns.Add("volume_total_estimado_carga", typeof(decimal));
            dados.Columns.Add("obs", typeof(string));
            dados.Columns.Add("codigo", typeof(long));
            dados.Rows.Add("Cliente de teste", new DateOnly(2026, 9, 17), 0m, DBNull.Value, 12345678901234567L);
            dados.Rows.Add("=1+1", new DateOnly(2026, 9, 18), 32.5m, "Observacao", 123L);
            MalaDiretaMontagem.ExportarExcel(dados, excel);
            using var font = File.OpenRead(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "arial.ttf"));
            var options = new ClosedXML.Excel.LoadOptions
            { GraphicEngine = ClosedXML.Graphics.DefaultGraphicEngine.CreateOnlyWithFonts(font) };
            using (var workbook = new XLWorkbook(excel, options))
            {
                var sheet = workbook.Worksheet(MalaDiretaMontagem.NomePlanilha);
                Check(sheet.Cell(1, 2).GetString() == "mindedata_de_expedicao", "Column names changed.");
                Check(sheet.Cell(2, 2).DataType == XLDataType.DateTime, "Date exported as text.");
                Check(sheet.Cell(2, 2).GetDateTime() == new DateTime(2026, 9, 17), "Incorrect date value.");
                Check(sheet.Cell(2, 3).DataType == XLDataType.Number && sheet.Cell(2, 3).GetDouble() == 0, "Zero was lost.");
                Check(sheet.Cell(2, 4).IsEmpty(), "NULL was not exported as blank.");
                Check(sheet.Cell(2, 5).GetString() == "12345678901234567", "Large identifier lost precision.");
                Check(!sheet.Cell(3, 1).HasFormula && sheet.Cell(3, 1).GetString() == "=1+1", "Text became an Excel formula.");
            }
            File.Copy(template, carta, overwrite: true);
            MalaDiretaMontagem.VincularPlanilha(carta, excel);
            Check(original.SequenceEqual(File.ReadAllBytes(template)), "Original template was modified.");
            using var before = ZipFile.OpenRead(template);
            using var after = ZipFile.OpenRead(carta);
            Check(before.Entries.Count == after.Entries.Count, "Document parts changed.");
            foreach (var entry in before.Entries)
            {
                if (entry.FullName is "word/settings.xml" or "word/_rels/settings.xml.rels") continue;
                using var a = entry.Open();
                using var b = after.GetEntry(entry.FullName)!.Open();
                Check(SHA256.HashData(a).SequenceEqual(SHA256.HashData(b)), $"Document formatting/content changed: {entry.FullName}");
            }
            XNamespace w = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
            using var settingsStream = after.GetEntry("word/settings.xml")!.Open();
            var settings = XDocument.Load(settingsStream);
            var connection = new DbConnectionStringBuilder
            {
                ConnectionString = (string)settings.Descendants(w + "connectString").Single().Attribute(w + "val")!
            };
            Check((string)connection["Data Source"] == excel, "Connection does not point to the adjacent workbook.");
            Check((string?)settings.Descendants(w + "query").Single().Attribute(w + "val") ==
                "SELECT * FROM [qrybasemalamontagem$]", "Wrong worksheet in merge query.");
            using var relsStream = after.GetEntry("word/_rels/settings.xml.rels")!.Open();
            var rels = XDocument.Load(relsStream);
            foreach (var rel in rels.Root!.Elements())
                Check((string?)rel.Attribute("Target") == new Uri(excel).AbsoluteUri, "Stale mail merge source.");
            Console.WriteLine("PASS: Excel types, dates, zero, NULL, identifiers and literal text.");
            Console.WriteLine("PASS: mail merge source and query; all content/layout parts unchanged; original template untouched.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            return 1;
        }
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
