namespace Operacional.DataBase.Models;

public class t_cargas_desmontagem
{
    public long id { get; set; }
    public string siglaserv { get; set; }
    public DateTime? data_chegada_shopping { get; set; }
    public DateTime? data_saida_shopping { get; set; }
    public int? volume { get; set; }
    public string? caminhao { get; set; }
    public int? prev_volume { get; set; }
    public DateTime? data_chegada_cipolatti { get; set; }
    public string? obs { get; set; }
    public string? transportadora { get; set; }
    public string? confirmado { get; set; }
    public string? descarga_caminhao { get; set; }
    public string? obs_recebimento { get; set; }
    public double? vl_est_frete { get; set; }
    public double? vl_est_seguro { get; set; }
    public double? vl_est_icms { get; set; }
    public double? vl_est_total { get; set; }
    public string? obs_embalagem { get; set; }
    public DateTime? data_chegada_galpao { get; set; }
    public DateTime? hora_chegada_galpao { get; set; }
    public string? placa_caminhao { get; set; }
    public string? obs_frete_caminhao_desmont { get; set; }

}
