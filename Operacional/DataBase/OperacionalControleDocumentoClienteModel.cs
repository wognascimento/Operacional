using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Operacional.DataBase;

[Table("tblcontrole_documento_cliente", Schema = "operacional")]
public class OperacionalControleDocumentoClienteModel
{
    [Key]
    public int id { get; set; }
    public int id_documento { get; set; }
    public string sigla { get; set; }
    public string? fecha { get; set; }
    public bool? direcionado_resp { get; set; }
    public string? direcionado_resp_por { get; set; }
    public DateTime? direcionado_resp_em { get; set; }
    public bool? em_analise { get; set; }
    public string? em_analise_por { get; set; }
    public DateTime? em_analise_em { get; set; }
    public bool? concluido { get; set; }
    public string? concluido_por { get; set; }
    public DateTime? concluido_em { get; set; }
    public bool? enviado { get; set; }
    public string? enviado_por { get; set; }
    public DateTime? enviado_em { get; set; }
}
