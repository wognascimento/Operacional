namespace Operacional.Views.EquipeExterna.Consultas;

internal static class EquipePrevistaRealizadaSql
{
    internal const string Consulta = """
        WITH parametros AS (
            SELECT CURRENT_DATE AS data_corte
        ), previsao_pessoas AS (
            SELECT sigla, SUM(COALESCE(qtd_pessoas, 0)) AS pessoas_previstas
            FROM operacional.tblnoitescronog_qtd_pessoa_funcao
            WHERE fase = 'MONTAGEM'
            GROUP BY sigla
        ), datas AS (
            SELECT siglaserv AS sigla,
                   MIN(data_inicio_montagem)::date AS data_inicio,
                   MAX(data_termino_montagem)::date AS data_fim
            FROM operacional.t_data_efetiva
            GROUP BY siglaserv
        ), previsao_diaria AS (
            SELECT p.sigla, dia::date AS data, p.pessoas_previstas
            FROM previsao_pessoas p
            INNER JOIN datas d ON d.sigla = p.sigla
            CROSS JOIN parametros par
            CROSS JOIN LATERAL generate_series(
                d.data_inicio::timestamp,
                LEAST(d.data_fim, par.data_corte)::timestamp,
                INTERVAL '1 day'
            ) AS calendario(dia)
            WHERE d.data_inicio IS NOT NULL
              AND d.data_fim IS NOT NULL
              AND d.data_fim >= d.data_inicio
        ), realizado_diario AS (
            SELECT r.sigla_serv AS sigla, r.data::date AS data,
                   MAX(COALESCE(r.externa_pessoas1, 0)
                     + COALESCE(r.externa_pessoas2, 0)
                     + COALESCE(r.externa_pessoas3, 0)) AS pessoas_realizadas
            FROM equipe_externa.tblrelatorio_diario r
            CROSS JOIN parametros par
            WHERE r.fase = 'MONTAGEM'
              AND r.data::date <= par.data_corte
            GROUP BY r.sigla_serv, r.data::date
        ), comparativo AS (
            SELECT COALESCE(p.sigla, r.sigla) AS sigla,
                   COALESCE(p.data, r.data) AS data,
                   COALESCE(p.pessoas_previstas, 0) AS pessoas_previstas,
                   COALESCE(r.pessoas_realizadas, 0) AS pessoas_realizadas,
                   CASE WHEN p.sigla IS NULL THEN 'SEM PREVISAO'
                        WHEN r.sigla IS NULL THEN 'SEM RELATORIO'
                        ELSE 'COM RELATORIO' END AS situacao
            FROM previsao_diaria p
            FULL OUTER JOIN realizado_diario r ON r.sigla = p.sigla AND r.data = p.data
        ), acumulados AS (
            SELECT *,
                   SUM(pessoas_previstas) OVER janela AS previsto_acumulado,
                   SUM(pessoas_realizadas) OVER janela AS realizado_acumulado
            FROM comparativo
            WINDOW janela AS (
                PARTITION BY sigla ORDER BY data
                ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW
            )
        )
        SELECT sigla, data, pessoas_previstas, pessoas_realizadas,
               pessoas_realizadas - pessoas_previstas AS saldo_dia,
               previsto_acumulado, realizado_acumulado,
               realizado_acumulado - previsto_acumulado AS saldo_acumulado,
               GREATEST(previsto_acumulado - realizado_acumulado, 0) AS deficit_acumulado,
               ROUND((100.0 * GREATEST(previsto_acumulado - realizado_acumulado, 0)
                     / NULLIF(previsto_acumulado, 0))::numeric, 2) AS percentual_deficit_acumulado,
               situacao
        FROM acumulados
        ORDER BY sigla, data;
        """;
}
