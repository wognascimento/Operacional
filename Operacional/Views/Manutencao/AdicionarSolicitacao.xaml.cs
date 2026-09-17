using CommunityToolkit.Mvvm.ComponentModel;
using Dapper;
using Microsoft.Win32;
using Npgsql;
using Operacional.DataBase;
using Operacional.DataBase.Models;
using Operacional.DataBase.Models.DTOs;
using System.Collections.ObjectModel;
using System.Data;
using System.Text.RegularExpressions;
using System.Windows;
using Telerik.Windows.Controls;

namespace Operacional.Views.Manutencao;

/// <summary>
/// Interação lógica para AdicionarSolicitacao.xam
/// </summary>
public partial class AdicionarSolicitacao : RadWindow
{
    private int IdProgramacao;

    public AdicionarSolicitacao(int idProgramacao)
    {
        InitializeComponent();
        DataContext = new AdicionarSolicitacaoViewModel();
        IdProgramacao = idProgramacao;
    }

    private async void RadWindow_Loaded(object sender, System.Windows.RoutedEventArgs e)
    {
        try
        {
            if (DataContext is AdicionarSolicitacaoViewModel viewModel)
            {
                await viewModel.LoadManutencaoSolicitacaoAsync(IdProgramacao);
            }
        }
        catch (Exception)
        {
            MessageBox.Show("Erro ao carregar dados. Verifique a conexão com o banco de dados.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ManutFuncaoRow_AddingNewDataItem(object sender, Telerik.Windows.Controls.GridView.GridViewAddingNewEventArgs e)
    {
        e.NewObject = new SolicitacaoManutencaoDTO
        {
            IdProgramacao = IdProgramacao,
        };
    }

    private async void ManutFuncaoRowValidated(object sender, GridViewRowValidatedEventArgs e)
    {
        if (DataContext is AdicionarSolicitacaoViewModel viewModel)
        {
            if (e.Row.Item is SolicitacaoManutencaoDTO model)
            {
                try
                {
                    await viewModel.AddManutencaoSolicitacaoAsync(model);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erro ao adicionar função: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }

    private void RadGridViewFilho_RowValidating(object sender, GridViewRowValidatedEventArgs e)
    {

    }

    private async void OnAddImageClick(object sender, Telerik.Windows.RadRoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Title = "Selecione uma imagem",
            Filter = "Imagens (*.jpg;*.jpeg;*.png)|*.jpg;*.jpeg;*.png",
            Multiselect = false
        };
        var padrao = new Regex(@"^192\.168\.0\.");
        bool? result = dlg.ShowDialog();
        if (result == true)
        {
            string caminhoImagem = dlg.FileName;
            if(!padrao.IsMatch(caminhoImagem))
            {
                MessageBox.Show("Caminho inválido. Selecione um local da rede.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (DataContext is AdicionarSolicitacaoViewModel viewModel && e.OriginalSource is FrameworkElement element)
            {
                if (element.DataContext is SolicitacaoManutencaoDTO model)
                {
                    try
                    {
                        var fotoDTO = new SolicitacaoManutencaoFotoDTO
                        {
                            IdSolicitacao = model.Id,
                            CaminhoImagem = caminhoImagem
                        };
                        await viewModel.AddManutencaoSolicitacaoFotoAsync(fotoDTO, model.IdProgramacao);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Erro ao adicionar imagem: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

    }
}

public partial class AdicionarSolicitacaoViewModel : ObservableObject
{
    private readonly DataBaseSettings _dataBaseSettings = DataBaseSettings.Instance;

    [ObservableProperty]
    private ObservableCollection<OperacionalSolicitacaoManutencaoModel> manutencaoSolicitacao;

    [ObservableProperty]
    private ObservableCollection<OperacionalSolicitacaoManutencaoFotoModel> manutencaoSolicitacaoFotos;

    [ObservableProperty]
    private ObservableCollection<SolicitacaoManutencaoDTO> solicitacoes;

    [ObservableProperty]
    private ObservableCollection<string> tipos = ["CLIENTE", "CIPOLATTI"];

    public async Task LoadManutencaoSolicitacaoAsync(long idProgramacao)
    {
        using var connection = new NpgsqlConnection(_dataBaseSettings.ConnectionString);
        var solicitacoes = (await connection.QueryAsync<OperacionalSolicitacaoManutencaoModel>(
            @"SELECT *
              FROM operacional.tbl_solicitacao_manutencao
              WHERE id_programacao = @idProgramacao
              ORDER BY id;",
            new { idProgramacao })).ToList();

        var fotos = solicitacoes.Count == 0
            ? []
            : (await connection.QueryAsync<OperacionalSolicitacaoManutencaoFotoModel>(
                @"SELECT *
                  FROM operacional.tbl_solicitacao_manutencao_foto
                  WHERE id_solicitacao = ANY(@ids)
                  ORDER BY id;",
                new { ids = solicitacoes.Select(s => s.id).ToArray() })).ToList();

        // Mapeia entidades para DTOs
        Solicitacoes = new ObservableCollection<SolicitacaoManutencaoDTO>(
            solicitacoes.Select(solicitacao => new SolicitacaoManutencaoDTO
            {
                Id = solicitacao.id,
                IdProgramacao = solicitacao.id_programacao,
                Tipo = solicitacao.tipo,
                Item = solicitacao.item,
                Solicitacao = solicitacao.solicitacao,
                Imagens = [
                    .. fotos.Where(foto => foto.id_solicitacao == solicitacao.id)
                    .Select(foto => new SolicitacaoManutencaoFotoDTO
                    {
                        Id = foto.id,
                        IdSolicitacao = foto.id_solicitacao,
                        CaminhoImagem = foto.caminho_imagem
                    })]
            }));
    }

    public async Task AddManutencaoSolicitacaoAsync(SolicitacaoManutencaoDTO modelDTO)
    {
        using var connection = new NpgsqlConnection(_dataBaseSettings.ConnectionString);
        var model = new OperacionalSolicitacaoManutencaoModel
        {
            id = modelDTO.Id,
            id_programacao = modelDTO.IdProgramacao,
            item = modelDTO.Item,
            tipo = modelDTO.Tipo,
            solicitacao = modelDTO.Solicitacao
        };

        if (model.id <= 0)
        {
            model.id = await connection.ExecuteScalarAsync<int>(@"
                INSERT INTO operacional.tbl_solicitacao_manutencao
                (id_programacao, item, tipo, solicitacao)
                VALUES (@id_programacao, @item, @tipo, @solicitacao)
                RETURNING id;",
                model);

            modelDTO.Id = model.id;
        }
        else
        {
            var linhas = await connection.ExecuteAsync(@"
                UPDATE operacional.tbl_solicitacao_manutencao
                SET id_programacao = @id_programacao,
                    item = @item,
                    tipo = @tipo,
                    solicitacao = @solicitacao
                WHERE id = @id;",
                model);

            if (linhas != 1)
            {
                throw new InvalidOperationException("O registro nao existe mais ou foi alterado por outro usuario. Recarregue a tela; nenhum novo registro foi criado.");
            }
        }

        await LoadManutencaoSolicitacaoAsync(modelDTO.IdProgramacao);
    }

    public async Task AddManutencaoSolicitacaoFotoAsync(SolicitacaoManutencaoFotoDTO modelDTO, int IdProgramacao)
    {
        using var connection = new NpgsqlConnection(_dataBaseSettings.ConnectionString);
        var model = new OperacionalSolicitacaoManutencaoFotoModel { id_solicitacao = modelDTO.IdSolicitacao, caminho_imagem = modelDTO.CaminhoImagem };
        if (modelDTO.Id <= 0)
        {
            modelDTO.Id = await connection.ExecuteScalarAsync<int>(@"
                INSERT INTO operacional.tbl_solicitacao_manutencao_foto
                (id_solicitacao, caminho_imagem)
                VALUES (@id_solicitacao, @caminho_imagem)
                RETURNING id;",
                model);
        }
        else
        {
            model.id = modelDTO.Id;
            var linhas = await connection.ExecuteAsync(@"
                UPDATE operacional.tbl_solicitacao_manutencao_foto
                SET id_solicitacao = @id_solicitacao,
                    caminho_imagem = @caminho_imagem
                WHERE id = @id;",
                model);

            if (linhas != 1)
            {
                throw new InvalidOperationException("O registro nao existe mais ou foi alterado por outro usuario. Recarregue a tela; nenhum novo registro foi criado.");
            }
        }

        await LoadManutencaoSolicitacaoAsync(IdProgramacao);
    }

}
