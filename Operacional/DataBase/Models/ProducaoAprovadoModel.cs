using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Operacional.DataBase.Models;

[Table("t_aprovados", Schema = "producao")]
public class ProducaoAprovadoModel
{
    [Key]
    public long id_aprovado { get; set; } // Primary Key
    public string? sigla { get; set; } // Nome do funcionário
    public string? sigla_serv { get; set; } // Cargo do funcionário
    public string? nome { get; set; } // Cargo do funcionário
    public string? tema { get; set; } // Cargo do funcionário
    public string? cronog_resp { get; set; } // Cargo do funcionário
    public DateTime? cronog_data { get; set; } // Data de aprovação
}
