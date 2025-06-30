using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Operacional.DataBase.Models;

[Table("tblequipesext", Schema = "equipe_externa")]
public class EquipeExternaEquipeModel
{
    [Key]
    public long       id { get; set; }
    public string?   equipe_e	{ get; set; }
    public string?   nome	{ get; set; }
    public string?   tipo    { get; set; }
    public string?   cidadereferencia    { get; set; }
    public string?   razaosocial { get; set; }
    public string?   cgc { get; set; }
    public string?   insc_estadual   { get; set; }
    public string?   insc_municipal  { get; set; }
    public string?   endereco_comercial  { get; set; }
    public string?   bairro_comercial    { get; set; }
    public string?   cidade_comercial    { get; set; }
    public string?   estado_comercial    { get; set; }
    public string?   pais_comercial  { get; set; }
    public int   ?   cep_comercial { get; set; }
    public int   ?   ddi_comercial { get; set; }
    public int   ?   ddd_comercial { get; set; }
    public int   ?   tel_comercial { get; set; }
    public int   ?   fax_comercial { get; set; }
    public string?   email_comercial { get; set; }
    public string?   website_comercial { get; set; }
    public string?   responsavel { get; set; }
    public string?   profissao   { get; set; }
    public string?   numero_crea { get; set; }
    public string?   local_crea  { get; set; }
    public string?   pgto_crea { get; set; }
    public int   ?   ddd_celular { get; set; }
    public int   ?   tel_celular { get; set; }
    public int   ?   ddd_residencia { get; set; }
    public int   ?   tel_residencia { get; set; }
    public string?   endereco_residencial    { get; set; }
    public string?   bairro_residencial  { get; set; }
    public string?   cidade_residencial  { get; set; }
    public string?   estado_residencial  { get; set; }
    public string?   pais_residencial    { get; set; }
    public string?   cep_residencial { get; set; }
    public int   ?   ddi_residencial { get; set; }
    public string?   internacional { get; set; }
    public double?   valorlanche { get; set; }
    public double?   valortransporte { get; set; }
    public string?   usuario { get; set; }
    public string?   senha   { get; set; }
    public string?   cnae1   { get; set; }
    public string?   cnae2   { get; set; }
    public string?   natureza_juridica   { get; set; }
    public string?   cod_servico { get; set; }
    public string?   simples { get; set; }
    public string?   anexo_3 { get; set; }
    public int   ?   id_totvs { get; set; }
    public double?   irrf    { get; set; }
    public double?   csrf    { get; set; }
    public double?   iss { get; set; }
    public bool  ?   ativo_manutencao { get; set; }
}
