using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Operacional.DataBase.Models
{
    [Table("tblfases", Schema = "operacional")]
    public class OperacionalFaseModel
    {
        [Key]
        public required string fase { get; set; }
    }
}
