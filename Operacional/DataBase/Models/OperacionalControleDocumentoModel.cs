using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Operacional.DataBase.Models;

[Table("tblcontrole_documento", Schema = "operacional")]
public class OperacionalControleDocumentoModel
{
    [Key]
    public int id { get; set; }
    public string item { get; set; }
    public string? quando_enviar { get; set; }
    public string? responsavel_liberacao { get; set; }
    public string? email_responsavel_liberacao { get; set; }
}
