using Operacional.Views.Despesa;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Operacional.DataBase.Models
{
    [Table("t_relatorio_despesas", Schema = "operacional")]
    public class OperacionalRelatorioDespesaModel
    {
        [Key]
        public long cod_relatorio { get; set; }
        public DateTime? data { get; set; }
        public long? codigo_funcionario { get; set; }
        public string? nome_funcionario { get; set; }
        public string? nome_relatorio { get; set; }
        public string? localidade { get; set; }
        public long? codigo_empresa { get; set; }
        public string? emitido_por { get; set; }
        public DateTime? emitido_data { get; set; }
        public long? cod_conta_corrente { get; set; }
        public string? finalizado { get; set; }
        public string? classif_financeiro { get; set; }

        public ICollection<OperacionalRelatorioObservacaoModel>? RelatorioObservacao { get; set; } = [];
        public ICollection<OperacionalAdiantamentoModel>? RelatorioAdiantamento { get; set; } = [];
        public ICollection<OperacionalRelatorioDespesasDetalheModel> RelatorioDespesaDetalhes { get; set; } = [];

        [NotMapped]
        public ObservableCollection<RegistroDespesa> RelatorioDespesaDetalhesEditaveis { get; set; } = [];

    }
}
