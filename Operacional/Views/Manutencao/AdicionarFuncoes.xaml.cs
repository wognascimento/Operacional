using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.EntityFrameworkCore;
using Operacional.DataBase.Models;
using System.Collections.ObjectModel;
using System.Windows;
using Telerik.Windows.Controls;

namespace Operacional.Views.Manutencao;

/// <summary>
/// Interação lógica para AdicionarFuncoes.xam
/// </summary>
public partial class AdicionarFuncoes : RadWindow
{
    private int IdProgramacao;

    public AdicionarFuncoes(int idProgramacao)
    {
        InitializeComponent();
        DataContext = new AdicionarFuncoesViewModel();
        IdProgramacao = idProgramacao;
    }

    private async void RadWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            if (DataContext is AdicionarFuncoesViewModel viewModel)
            {
                await viewModel.LoadFuncoesAsync();
                await viewModel.LoadManutencaoFuncoesAsync(IdProgramacao);
            }
        }
        catch (Exception)
        {
            MessageBox.Show("Erro ao carregar dados. Verifique a conexão com o banco de dados.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void ManutFuncaoRowValidated(object sender, GridViewRowValidatedEventArgs e)
    {
        if (DataContext is AdicionarFuncoesViewModel viewModel)
        {
            if (e.Row.Item is OperacionalPessoasManutencaoModel model)
            {
                try
                {
                    await viewModel.AddManutencaoFuncoesAsync(model);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erro ao adicionar função: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }

    private void ManutFuncaoRow_AddingNewDataItem(object sender, Telerik.Windows.Controls.GridView.GridViewAddingNewEventArgs e)
    {

        e.NewObject = new OperacionalPessoasManutencaoModel
        {
            id_programacao = IdProgramacao,
        };
    }
}

public partial class AdicionarFuncoesViewModel : ObservableObject
{

    [ObservableProperty]
    private ObservableCollection<OperacionalFuncoesCronogramaModel> funcoes;

    [ObservableProperty]
    private ObservableCollection<OperacionalPessoasManutencaoModel> manutencaoFuncoes;

    public async Task LoadFuncoesAsync()
    {
        using var context = new Context();
        Funcoes = new ObservableCollection<OperacionalFuncoesCronogramaModel>(
            await context.OperacionalFuncoesCronogramas.ToListAsync());
    }   

    public async Task LoadManutencaoFuncoesAsync(long idProgramacao)
    {
        using var context = new Context();
        ManutencaoFuncoes = new ObservableCollection<OperacionalPessoasManutencaoModel>(
            await context.OperacionalPessoasManutencoes
                .Where(x => x.id_programacao == idProgramacao)
                .OrderBy(x => x.funcao)
                .ToListAsync());
    }

    public async Task AddManutencaoFuncoesAsync(OperacionalPessoasManutencaoModel model)
    {
        using var context = new Context();
        var modelExistente = await context.OperacionalPessoasManutencoes.FindAsync(model.id);
        if (modelExistente == null)
        {

            context.OperacionalPessoasManutencoes.Add(model);
        }
        else
        {
            context.Entry(modelExistente).CurrentValues.SetValues(model);
        }
        await context.SaveChangesAsync();
        await LoadManutencaoFuncoesAsync(model.id_programacao);
    }


}