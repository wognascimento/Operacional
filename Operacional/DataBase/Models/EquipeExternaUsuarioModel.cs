using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Operacional.DataBase.Models
{
    [Table("tblusuario", Schema = "equipe_externa")]
    public class EquipeExternaUsuarioModel
    {
        [Key]
        public long? id { get; set; }
        [Required]
        public required long id_equipe { get; set; }
        [Required]
        public required string nome { get; set; }
        [Required]
        public required string email { get; set; }
        public string? aux { get; set; }
    }
}
