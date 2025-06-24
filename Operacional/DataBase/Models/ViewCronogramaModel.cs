using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Operacional.DataBase.Models;

[Table("qry_cronograma", Schema = "operacional")]
public class ViewCronogramaModel
{
    [Key]
    public long? codfecha { get; set; }
    public string tipo_evento { get; set; }
    public string sigla_completa { get; set; }
    public string sigla { get; set; }
    public long id_aprovado { get; set; }
    public string item { get; set; }
    public string localitem { get; set; }
    public string descricao { get; set; }
    public double? qtd { get; set; }
    public double? n1 { get; set; }
    public double? n2 { get; set; }
    public double? n3 { get; set; }
    public double? n4 { get; set; }
    public double? n5 { get; set; }
    public double? n6 { get; set; }
    public double? n7 { get; set; }
    public double? n8 { get; set; }
    public double? n9 { get; set; }
    public double? n10 { get; set; }
    public double? n11 { get; set; }
    public double? n12 { get; set; }
    public double? n13 { get; set; }
    public double? n14 { get; set; }
    public double? n15 { get; set; }
    public double? n16 { get; set; }
    public string? obs_coordenador { get; set; }
    public string? obs_cliente { get; set; }
}
