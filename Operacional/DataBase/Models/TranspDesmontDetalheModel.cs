namespace Operacional.DataBase.Models;

public class TranspDesmontDetalheModel
{
    public long id { get; set; }
    public string sigla_serv { get; set; }
    public string cidade { get; set; }
    public string est { get; set; }
    public double distancia { get; set; }
    public string transporte { get; set; }
    public string regiao { get; set; }
    public string local_descarga { get; set; }
    public DateTime? data_chegada_shopping { get; set; }
    public DateTime? data_saida_shopping { get; set; }
    public int prazo_chegada { get; set; }
    public DateTime? data_chegada_cipolatti { get; set; }
    public int volume_carga_desmontagem { get; set; }
    public int num_caminhoes_desmont { get; set; }
    public string? caminhao { get; set; }
    public int? volume { get; set; }
    public DateTime data_altera { get; set; }
    public string alterado_por { get; set; }
    public string comboio { get; set; }
    public string? obs_recebimento { get; set; }
    public DateTime? chegada_calculada { get; set; }
    public string? obs { get; set; }
    public string? transportadora { get; set; }
    public double? vl_est_frete { get; set; }
    public double? vl_est_seguro { get; set; }
    public double? vl_est_icms { get; set; }
    public double? vl_est_total { get; set; }
    public string? descarga_caminhao { get; set; }
    public string? obs_embalagem { get; set; }
    public DateTime? data_chegada_galpao { get; set; }
    public DateTime? hora_chegada_galpao { get; set; }
    public string? placa_caminhao { get; set; }
    public string? obs_frete_caminhao_desmont { get; set; }

}
