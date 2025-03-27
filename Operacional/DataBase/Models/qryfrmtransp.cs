using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Operacional.DataBase.Models;

[Keyless]
[Table("qryfrmtransp", Schema = "operacional")]
public partial class qryfrmtransp
{
    [StringLength(50)]
    public string? siglaserv { get; set; }

    [StringLength(30)]
    public string? transporte { get; set; }

    [StringLength(30)]
    public string? regiao { get; set; }

    [StringLength(30)]
    public string? cidade { get; set; }

    public DateTime? data_de_expedicao { get; set; }

    public int? volume_da_carga { get; set; }

    public int numero_de_caminhoes { get; set; }

    [StringLength(150)]
    public string? transportadora { get; set; }

    [StringLength(5)]
    public string? ok { get; set; }

    [Column(TypeName = "character varying")]
    public string? origem { get; set; }

    [StringLength(30)]
    public string? post_alterado { get; set; }

    [Column(TypeName = "timestamp without time zone")]
    public DateTime? post_data_alterado { get; set; }

    public int? volume_informado { get; set; }

    public int? distancia { get; set; }

    public double? valor_frete_contratado { get; set; }

    public decimal? cubagem_por_produto { get; set; }

    public double? cubagem_expedida { get; set; }

    public double? perc_shop { get; set; }

    public DateTime? dataaltera { get; set; }

    [StringLength(50)]
    public string? alteradopor { get; set; }

    [StringLength(30)]
    public string? sigla_serv { get; set; }
}
