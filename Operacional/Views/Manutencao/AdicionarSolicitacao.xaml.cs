using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
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
        using var context = new Context();

        var solicitacoes = await context.OperacionalSolicitacaoManutencoes
            .Where(x => x.id_programacao == idProgramacao)
            .ToListAsync();

        var fotos = await context.OperacionalSolicitacaoManutencaoFotos
            .Where(x => solicitacoes.Select(s => s.id).Contains(x.id_solicitacao))
            .ToListAsync();

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
        using var context = new Context();
        var model = new OperacionalSolicitacaoManutencaoModel
        {
            id = modelDTO.Id,
            id_programacao = modelDTO.IdProgramacao,
            item = modelDTO.Item,
            tipo = modelDTO.Tipo,
            solicitacao = modelDTO.Solicitacao
        };
        var modelExistente = await context.OperacionalSolicitacaoManutencoes.FindAsync(modelDTO.Id);
        if (modelExistente == null)
        {

            context.OperacionalSolicitacaoManutencoes.Add(model);
        }
        else
        {
            context.Entry(modelExistente).CurrentValues.SetValues(model);
        }
        await context.SaveChangesAsync();
        await LoadManutencaoSolicitacaoAsync(modelDTO.IdProgramacao);
    }

    public async Task AddManutencaoSolicitacaoFotoAsync(SolicitacaoManutencaoFotoDTO modelDTO, int IdProgramacao)
    {
        using var context = new Context();
        var model = new OperacionalSolicitacaoManutencaoFotoModel { id_solicitacao = modelDTO.IdSolicitacao, caminho_imagem = modelDTO.CaminhoImagem };
        var modelExistente = await context.OperacionalSolicitacaoManutencaoFotos.FindAsync(modelDTO.Id);
        if (modelExistente == null)
        {
            context.OperacionalSolicitacaoManutencaoFotos.Add(model);
        }
        else
        {
            context.Entry(modelExistente).CurrentValues.SetValues(model);
        }
        await context.SaveChangesAsync();
        await LoadManutencaoSolicitacaoAsync(IdProgramacao);
    }

}