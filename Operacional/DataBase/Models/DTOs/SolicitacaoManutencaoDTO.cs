using System.ComponentModel.DataAnnotations;

namespace Operacional.DataBase.Models.DTOs;

public class SolicitacaoManutencaoDTO
{
    public int Id { get; set; }
    [Required]
    public int IdProgramacao { get; set; }
    [Required]
    public string Tipo { get; set; }
    [Required]
    public string Item { get; set; }
    [Required]
    public string Solicitacao { get; set; }
    public List<SolicitacaoManutencaoFotoDTO> Imagens { get; set; } = [];
}

public class SolicitacaoManutencaoFotoDTO
{
    public int Id { get; set; }
    [Required]
    public required int IdSolicitacao { get; set; }
    [Required]
    public required string CaminhoImagem { get; set; }
}
