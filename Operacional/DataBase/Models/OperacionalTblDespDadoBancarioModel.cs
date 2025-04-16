using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Operacional.DataBase.Models
{
    [Table("tbl_desp_dados_bancarios", Schema = "operacional")]
    public class OperacionalTblDespDadoBancarioModel
    {
        [Key]
        public long? cod_linha_dados_bancarios { get; set; }
        public long? cod_func { get; set; }
        public string? titular_conta { get; set; }
        public string? banco { get; set; }
        public string? tipo_conta { get; set; }
        public string? agencia { get; set; }
        public string? numero_conta { get; set; }
        public string? digito_agencia { get; set; }
        public string? digito_conta { get; set; }
        public string? cpf_conta { get; set; }
        // Relacionamento: Cada dado bancário pertence a um funcionário
        [ForeignKey("cod_func")]
        public OperacionalTDespFuncionarioModel? Funcionario { get; set; }

    }
}
