using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Operacional.DataBase.Models;

[Table("tbl_solicitacao_manutencao", Schema = "operacional")]
public class OperacionalSolicitacaoManutencaoModel
{
    [Key]
    public int id { get; set; }

    [Required]
    public int id_programacao { get; set; }

    [Required]
    [MaxLength(20)]
    public string tipo { get; set; }

    [Required]
    [MaxLength(20)]
    public string item { get; set; }

    [Required]
    public string solicitacao { get; set; }

}
