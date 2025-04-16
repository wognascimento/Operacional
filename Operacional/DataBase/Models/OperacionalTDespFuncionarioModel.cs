using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Operacional.DataBase.Models
{
    [Table("t_desp_funcionario", Schema = "operacional")]
    public class OperacionalTDespFuncionarioModel
    {
        [Key]
        public long? cod_func { get; set; }
        public string? nome_func { get; set; }
        public string? telefone_func { get; set; }
        public string? celular_func { get; set; }
        public string? cidade_func { get; set; }
        public string? estado_func { get; set; }
        public string? observacao { get; set; }
        public string? cpf { get; set; }
        public string? empresa { get; set; }
        public string? tipo_financeiro { get; set; }
        public string? cnpj_razao_social { get; set; }
        // Relacionamento: Um funcionário pode ter vários dados bancários
        public ICollection<OperacionalTblDespDadoBancarioModel> DadosBancarios { get; set; } = [];
    }
}
