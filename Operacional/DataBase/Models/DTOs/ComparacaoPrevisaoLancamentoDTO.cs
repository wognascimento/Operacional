namespace Operacional.DataBase.Models.DTOs;

public class ComparacaoPrevisaoLancamentoDTO
{
    public string? sigla { get; set; }
    public string? equipe_e { get; set; }
    public string? fase { get; set; }
    public string? funcoes { get; set; }
    public double? noites_previstas { get; set; }
    public double? qtd_pessoas_prevista { get; set; }
    public double? valor_total_previsto { get; set; }
    public int? noites_lancadas { get; set; }
    public int? qtd_pessoas_lancadas { get; set; }
    public double? valor_total_lancada { get; set; }
    public int? noites_extras { get; set; }
    public int? qtd_pessoas_extra { get; set; }
    public double? valor_total_extra { get; set; }
    public int? noites_total { get; set; }
    public int? qtd_pessoas_total { get; set; }
    public double? valor_total_lancadas { get; set; }
}
