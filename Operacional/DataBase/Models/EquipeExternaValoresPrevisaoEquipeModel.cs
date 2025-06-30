using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Operacional.DataBase.Models;

[Table("tbl_valores_previsao_equipe", Schema = "equipe_externa")]
public class EquipeExternaValoresPrevisaoEquipeModel
{
    [Key]
    public long cod_valor_previsao { get; set; }
    public long id_equipe { get; set; }
    public string? equipe_e { get; set; }
    public required string cliente { get; set; }
    public required string fase { get; set; }
    public required string funcao { get; set; }
    public double? valor_ano_anterior { get; set; }
    public double? valor_ano_atual { get; set; }
    public double? lanche { get; set; }
    public double? transporte { get; set; }
    public string? inserido_por { get; set; }
    public DateTime? inserido_em { get; set; }
    public string? alterado_por { get; set; }
    public DateTime? data_altera { get; set; }
}
