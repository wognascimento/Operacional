using System.ComponentModel.DataAnnotations.Schema;

namespace Operacional.DataBase.Models;

[Table("tblrelatorio_diario", Schema = "equipe_externa")]
public class RelatorioDiarioModel
{
    public int id { get; set; }
    public int user_id { get; set; }
    public string assistente { get; set; }
    public string coordenador { get; set; }
    public DateTime data { get; set; }
    public string descricao { get; set; }
    public string externa_lider1 { get; set; }
    public string externa_lider2 { get; set; }
    public string externa_lider3 { get; set; }
    public int externa_pessoas1 { get; set; }
    public int externa_pessoas2 { get; set; }
    public int externa_pessoas3 { get; set; }
    public string fase { get; set; }
    public TimeSpan interna_entrada { get; set; }
    public int interna_pessoas { get; set; }
    public TimeSpan interna_saida { get; set; }
    public string mensagem { get; set; }
    public int noite { get; set; }
    public int id_aprovado { get; set; }
    public string sigla_serv { get; set; }
    public string tipo { get; set; }
    public bool enviado { get; set; }
    public bool inseridoCipolatti { get; set; }
    public DateTime created_at { get; set; }
    public DateTime updated_at { get; set; }
}
