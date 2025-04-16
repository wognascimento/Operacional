using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Operacional.DataBase.Models
{
    [Table("tblbasecustos", Schema = "operacional")]
    public class OperacionalBaseCustoModel
    {
        public required string tipo { get; set; }
        [Key]
        public required string descr { get; set; }
    }
}
