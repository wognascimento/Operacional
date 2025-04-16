using System.Collections.ObjectModel;

namespace Operacional.DataBase.Models.DTOs
{
    public class QryTransporteDTO
    {

        public string? SiglaServ { get; set; }
        public DateTime? data_de_expedicao { get; set; }
        public decimal? cubagem_por_produto { get; set; }
        public int? volume_da_carga { get; set; }
        public double? cubagem_expedida { get; set; }
        public double? perc_shop { get; set; }
        public int? volume_informado { get; set; }
        public int numero_de_caminhoes { get; set; }
        public int? distancia { get; set; }
        public string? transporte { get; set; }
        public string? cidade { get; set; }
        public string? regiao { get; set; }
        public string? Origem { get; set; }
        public string? transportadora { get; set; }
        public string? OK { get; set; }
        public double? valor_frete_contratado { get; set; }
        public string? AlteradoPor { get; set; }
        public DateTime? DataAltera { get; set; }
        public ObservableCollection<QryCargaMontagemDTO> Cargas { get; set; }
    }
}
