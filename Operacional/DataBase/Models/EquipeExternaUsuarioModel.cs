using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Operacional.DataBase.Models
{
    [Table("tblusuario", Schema = "equipe_externa")]
    public class EquipeExternaUsuarioModel
    {
        [Key]
        public long? id { get; set; }
        public long id_equipe { get; set; }
        public string nome { get; set; }
        public string email { get; set; }
        public string? aux { get; set; }
    }
}
