namespace Operacional.DataBase.Models.DTOs;

public class FluxoDTO
{
    public double Debito { get; set; }
    public DateTime DataEmissao { get; set; }
    public string NumeroDocumento { get; set; }
    public DateTime? DataVencimento { get; set; }
    public string Conta { get; set; }
    public string Descricao { get; set; }
    public DateTime DataPagamento { get; set; }
    public string Depto { get; set; }
    public string Classif { get; set; }
    public string Tipo { get; set; }
    public string Sub_Classif { get; set; }
    public string Class3 { get; set; }
    public string Razao_Social { get; set; }
    public string? Mes { get; set; }
    public string? Mes_Emissao { get; set; }
    public string Cnpj { get; set; }
}
