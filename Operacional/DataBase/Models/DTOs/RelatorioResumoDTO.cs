namespace Operacional.DataBase.Models.DTOs;

public class RelatorioResumoDTO
{
    public string? equipe { get; set; }
    //public string? tipo_detalhe { get; set; }
    //public string? descricao { get; set; }
    public string? numero_nf { get; set; }
    public double? valor_detalhe { get; set; }
    public double? saldo { get; set; }
    public DateTime data { get; set; }
    public DateTime data_pagto { get; set; }
    public string? empresa_pagadora { get; set; }
}
