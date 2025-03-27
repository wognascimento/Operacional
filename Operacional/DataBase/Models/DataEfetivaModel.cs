using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Operacional.DataBase.Models
{
    [Table("t_data_efetiva", Schema = "operacional")]
    public class DataEfetivaModel
    {
        [Key]
        public string? siglaserv { get; set; }
        public DateTime? data_inicio_montagem { get; set; }
        public DateTime? data_termino_montagem { get; set; }
        public DateTime? data_inauguracao { get; set; }
        public DateTime? data_inicio_desmontagem { get; set; }
        public DateTime? data_final_desmontagem { get; set; }
        public DateTime? data_libera_area_desmontagem { get; set; }
        public double? prazotransportecliente { get; set; }
        public string? alterado_por { get; set; }
        public DateTime? data_alterado { get; set; }
        public string? confirmado { get; set; }
        public DateTime? data_informada_cliente { get; set; }
        public string? confirmado_dsl { get; set; }
        public string? obs_data_inicio_montagem { get; set; }
        public string? obs_data_termino_montagem { get; set; }
        public int? diarias_cronograma { get; set; }
        public DateTime? data_pedido_cliente_inicio_montagem { get; set; }
        public DateTime? data_pedido_cliente_termino_montagem { get; set; }
        public string? obs_desmontagem { get; set; }
        public DateTime? data_combinada_mo_inicio { get; set; }
        public DateTime? data_combinada_mo_fim { get; set; }
    }
}
