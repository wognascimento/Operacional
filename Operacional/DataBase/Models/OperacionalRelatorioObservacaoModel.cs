using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Operacional.DataBase.Models
{
    [Table("t_relatorio_observacao", Schema = "operacional")]
    public class OperacionalRelatorioObservacaoModel
    {
        [Key]
        public long cod_relatorio { get; set; }
        public string? observacao { get; set; }

        [ForeignKey("cod_relatorio")]
        public OperacionalRelatorioDespesaModel? RelatorioDespesa { get; set; }
    }
}
