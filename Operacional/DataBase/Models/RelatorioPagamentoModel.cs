using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Operacional.DataBase.Models;

[Table("tblrelatorio_pagamento", Schema = "equipe_externa")]
public class RelatorioPagamentoModel
{
    [Key]
    public long cod_relatorio { get; set; }
    public long id_equipe { get; set; }
    public string? equipe { get; set; }
    public DateTime? data { get; set; }
    public double? valor_liberado { get; set; }
    public string? empresa_pagadora { get; set; }
    public string? tipo { get; set; }
    public string? sigla { get; set; }
}
