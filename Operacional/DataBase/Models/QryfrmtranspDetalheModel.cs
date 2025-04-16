using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace Operacional.DataBase.Models
{
    [Keyless]
    [Table("qryfrmtransp_detalhe", Schema = "operacional")]
    public partial class QryfrmtranspDetalheModel
    {
        public DateTime? data { get; set; }
        public DateTime? data_de_expedicao { get; set; }
        public string? siglaserv { get; set; }
        public string? num_caminhao { get; set; }
        public int? numero_de_caminhoes { get; set; }
        public int? noite_montagem { get; set; }
        public double? m3_contratado { get; set; }
        public DateTime? data_chegada { get; set; }
        public string? transporte { get; set; }
        public string? obscarga { get; set; }
        public string? regiao { get; set; }
        public string? cidade { get; set; }
        public string? veiculo_programado { get; set; }
        public string? trasnportadora { get; set; }
        public string? obs_saida { get; set; }
        public int? volume_informado { get; set; }
        public int? distancia { get; set; }
        public string? local_carga { get; set; }
        public string? obs_externas { get; set; }
        public double? valor_frete_contratado_caminhao { get; set; }
        public string? obs_frete_contratado { get; set; }
        public char? ok { get; set; }
        public DateTime? dataaltera { get; set; }
        public string? alteradopor { get; set; }
        public string? origem { get; set; }
        public int? volume_da_carga { get; set; }
        public string? post_alterado { get; set; }
        public DateTime? post_data_alterado { get; set; }
        public DateTime? data_chegada_efetiva { get; set; }
    }
}
