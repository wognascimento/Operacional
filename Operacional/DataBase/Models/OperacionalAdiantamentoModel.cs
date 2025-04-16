using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Operacional.DataBase.Models
{
    [Table("t_adiantamento", Schema = "operacional")]
    public class OperacionalAdiantamentoModel
    {
        [Key]
        public long cod_relatorio { get; set; }
        public double? valor_adiantamento { get; set; }
        public double? valor_real_peso { get; set; }
        public double? valor_cotacao_peso { get; set; }
        public double? valor_peso_peso { get; set; }
        public double? valor_real_dolar { get; set; }
        public double? valor_cotacao_dolar { get; set; }
        public double? valor_dolar_dolar { get; set; }
        public double? total_adiantamento { get; set; }
        public double? total_despesas { get; set; }
        public double? saldo_final { get; set; }
        public string? emitido_por { get; set; }
        public DateTime? emitido_data { get; set; }
        public string? alterado_por { get; set; }
        public DateTime? alterado_data { get; set; }
        public string? aprovado_por { get; set; }
        public DateTime? data_aprovacao { get; set; }
        public DateTime? data_pagamento { get; set; }
        public string? forma_pagto { get; set; }
        public string? pagto_realizado_por { get; set; }
        public DateTime? data_pagto_realizado { get; set; }
        public string? aprovacao { get; set; }

        [ForeignKey("cod_relatorio")]
        public OperacionalRelatorioDespesaModel? RelatorioDespesa { get; set; }
    }
}
