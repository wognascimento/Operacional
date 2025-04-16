using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Operacional.DataBase.Models
{
    [Table("tblempresa", Schema = "compras")]
    public class ComprasEmpresaModel
    {
        [Key]
        public long? codigo { get; set; }
        public string? nome { get; set; }
        public string? endereco { get; set; }
        public string? cnpj { get; set; }
        public string? inscricao { get; set; }
        public string? bairro { get; set; }
        public string? cidade { get; set; }
        public string? estado { get; set; }
        public string? abreviacao { get; set; }
        public string? localentrega { get; set; }
        public string? cep { get; set; }
        public string? telefone { get; set; }
        public string? fax { get; set; }
        public int? totvs { get; set; }
        public string? conta { get; set; }
        public string? conta_cartao { get; set; }
        public string? conta_caixa { get; set; }
    }
}
