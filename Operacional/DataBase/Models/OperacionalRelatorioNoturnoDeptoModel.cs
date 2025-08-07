using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Operacional.DataBase.Models;

[Table("tbl_relatorio_noturno_depto", Schema = "operacional")]
public class OperacionalRelatorioNoturnoDeptoModel
{
    [Key]
    public string? depto { get; set; }
}
