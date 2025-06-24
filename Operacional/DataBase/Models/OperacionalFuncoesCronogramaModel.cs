using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Operacional.DataBase.Models;

[Table("tblfuncoes_cronograma", Schema = "operacional")]
public class OperacionalFuncoesCronogramaModel
{
    [Key]
    public string? funcao { get; set; }
    public string? grupo { get; set; }
}
