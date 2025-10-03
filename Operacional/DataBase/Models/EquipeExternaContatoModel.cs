using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Operacional.DataBase.Models;

[Table("tbl_contatos", Schema = "equipe_externa")]
public class EquipeExternaContatoModel
{
    [Key]
    public long? cod_linha { get; set; }
    [Required]
    public string nome { get; set; }
    [Required]
    public string funcao { get; set; }
    [Required]
    public string? tel_1 { get; set; }
    public string? tel_2 { get; set; }
    public string? e_mail { get; set; }

}
