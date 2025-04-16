namespace Operacional.DataBase.Models.DTOs
{
    public class QryCargaMontagemDTO
    {
        public string siglaserv { get; set; }
        public DateTime? data { get; set; }
        public string? num_caminhao { get; set; }
        public string? placa_caminhao { get; set; }
        public double? m3_contratado { get; set; }
        public int? m3_utilizado { get; set; }
        public TimeOnly? hora_saida { get; set; }
        public string? obs { get; set; }
        public string? local_carga { get; set; }
        public string? obscarga { get; set; }
        public string? trasnportadora { get; set; }
        public string? veiculo_programado { get; set; }
        public DateOnly? data_chegada { get; set; }
        public DateTime? data_chegada_efetiva { get; set; }
        public string? obs_saida { get; set; }
        public double? valor_frete_contratado_caminhao { get; set; }
        public int? noite_montagem { get; set; }
        public string? obs_externas { get; set; }
        public string? obs_frete_contratado { get; set; }

        // Referência ao Pai
        public QryTransporteDTO TransportePai { get; set; }
    }
}
