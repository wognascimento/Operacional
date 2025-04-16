using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Operacional.DataBase.Models
{
    [Table("t_desp_relatorios", Schema = "operacional")]
    public class OperacionalDespRelatorioModel
    {
        [Key]
        public long? cod_relatorio { get; set; }
        public string? descricao_relatorio { get; set; }
    }
}
