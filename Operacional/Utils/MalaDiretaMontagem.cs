using ClosedXML.Excel;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Xml.Linq;

namespace Operacional.Utils;

internal static class MalaDiretaMontagem
{
    internal const string NomePlanilha = "qrybasemalamontagem";

    public static void ExportarExcel(
        DataTable dados,
        string caminho,
        CancellationToken cancellationToken = default,
        string nomePlanilha = NomePlanilha)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add(nomePlanilha);
        for (int c = 0; c < dados.Columns.Count; c++)
            sheet.Cell(1, c + 1).Value = dados.Columns[c].ColumnName;
        for (int r = 0; r < dados.Rows.Count; r++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            for (int c = 0; c < dados.Columns.Count; c++)
            {
                var value = dados.Rows[r][c];
                var cell = sheet.Cell(r + 2, c + 1);
                switch (value)
                {
                    case DBNull: break;
                    case DateOnly date:
                        cell.Value = date.ToDateTime(TimeOnly.MinValue);
                        cell.Style.DateFormat.Format = "dd/MM/yyyy";
                        break;
                    case DateTime date:
                        cell.Value = date.Kind == DateTimeKind.Utc ? date.ToLocalTime() : date;
                        cell.Style.DateFormat.Format = "dd/MM/yyyy";
                        break;
                    case DateTimeOffset date:
                        cell.Value = date.LocalDateTime;
                        cell.Style.DateFormat.Format = "dd/MM/yyyy";
                        break;
                    case TimeOnly time:
                        cell.Value = time.ToTimeSpan();
                        cell.Style.NumberFormat.Format = "hh:mm";
                        break;
                    case long number when number > 999999999999999L || number < -999999999999999L:
                        cell.Value = number.ToString(CultureInfo.InvariantCulture);
                        break;
                    default:
                        cell.Value = XLCellValue.FromObject(value, CultureInfo.InvariantCulture);
                        break;
                }
            }
        }
        sheet.SheetView.FreezeRows(1);
        sheet.Columns(1, dados.Columns.Count).Width = 22;
        workbook.SaveAs(caminho);
    }

    public static void VincularPlanilha(string carta, string planilha, string nomePlanilha = NomePlanilha)
    {
        var caminho = Path.GetFullPath(planilha);
        if (!File.Exists(caminho)) throw new FileNotFoundException("Planilha da mala direta nao encontrada.", caminho);
        using var zip = ZipFile.Open(carta, ZipArchiveMode.Update);
        var settingsEntry = zip.GetEntry("word/settings.xml")
            ?? throw new InvalidDataException("O modelo nao possui configuracoes de mala direta.");
        var relsEntry = zip.GetEntry("word/_rels/settings.xml.rels")
            ?? throw new InvalidDataException("O modelo nao possui vinculos de mala direta.");
        var settings = LerXml(settingsEntry);
        var rels = LerXml(relsEntry);
        XNamespace w = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
        XNamespace r = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
        XNamespace package = "http://schemas.openxmlformats.org/package/2006/relationships";
        var merge = settings.Root?.Element(w + "mailMerge")
            ?? throw new InvalidDataException("O modelo nao esta configurado como mala direta.");
        var odso = merge.Element(w + "odso")
            ?? throw new InvalidDataException("Fonte de dados da mala direta nao configurada.");
        var connection = new DbConnectionStringBuilder
        {
            ["Provider"] = "Microsoft.ACE.OLEDB.12.0",
            ["Data Source"] = caminho,
            ["Mode"] = "Read",
            ["Extended Properties"] = "Excel 12.0 Xml;HDR=YES;IMEX=1"
        }.ConnectionString;
        SetValue(merge, w + "connectString", connection, w);
        SetValue(merge, w + "query", $"SELECT * FROM [{nomePlanilha}$]", w);
        SetValue(odso, w + "udl", connection, w);
        SetValue(odso, w + "table", nomePlanilha + "$", w);
        var ids = new[] { merge.Element(w + "dataSource"), odso.Element(w + "src") }
            .Select(e => (string?)e?.Attribute(r + "id")).ToArray();
        foreach (var id in ids)
        {
            var relationship = rels.Root?.Elements(package + "Relationship")
                .SingleOrDefault(e => (string?)e.Attribute("Id") == id);
            if (string.IsNullOrWhiteSpace(id) || relationship == null)
                throw new InvalidDataException("Vinculo da fonte de dados ausente no modelo.");
            relationship.SetAttributeValue("Target", new Uri(caminho).AbsoluteUri);
            relationship.SetAttributeValue("TargetMode", "External");
        }
        GravarXml(settingsEntry, settings);
        GravarXml(relsEntry, rels);
    }

    public static void ConfigurarAssuntoEmail(string carta, string assunto)
    {
        using var zip = ZipFile.Open(carta, ZipArchiveMode.Update);
        var settingsEntry = zip.GetEntry("word/settings.xml")
            ?? throw new InvalidDataException("O modelo nao possui configuracoes de mala direta.");
        var settings = LerXml(settingsEntry);
        XNamespace w = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
        var merge = settings.Root?.Element(w + "mailMerge")
            ?? throw new InvalidDataException("O modelo nao esta configurado como mala direta.");

        SetOrAddValue(merge, w + "destination", "email", w);
        SetOrAddValue(merge, w + "mailSubject", assunto, w);
        GravarXml(settingsEntry, settings);
    }

    private static void SetValue(XElement parent, XName name, string value, XNamespace w)
    {
        var element = parent.Element(name)
            ?? throw new InvalidDataException($"Configuracao de mala direta ausente: {name.LocalName}.");
        element.SetAttributeValue(w + "val", value);
    }

    private static void SetOrAddValue(XElement parent, XName name, string value, XNamespace w)
    {
        var element = parent.Element(name);
        if (element == null)
        {
            element = new XElement(name);
            var odso = parent.Element(w + "odso");
            if (odso != null)
                odso.AddBeforeSelf(element);
            else
                parent.Add(element);
        }

        element.SetAttributeValue(w + "val", value);
    }

    private static XDocument LerXml(ZipArchiveEntry entry)
    {
        using var stream = entry.Open();
        return XDocument.Load(stream, System.Xml.Linq.LoadOptions.PreserveWhitespace);
    }

    private static void GravarXml(ZipArchiveEntry entry, XDocument xml)
    {
        using var stream = entry.Open();
        stream.SetLength(0);
        xml.Save(stream, System.Xml.Linq.SaveOptions.DisableFormatting);
    }
}
