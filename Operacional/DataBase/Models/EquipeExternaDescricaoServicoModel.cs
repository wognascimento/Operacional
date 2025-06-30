using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Operacional.DataBase.Models
{
    [Table("tbl_descricao_servicos", Schema = "equipe_externa")]
    public class EquipeExternaDescricaoServicoModel
    {
        [Key]
        public string? descricao { get; set; }
        public string? fase { get; set; }
    }
}
