using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Operacional.DataBase.Models
{
    [Table("t_relatorio_despesas_detalhe", Schema = "operacional")]
    public class OperacionalRelatorioDespesasDetalheModel
    {
        [Key]
        [Column(Order = 0)]
        public long? cod_linha_detalhe { get; set; }
        public long? cod_relatorio { get; set; }
        public DateTime? data { get; set; }
        public string? sigla { get; set; }
        public double? quantidade { get; set; }
        public string? etapa { get; set; }
        public string? classificacao { get; set; }
        public string? descricao { get; set; }
        public double? valor { get; set; }
        public long? codigo_empresa { get; set; }
        public long? cod_relatorio_empresa { get; set; }
        public string? emitido_por { get; set; }
        public DateTime? emitido_data { get; set; } 
        public string? alterado_por { get; set; }
        public DateTime? alterado_data { get; set; }
        public string? documento { get; set; }

        [ForeignKey("cod_relatorio")]
        public OperacionalRelatorioDespesaModel? RelatorioDespesa { get; set; }
    }
}
