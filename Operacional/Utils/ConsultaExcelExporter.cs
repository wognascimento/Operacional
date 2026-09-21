using ClosedXML.Excel;
using System.Data;
using System.Globalization;
using System.Reflection;

namespace Operacional.Utils;

internal static class ConsultaExcelExporter
{
    public static void Exportar<T>(IEnumerable<T> dados, string caminho, string nomePlanilha)
    {
        var propriedades = typeof(T)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(property => property.CanRead && property.GetIndexParameters().Length == 0)
            .ToArray();

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add(nomePlanilha);

        for (var column = 0; column < propriedades.Length; column++)
            worksheet.Cell(1, column + 1).Value = propriedades[column].Name;

        var row = 2;
        foreach (var item in dados)
        {
            for (var column = 0; column < propriedades.Length; column++)
                DefinirValor(worksheet.Cell(row, column + 1), propriedades[column].GetValue(item));
            row++;
        }

        workbook.SaveAs(caminho);
    }

    public static void Exportar(DataTable dados, string caminho, string nomePlanilha)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add(nomePlanilha);

        for (var column = 0; column < dados.Columns.Count; column++)
            worksheet.Cell(1, column + 1).Value = dados.Columns[column].ColumnName;

        for (var row = 0; row < dados.Rows.Count; row++)
        {
            for (var column = 0; column < dados.Columns.Count; column++)
                DefinirValor(worksheet.Cell(row + 2, column + 1), dados.Rows[row][column]);
        }

        workbook.SaveAs(caminho);
    }

    private static void DefinirValor(IXLCell cell, object? value)
    {
        switch (value)
        {
            case null:
            case DBNull:
                cell.Clear();
                break;
            case DateOnly date:
                cell.Value = date.ToDateTime(TimeOnly.MinValue);
                break;
            case DateTimeOffset date:
                cell.Value = date.LocalDateTime;
                break;
            case TimeOnly time:
                cell.Value = time.ToTimeSpan();
                break;
            case Enum enumValue:
                cell.Value = enumValue.ToString();
                break;
            default:
                cell.Value = XLCellValue.FromObject(value, CultureInfo.CurrentCulture);
                break;
        }
    }
}
