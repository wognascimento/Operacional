using CommunityToolkit.Mvvm.ComponentModel;
using DocumentFormat.OpenXml.ExtendedProperties;
using DocumentFormat.OpenXml.InkML;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;

public partial class CustoModel : ObservableValidator
{
    
    [Required]
    public string sigla { get; set; }

    [Required]
    public DateTime? data { get; set; }

    [Required]
    public double qtd { get; set; }

    public string unid { get; set; }
    public string extra { get; set; }

    [Required]
    public string etapa { get; set; }

    [Required]
    [ObservableProperty]
    private string? classificacao;

    [Required]
    [ObservableProperty]
    private string? descricao;

    [ObservableProperty]
    private ObservableCollection<string> descricoes = [];

    [Required]
    public double vunit { get; set; }

    public int codcusto { get; set; }

    public string cadastro_por { get; set; }
    public DateTime? cadastro_data { get; set; }
    public string alterado_por { get; set; }
    public DateTime? alterado_data { get; set; }

    public void Validate()
    {
        ValidateAllProperties();
    }

    partial void OnClassificacaoChanged(string? value)
    {
        // limpa descrição quando troca classificação
        descricao = null;

        // limpa lista — será recarregada pelo ViewModel
        Descricoes.Clear();

        MarkAsModified();
    }

    public EntityState State { get; private set; } = EntityState.Unchanged;

    public void MarkAsLoaded()
    {
        State = EntityState.Unchanged;
    }

    public void MarkAsModified()
    {
        if (State == EntityState.Unchanged)
            State = EntityState.Modified;
    }

    public void MarkAsAdded()
    {
        State = EntityState.Added;
    }

    public void MarkAsSaved()
    {
        State = EntityState.Unchanged;
    }

    public enum EntityState
    {
        Unchanged,
        Modified,
        Added
    }
}
