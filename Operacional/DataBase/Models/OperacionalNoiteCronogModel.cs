using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Operacional.DataBase.Models;

[Table("tblnoitescronog", Schema = "operacional")]
public class OperacionalNoiteCronogModel
{
    [Key]
    public long? codfecha { get; set; }
    public string? sigla { get; set; }
    public string? obs_coordenador { get; set; }
    public string? obs_cliente { get; set; }
    public int? alpinista_inicio { get; set; }
    public int? alpinista_fim { get; set; }
    public int? munck_inicio { get; set; }
    public int? munck_fim { get; set; }
    public double? n1 { get; set; }
    public double? enf1 { get; set; }
    public double? n2 { get; set; }
    public double? enf2 { get; set; }
    public double? n3 { get; set; }
    public double? enf3 { get; set; }
    public double? n4 { get; set; }
    public double? enf4 { get; set; }
    public double? n5 { get; set; }
    public double? enf5 { get; set; }
    public double? n6 { get; set; }
    public double? enf6 { get; set; }
    public double? n7 { get; set; }
    public double? enf7 { get; set; }
    public double? n8 { get; set; }
    public double? enf8 { get; set; }
    public double? n9 { get; set; }
    public double? enf9 { get; set; }
    public double? n10 { get; set; }
    public double? enf10 { get; set; }
    public double? n11 { get; set; }
    public double? enf11 { get; set; }
    public double? n12 { get; set; }
    public double? enf12 { get; set; }
    public double? n13 { get; set; }
    public double? enf13 { get; set; }
    public double? n14 { get; set; }
    public double? enf14 { get; set; }
    public double? n15 { get; set; }
    public double? enf15 { get; set; }
    public double? n16 { get; set; }
    public double? enf16 { get; set; }
    public string? extra { get; set; }
}
