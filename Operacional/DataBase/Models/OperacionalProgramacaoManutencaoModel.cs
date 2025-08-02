using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Operacional.DataBase.Models;

[Table("tbl_programacao_manutencao", Schema = "operacional")]
public class OperacionalProgramacaoManutencaoModel
{
    [Key]
    public int id { get; set; }

    [Required]
    public DateTime? data { get; set; } = null;

    [Required]
    public string shopp { get; set; }
    public string cidade { get; set; }
    public string est { get; set; }

    [Required]
    public string? tipo { get; set; }

    public string? orientacao { get; set; }

    public int? qtde_pessoa { get; set; }

    public string? nome_equipe { get; set; }

    public bool? relatorio_enviado { get; set; }
    public bool? relatorio_retorno { get; set; }

    public string? cadastrado_por { get; set; }

    public DateTimeOffset? data_cadastro { get; set; }

    public string? alterado_por { get; set; }

    public DateTimeOffset? data_alteracao { get; set; }

    public float? vlortotalpessoa { get; set; }
    public string? obs_retorno { get; set; }

    public bool? mat_envio { get; set; }
    public int? mat_chk { get; set; }
    public bool? mat_conf_entrega { get; set; }
    public DateTime? mat_data_entrega { get; set; }

    public string? mat_resp { get; set; }
    public DateTimeOffset? mat_data { get; set; }

    public string? motivo { get; set; }

    public string? nome_equipe_2 { get; set; }
    public int? qtde_pessoa_2 { get; set; }

    public bool? relatorio_enviado_2 { get; set; }
    public bool? relatorio_retorno_2 { get; set; }

    public string? periodo { get; set; }

    public int? id_kit_solucao { get; set; }

    public DateTime? data_chamado_cliente { get; set; }
    public string? informmacoes_passadas_cliente { get; set; }

    public TimeSpan? hora_solicitada_interdicao { get; set; }
    public DateTime? data_conclusao { get; set; }
    public TimeSpan? hora_conclusao { get; set; }
    public TimeSpan? hora_interdicao { get; set; }

    public string? motivo_interno { get; set; }

    public string? detalhes_motivo { get; set; }

    public string? resp_atendimento { get; set; }
}
