using Npgsql;
using System;
using System.Data.Common;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Windows;

namespace Operacional;

public static class ErrorDialog
{
    public static void Show(Exception exception, string context = "Erro", MessageBoxImage icon = MessageBoxImage.Error)
    {
        MessageBox.Show(BuildMessage(exception, context), context, MessageBoxButton.OK, icon);
    }

    public static string BuildMessage(Exception exception, string context = "Erro")
    {
        var builder = new StringBuilder();
        builder.AppendLine(context);
        builder.AppendLine();

        AppendException(builder, exception, 0);

        if (!string.IsNullOrWhiteSpace(exception.StackTrace))
        {
            builder.AppendLine();
            builder.AppendLine("Stack:");
            builder.AppendLine(exception.StackTrace);
        }

        return builder.ToString();
    }

    private static void AppendException(StringBuilder builder, Exception exception, int level)
    {
        var prefix = level == 0 ? string.Empty : $"Inner {level}: ";
        builder.AppendLine($"{prefix}{exception.GetType().FullName}");

        switch (exception)
        {
            case PostgresException pg:
                AppendPostgres(builder, pg);
                break;
            case NpgsqlException npgsql:
                AppendLine(builder, "Mensagem", npgsql.Message);
                AppendLine(builder, "SqlState", npgsql.SqlState);
                AppendLine(builder, "Fonte", npgsql.Source);
                break;
            case DbException db:
                AppendLine(builder, "Mensagem", db.Message);
                AppendLine(builder, "Codigo", db.ErrorCode.ToString());
                AppendLine(builder, "Fonte", db.Source);
                break;
            case ReflectionTypeLoadException reflection:
                AppendLine(builder, "Mensagem", reflection.Message);
                foreach (var loaderException in reflection.LoaderExceptions)
                {
                    if (loaderException != null)
                        AppendException(builder, loaderException, level + 1);
                }
                break;
            default:
                AppendLine(builder, "Mensagem", exception.Message);
                AppendLine(builder, "Fonte", exception.Source);
                AppendLine(builder, "Metodo", exception.TargetSite?.ToString());
                AppendEntityFrameworkDetails(builder, exception);
                break;
        }

        if (exception.InnerException != null)
        {
            builder.AppendLine();
            AppendException(builder, exception.InnerException, level + 1);
        }
    }

    private static void AppendPostgres(StringBuilder builder, PostgresException pg)
    {
        AppendLine(builder, "Mensagem", pg.MessageText);
        AppendLine(builder, "Detalhe", pg.Detail);
        AppendLine(builder, "Dica", pg.Hint);
        AppendLine(builder, "SqlState", pg.SqlState);
        AppendLine(builder, "Severidade", pg.Severity);
        AppendLine(builder, "Tabela", pg.TableName);
        AppendLine(builder, "Coluna", pg.ColumnName);
        AppendLine(builder, "Constraint", pg.ConstraintName);
        AppendLine(builder, "Schema", pg.SchemaName);
        AppendLine(builder, "Where", pg.Where);
        AppendLine(builder, "Posicao", pg.Position.ToString());
    }

    private static void AppendEntityFrameworkDetails(StringBuilder builder, Exception exception)
    {
        if (!exception.GetType().Name.Contains("DbUpdateException", StringComparison.OrdinalIgnoreCase))
            return;

        var entries = exception.GetType().GetProperty("Entries")?.GetValue(exception) as System.Collections.IEnumerable;
        if (entries == null)
            return;

        builder.AppendLine("Entidades:");
        foreach (var entry in entries)
        {
            var entity = entry?.GetType().GetProperty("Entity")?.GetValue(entry);
            var state = entry?.GetType().GetProperty("State")?.GetValue(entry);
            builder.AppendLine($"- {entity?.GetType().FullName ?? "desconhecida"} ({state})");
        }
    }

    private static void AppendLine(StringBuilder builder, string label, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
            builder.AppendLine($"{label}: {value}");
    }
}
