using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Operacional.DataBase.Models;

[Table("tbl_solicitacao_manutencao_foto", Schema = "operacional")]
public class OperacionalSolicitacaoManutencaoFotoModel
{
    [Key]
    public int id { get; set; }
    [Required]
    public int id_solicitacao { get; set; }
    [Required]
    public required string caminho_imagem { get; set; }

}
