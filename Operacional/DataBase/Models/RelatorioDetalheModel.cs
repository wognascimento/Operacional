using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Operacional.DataBase.Models;

[Table("tbl_detalhes_relatorio", Schema = "equipe_externa")]
public class RelatorioDetalheModel
{
    [Key]
    public long?     cod_detalhe_relatorio { get; set; }
    public long      codrelatorio { get; set; }
    public string    tipo_detalhe	{ get; set; }
    public double    valor_detalhe { get; set; }
    public DateTime? data { get; set; }
    public string?   enviado_fin	{ get; set; }
    public string    descricao   { get; set; }
    public DateTime? data_pagto { get; set; }
    public string?   numero_nf   { get; set; }
    public string?   empresa_nf  { get; set; }
    public long      id_equipe { get; set; }
    public string?   totvs   { get; set; }
    public string?   cod_servico_totvs   { get; set; }
    public string?   empresa_pagadora    { get; set; }
    public string?   aprovado_por    { get; set; }
    public DateTime? data_aprovado	{ get; set; }
    public bool?     exportado   { get; set; } = false;
    public bool?     cancelado { get; set; } = false;
    public string?   cliente { get; set; }
    public bool      envia_fluxo { get; set; } = false;
    public double?   saldo { get; set; }
    public string?   inserido_por { get; set; }
    public DateTime? inserido_em { get; set; }
    public string?   alterado_por { get; set; }
    public DateTime? alterado_em { get; set; }
    public string?   enviado_fluxo_por { get; set; }
    public DateTime? enviado_fluxo_em { get; set; }

}
