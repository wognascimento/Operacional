using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Operacional.DataBase.Models
{
    [Table("t_transportes_mont", Schema = "operacional")]
    public class TransporteMontagemModel
    {
        [Key]
        [Column("siglaserv")]
        [StringLength(50)]
        public string SiglaServ { get; set; }

        [Column("data_de_expedicao")]
        public DateTime? DataDeExpedicao { get; set; }

        [Column("hora_expedicao")]
        public TimeSpan? HoraExpedicao { get; set; }

        [Column("volume_da_carga")]
        public int? VolumeDaCarga { get; set; }

        [Column("numero_de_caminhoes")]
        public int NumeroDeCaminhoes { get; set; } = 0;

        [Column("transportadora")]
        [StringLength(150)]
        public string? Transportadora { get; set; }

        [Column("ok")]
        [StringLength(5)]
        public string? Ok { get; set; }

        [Column("dataaltera")]
        public DateTimeOffset? DataAltera { get; set; }

        [Column("alteradopor")]
        [StringLength(50)]
        public string? AlteradoPor { get; set; }

        [Column("origem")]
        [StringLength(255)] // Sem tamanho definido no banco, assumindo 255 caracteres
        public string Origem { get; set; } = "JACAREÍ";

        [Column("post_alterado")]
        [StringLength(30)]
        public string? PostAlterado { get; set; }

        [Column("post_data_alterado")]
        public DateTime? PostDataAlterado { get; set; }

        [Column("volume_informado")]
        public int? VolumeInformado { get; set; }

        [Column("distancia")]
        public int Distancia { get; set; } = 0;

        [Column("valor_frete_contratado")]
        public double? ValorFreteContratado { get; set; }
    }
}
