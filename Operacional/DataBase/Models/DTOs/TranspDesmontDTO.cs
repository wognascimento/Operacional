using System.Collections.ObjectModel;

namespace Operacional.DataBase.Models.DTOs;

public class TranspDesmontDTO
{
    public string? siglaserv { get; set; }
    public string? transporte { get; set; }
    public string? regiao { get; set; }
    public string? cidade { get; set; }
    public int? prazo_chegada { get; set; }
    public int? volume_carga_desmontagem { get; set; }
    public int? num_caminhoes_desmont { get; set; }
    public DateTime? data_inicio_desmontagem { get; set; }
    public DateTime? data_final_desmontagem{ get; set; }
	public DateTime? data_libera_area_desmontagem{ get; set; }
    public string? transportadora { get; set; }
    public string? ok { get; set; }
    public string? local_descarga { get; set; }
    public string? comboio { get; set; }
    public double? valor_frete_desmont { get; set; }
    public double? cubagem_por_produto { get; set; }
    public double? cubagem_expedida { get; set; }
    public double? perc_shop { get; set; }
    public DateTime? data_altera { get; set; }
    public string? alterado_por { get; set; }
    public string? sigla_serv { get; set; }

    public ObservableCollection<OperacionalCargaDesmontagemModel> Cargas { get; set; }
}
