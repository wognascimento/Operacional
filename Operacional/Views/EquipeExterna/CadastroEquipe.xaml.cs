using CommunityToolkit.Mvvm.ComponentModel;
using Dapper;
using Npgsql;
using Operacional.DataBase;
using Operacional.DataBase.Models;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Operacional.Views.EquipeExterna;

/// <summary>
/// Interação lógica para CadastroEquipe.xam
/// </summary>
public partial class CadastroEquipe : UserControl
{
    public CadastroEquipe()
    {
        InitializeComponent();
        DataContext = new CadastroEquipeViewModel();
    }

    private async void UserControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
    {
        try
        {
            Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = Cursors.Wait; });
            CadastroEquipeViewModel vm = (CadastroEquipeViewModel)DataContext;
            vm.Equipes = await vm.GetEquipesAsync();
            Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException pgEx)
        {
            MessageBox.Show($"Erro do banco: {pgEx.MessageText}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
        }
        catch (Exception ex)
        {
            Operacional.ErrorDialog.Show(ex, "Erro inesperado");
            Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
        }
    }

    private void EquipeRowValidated(object sender, Telerik.Windows.Controls.GridViewRowValidatingEventArgs e)
    {
        if (e.Row?.IsInEditMode != true) return;
        if (e.Row.Item is not EquipeExternaEquipeModel item) return;
        if (string.IsNullOrWhiteSpace(item.equipe_e))
        {
            e.IsValid = false;
            e.ValidationResults.Add(new Telerik.Windows.Controls.GridViewCellValidationResult
            { ErrorMessage = "Preencha os campos obrigatorios." });
            return;
        }
        ValidatedGridSave.Save(sender, e, () => ((CadastroEquipeViewModel)DataContext).AddEquipeAsync(item));
    }
}

public partial class CadastroEquipeViewModel : ObservableObject
{
    private readonly DataBaseSettings _dataBaseSettings = DataBaseSettings.Instance;

    [ObservableProperty]
    private ObservableCollection<EquipeExternaEquipeModel> equipes;

    public async Task<ObservableCollection<EquipeExternaEquipeModel>> GetEquipesAsync()
    {
        using var connection = new NpgsqlConnection(_dataBaseSettings.ConnectionString);
        var result = await connection.QueryAsync<EquipeExternaEquipeModel>(
            @"SELECT *
              FROM equipe_externa.tblequipesext
              ORDER BY id;");

        return new ObservableCollection<EquipeExternaEquipeModel>(result);
    }

    public async Task<bool> AddEquipeAsync(EquipeExternaEquipeModel model)
    {
        using var connection = new NpgsqlConnection(_dataBaseSettings.ConnectionString);

        if (model.id <= 0)
        {
            model.id = await connection.ExecuteScalarAsync<long>(@"
                INSERT INTO equipe_externa.tblequipesext
                (equipe_e, nome, tipo, cidadereferencia, razaosocial, cgc, insc_estadual, insc_municipal,
                 endereco_comercial, bairro_comercial, cidade_comercial, estado_comercial, pais_comercial,
                 cep_comercial, ddi_comercial, ddd_comercial, tel_comercial, fax_comercial, email_comercial,
                 website_comercial, responsavel, profissao, numero_crea, local_crea, pgto_crea, ddd_celular,
                 tel_celular, ddd_residencia, tel_residencia, endereco_residencial, bairro_residencial,
                 cidade_residencial, estado_residencial, pais_residencial, cep_residencial, ddi_residencial,
                 internacional, valorlanche, valortransporte, usuario, senha, cnae1, cnae2, natureza_juridica,
                 cod_servico, simples, anexo_3, id_totvs, irrf, csrf, iss, ativo_manutencao)
                VALUES
                (@equipe_e, @nome, @tipo, @cidadereferencia, @razaosocial, @cgc, @insc_estadual, @insc_municipal,
                 @endereco_comercial, @bairro_comercial, @cidade_comercial, @estado_comercial, @pais_comercial,
                 @cep_comercial, @ddi_comercial, @ddd_comercial, @tel_comercial, @fax_comercial, @email_comercial,
                 @website_comercial, @responsavel, @profissao, @numero_crea, @local_crea, @pgto_crea, @ddd_celular,
                 @tel_celular, @ddd_residencia, @tel_residencia, @endereco_residencial, @bairro_residencial,
                 @cidade_residencial, @estado_residencial, @pais_residencial, @cep_residencial, @ddi_residencial,
                 @internacional, @valorlanche, @valortransporte, @usuario, @senha, @cnae1, @cnae2, @natureza_juridica,
                 @cod_servico, @simples, @anexo_3, @id_totvs, @irrf, @csrf, @iss, @ativo_manutencao)
                RETURNING id;",
                model);

            return true;
        }

        var linhas = await connection.ExecuteAsync(@"
            UPDATE equipe_externa.tblequipesext SET
                equipe_e = @equipe_e,
                nome = @nome,
                tipo = @tipo,
                cidadereferencia = @cidadereferencia,
                razaosocial = @razaosocial,
                cgc = @cgc,
                insc_estadual = @insc_estadual,
                insc_municipal = @insc_municipal,
                endereco_comercial = @endereco_comercial,
                bairro_comercial = @bairro_comercial,
                cidade_comercial = @cidade_comercial,
                estado_comercial = @estado_comercial,
                pais_comercial = @pais_comercial,
                cep_comercial = @cep_comercial,
                ddi_comercial = @ddi_comercial,
                ddd_comercial = @ddd_comercial,
                tel_comercial = @tel_comercial,
                fax_comercial = @fax_comercial,
                email_comercial = @email_comercial,
                website_comercial = @website_comercial,
                responsavel = @responsavel,
                profissao = @profissao,
                numero_crea = @numero_crea,
                local_crea = @local_crea,
                pgto_crea = @pgto_crea,
                ddd_celular = @ddd_celular,
                tel_celular = @tel_celular,
                ddd_residencia = @ddd_residencia,
                tel_residencia = @tel_residencia,
                endereco_residencial = @endereco_residencial,
                bairro_residencial = @bairro_residencial,
                cidade_residencial = @cidade_residencial,
                estado_residencial = @estado_residencial,
                pais_residencial = @pais_residencial,
                cep_residencial = @cep_residencial,
                ddi_residencial = @ddi_residencial,
                internacional = @internacional,
                valorlanche = @valorlanche,
                valortransporte = @valortransporte,
                usuario = @usuario,
                senha = @senha,
                cnae1 = @cnae1,
                cnae2 = @cnae2,
                natureza_juridica = @natureza_juridica,
                cod_servico = @cod_servico,
                simples = @simples,
                anexo_3 = @anexo_3,
                id_totvs = @id_totvs,
                irrf = @irrf,
                csrf = @csrf,
                iss = @iss,
                ativo_manutencao = @ativo_manutencao
            WHERE id = @id;",
            model);

        if (linhas != 1)
            {
                throw new InvalidOperationException("O registro nao existe mais ou foi alterado por outro usuario. Recarregue a tela; nenhum novo registro foi criado.");
            }

        return true;
    }
}
