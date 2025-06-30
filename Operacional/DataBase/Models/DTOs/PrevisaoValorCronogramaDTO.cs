namespace Operacional.DataBase.Models.DTOs;

public class PrevisaoValorCronogramaDTO
{
    public string?   sigla	{ get; set; }
    public double?   qtd_pessoas { get; set; }
    public double?   qtd_noites { get; set; }
    public string?   equipe	{ get; set; }
    public string?   fase    { get; set; }
    public string?   funcao  { get; set; }
    public double?   valor_ano_atual { get; set; }
    public double?   valor_total { get; set; }
    public double?   lanche { get; set; }
    public double?   transporte { get; set; }
    public int?      id_equipe { get; set; }
    public double?   indice_pessoas_noite { get; set; }
    public string?   razaosocial { get; set; }
    public bool?     vai_equipe { get; set; }
}
