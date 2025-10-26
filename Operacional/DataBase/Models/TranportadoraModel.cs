using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Operacional.DataBase.Models;

[Table("tbltranportadoras", Schema = "operacional")]
public class TranportadoraModel
{
    [Key]
    public long? codtransportadora { get; set; }
    [Required]
    public required string nometransportadora { get; set; }
    [Required]
    public required string cep { get; set; }
    [Required]
    public required string endereco { get; set; }
    [Required]
    public required string bairro { get; set; }
    [Required]
    public required string cidade { get; set; }
    [Required]
    public required string uf { get; set; }
    public string? ie { get; set; }
    public string? ccm { get; set; }
    [Required]
    public required string cnpj { get; set; }
    [Required]
    public required int ddd { get; set; }
    [Required]
    public required int fone_1 { get; set; }
    public int? fone_2 { get; set; }
    public string? contato { get; set; }
    public string? id_nextel { get; set; }
}
