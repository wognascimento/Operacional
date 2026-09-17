using CommunityToolkit.Mvvm.ComponentModel;
using Dapper;
using Npgsql;
using Operacional.DataBase;
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
    private readonly DataBaseSettings _dataBaseSettings = DataBaseSettings.Instance;

    [ObservableProperty]
    private ObservableCollection<OperacionalFuncoesCronogramaModel> funcoes;

    [ObservableProperty]
    private ObservableCollection<OperacionalPessoasManutencaoModel> manutencaoFuncoes;

    public async Task LoadFuncoesAsync()
    {
        using var connection = new NpgsqlConnection(_dataBaseSettings.ConnectionString);
        var funcoes = await connection.QueryAsync<OperacionalFuncoesCronogramaModel>(
            @"SELECT *
              FROM operacional.tblfuncoes_cronograma
              ORDER BY funcao;");

        Funcoes = new ObservableCollection<OperacionalFuncoesCronogramaModel>(funcoes);
    }   

    public async Task LoadManutencaoFuncoesAsync(long idProgramacao)
    {
        using var connection = new NpgsqlConnection(_dataBaseSettings.ConnectionString);
        var funcoes = await connection.QueryAsync<OperacionalPessoasManutencaoModel>(
            @"SELECT *
              FROM operacional.tbl_pessoas_manutencao
              WHERE id_programacao = @idProgramacao
              ORDER BY funcao;",
            new { idProgramacao });

        ManutencaoFuncoes = new ObservableCollection<OperacionalPessoasManutencaoModel>(funcoes);
    }

    public async Task AddManutencaoFuncoesAsync(OperacionalPessoasManutencaoModel model)
    {
        using var connection = new NpgsqlConnection(_dataBaseSettings.ConnectionString);

        if (model.id <= 0)
        {
            model.id = await connection.ExecuteScalarAsync<int>(@"
                INSERT INTO operacional.tbl_pessoas_manutencao
                (id_programacao, funcao, qtd)
                VALUES (@id_programacao, @funcao, @qtd)
                RETURNING id;",
                model);
        }
        else
        {
            var linhas = await connection.ExecuteAsync(@"
                UPDATE operacional.tbl_pessoas_manutencao
                SET id_programacao = @id_programacao,
                    funcao = @funcao,
                    qtd = @qtd
                WHERE id = @id;",
                model);

            if (linhas != 1)
            {
                throw new InvalidOperationException("O registro nao existe mais ou foi alterado por outro usuario. Recarregue a tela; nenhum novo registro foi criado.");
            }
        }

        await LoadManutencaoFuncoesAsync(model.id_programacao);
    }


}
