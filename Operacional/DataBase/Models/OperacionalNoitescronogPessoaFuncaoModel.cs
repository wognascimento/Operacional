using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Operacional.DataBase.Models;

[Table("tblnoitescronog_qtd_pessoa_funcao", Schema = "operacional")]
public class OperacionalNoitescronogPessoaFuncaoModel
{
    [Key]
    public long id { get; set; }
    public string? sigla { get; set; }
    public string? fase { get; set; }
    public string? funcao { get; set; }
    public double? qtd_pessoas { get; set; }
    public double? qtd_noites { get; set; }
    public bool? equipe { get; set; }
}
