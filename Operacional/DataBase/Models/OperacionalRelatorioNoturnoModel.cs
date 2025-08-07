using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Operacional.DataBase.Models;

[Table("tbl_relatorio_noturno", Schema = "operacional")]
public class OperacionalRelatorioNoturnoModel
{
    [Key]
    public long? cod_relatorio_noturno { get; set; }
    [Required]
    public string? sigla { get; set; }
    [Required]
    public DateTime? data { get; set; } = DateTime.Now;
    [Required]
    public string? depto { get; set; }
    [Required]
    public string? detalhe { get; set; }
    public string? classificacao_detalhe { get; set; }
    [Required]
    public int? noite { get; set; }
    [Required]
    public string? coordenador { get; set; }
    public string? grau_de_urgencia { get; set; }
    public string? retorno_informacao { get; set; }
    public string? inserido_por { get; set; }
    public DateTime? inserido_em { get; set; }
}
