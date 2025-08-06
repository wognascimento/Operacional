using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Operacional.DataBase.Models
{
    [Table("clientes", Schema = "comercial")]
    public class ComercialClienteModel
    {
        [Key]
        public string  sigla { get; set; }
        public string? cidade { get; set; }
        public string? est { get; set; }
    }
}
