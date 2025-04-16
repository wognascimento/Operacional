using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Operacional.DataBase.Models
{
    [Table("t_empresas", Schema = "operacional")]
    public class OperacionalEmpresaModel
    {
        [Key]
        public required long codigo_empresa { get; set; }
        public required string nome_empresa { get; set; }
    }
}
