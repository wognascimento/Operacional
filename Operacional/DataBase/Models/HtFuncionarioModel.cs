using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Operacional.DataBase.Models
{
    [Table("view_ht_funcionarios", Schema = "ht")]
    public class HtFuncionarioModel
    {
        [Key]
        public long? codfun { get; set; }
        public string? nome_apelido { get; set; }
        public string? rg { get; set; }
        public string? sexo { get; set; }
        public long? matricula { get; set; }
        public string? ncamiseta { get; set; }
        public double? ncalcado { get; set; }
        public DateTime? data_admissao { get; set; }
        public DateTime? data_demissao { get; set; }
        public string? apelido { get; set; }
        public string? setor { get; set; }
        public string? funcao { get; set; }
        public string? empresa { get; set; }
        public string? ativo { get; set; }
        public string? impresso { get; set; }
        public string? cidade { get; set; }
        public string? local { get; set; }
        public string? local_galpao { get; set; }
        public string? exibir_furo { get; set; }
        public long? codigo_setor { get; set; }
        public string? ocultar_dados { get; set; }
        public string? setor_principal { get; set; }
    }
}
