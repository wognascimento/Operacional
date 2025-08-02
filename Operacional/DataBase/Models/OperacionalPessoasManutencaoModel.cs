using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Operacional.DataBase.Models;

[Table("tbl_pessoas_manutencao", Schema = "operacional")]
public class OperacionalPessoasManutencaoModel
{
    [Key]
    public int id { get; set; }

    [Required]
    public int id_programacao { get; set; }

    [Required]
    public string funcao { get; set; }

    [Required]
    public int qtd { get; set; }

}
