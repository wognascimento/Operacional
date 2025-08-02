using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Operacional.DataBase.Models
{
    [Table("clientes", Schema = "comercial")]
    public class ComercialClienteModel
    {
        [Key]
        public required string sigla { get; set; }
        public required string cidade { get; set; }
        public required string est { get; set; }
    }
}
