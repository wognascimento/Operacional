using Microsoft.EntityFrameworkCore;
using Operacional.DataBase.Models.DTOs;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Operacional.DataBase.Models;

[PrimaryKey("siglaserv", "num_caminhao")]
[Table("tbl_cargas_montagem", Schema = "operacional")]
public partial class tbl_cargas_montagem
{
    [Key]
    [StringLength(50)]
    public string siglaserv { get; set; }

    public DateTime? data { get; set; }

    [Key]
    [StringLength(10)]
    public string? num_caminhao { get; set; }

    [StringLength(10)]
    public string? placa_caminhao { get; set; }

    public double? m3_contratado { get; set; }

    public int? m3_utilizado { get; set; }

    public TimeOnly? hora_saida { get; set; }

    [StringLength(254)]
    public string? obs { get; set; }

    [StringLength(15)]
    public string? local_carga { get; set; }

    [StringLength(200)]
    public string? obscarga { get; set; }

    [StringLength(100)]
    public string? trasnportadora { get; set; }

    [StringLength(50)]
    public string? veiculo_programado { get; set; }

    public DateOnly? data_chegada { get; set; }

    public DateTime? data_chegada_efetiva { get; set; }

    [StringLength(150)]
    public string? obs_saida { get; set; }

    public double? valor_frete_contratado_caminhao { get; set; }

    public int? noite_montagem { get; set; }

    [StringLength(254)]
    public string? obs_externas { get; set; }

    [StringLength(100)]
    public string? obs_frete_contratado { get; set; }

    // Referência ao Pai
    //public QryTransporteDTO TransportePai { get; set; }
}
