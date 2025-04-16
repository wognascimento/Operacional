using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Operacional.DataBase.Models;

[Table("tbltranportadoras", Schema = "operacional")]
[Index("nometransportadora", Name = "tbltranportadoras_nometransportadora_key", IsUnique = true)]
public partial class tbltranportadora
{
    [Key]
    public int codtransportadora { get; set; }

    [Required]
    [StringLength(200)]
    public string nometransportadora { get; set; }

    [Required]
    [StringLength(200)]
    public string endereco { get; set; }

    [Required]
    [StringLength(100)]
    public string bairro { get; set; }

    [Required]
    [StringLength(10)]
    public string cep { get; set; }

    [Required]
    [StringLength(150)]
    public string cidade { get; set; }

    [Required]
    [StringLength(15)]
    public string uf { get; set; }

    [Required]
    [StringLength(15)]
    public string ie { get; set; }

    [StringLength(15)]
    public string ccm { get; set; }

    [Required]
    [StringLength(60)]
    public string cnpj { get; set; }

    public int ddd { get; set; }

    public int fone_1 { get; set; }

    public int? fone_2 { get; set; }

    [Required]
    [StringLength(50)]
    public string contato { get; set; }

    [StringLength(15)]
    public string id_nextel { get; set; }
}
