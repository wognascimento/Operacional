using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace Operacional.DataBase.Models
{
    [Keyless]
    [Table("qry_data_efetiva", Schema = "operacional")]
    public class QryDataEfetivaModel
    {
        public string? sigla_contrato { get; set; }
        public string? regiao { get; set; }
        public string? siglaserv { get; set; }
        public DateTime? data_inicio_montagem { get; set; }
        public TimeSpan? hora_expedicao { get; set; }
        public DateTime? data_termino_montagem { get; set; }
        public DateTime? data_inauguracao { get; set; }
        public string? est { get; set; }
        public string? obs_data_inicio_montagem { get; set; }
        public DateTime? data_inicio_desmontagem { get; set; }
        public DateTime? data_final_desmontagem { get; set; }
        public DateTime? data_libera_area_desmontagem { get; set; }
        public string? obs_data_termino_montagem { get; set; }
        public float? prazotransportecliente { get; set; }
        public string? alterado_por { get; set; }
        public DateTime? data_alterado { get; set; }
        public string? cidade { get; set; }
        public string? confirmado { get; set; }
        public DateTime? data_informada_cliente { get; set; }
        public DateTime? data_de_expedicao { get; set; }
        public char? confirmado_dsl { get; set; }
        public string? obs_desmontagem { get; set; }
        public DateTime? data_contrato_mo_inicio { get; set; }
        public DateTime? data_contrato_mo_fim { get; set; }
        public int? noites_previstas_cronograma { get; set; }
        public DateTime? data_combinada_mo_inicio { get; set; }
        public DateTime? data_combinada_mo_fim { get; set; }
        public string? transporte_por_conta { get; set; }
        public int? dias_montagem { get; set; }
        public int? diferenca { get; set; }
        public string? grupo { get; set; }
        public DateTime? data_contrato_des_inicio { get; set; }
        public DateTime? data_contrato_des_fim { get; set; }
        public int? dias_desmontagem { get; set; }
        public int? diferenca_desmontagem { get; set; }
        public int? noites_previstas_cronograma_desmontagem { get; set; }
        public string? coordenador { get; set; }
        public string? lider_equipe { get; set; }
        public string? numero_equipe { get; set; }
        public string? tema { get; set; }
    }
}
