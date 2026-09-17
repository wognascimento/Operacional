using CommunityToolkit.Mvvm.ComponentModel;
using Dapper;
using Npgsql;
using Operacional.DataBase;
using Operacional.DataBase.Models;
using Operacional.Views.Cronograma;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Telerik.Windows.Controls;
using Telerik.Windows.Data;

namespace Operacional.Views.EquipeExterna;

/// <summary>
/// Interação lógica para RelatorioNoturnoDiario.xam
/// </summary>
public partial class RelatorioNoturnoDiario : UserControl
{
    public RelatorioNoturnoDiario()
    {
        InitializeComponent();
        DataContext = new RelatorioNoturnoDiarioViewModel();
    }

    private async void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            RelatorioNoturnoDiarioViewModel vm = (RelatorioNoturnoDiarioViewModel)DataContext;
            vm.IsBusy = true;
            await vm.LoadAprovados();
            await vm.LoadRelatorios();
            await vm.LoadDeptos();
            //ApplyCurrentAndPreviousDateFilter();
            vm.IsBusy = false;
        }
        catch (Exception ex)
        {
            Operacional.ErrorDialog.Show(ex, "Erro");
        }
    }

    public void ApplyCurrentAndPreviousDateFilter()
    {
        DateTime currentDate = DateTime.Today;
        DateTime previousDate = currentDate.AddDays(-1);

        var column = radGridViewRelatorio.Columns["data"] as GridViewDataColumn;
        if (column != null)
        {
            column.ClearFilters();

            // Adicionar filtro para o intervalo de datas
            var compositeFilter = new CompositeFilterDescriptor
            {
                LogicalOperator = FilterCompositionLogicalOperator.Or
            };

            // Filtro para a data anterior
            var previousDateFilter = new FilterDescriptor
            {
                Member = "data",
                Operator = FilterOperator.IsEqualTo,
                Value = previousDate
            };

            // Filtro para a data atual
            var currentDateFilter = new FilterDescriptor
            {
                Member = "data",
                Operator = FilterOperator.IsEqualTo,
                Value = currentDate
            };

            compositeFilter.FilterDescriptors.Add(previousDateFilter);
            compositeFilter.FilterDescriptors.Add(currentDateFilter);

            radGridViewRelatorio.FilterDescriptors.Add(compositeFilter);
        }
    }


    private async void ReportRowValidated(object sender, Telerik.Windows.Controls.GridViewRowValidatedEventArgs e)
    {
        try
        {
            RelatorioNoturnoDiarioViewModel vm = (RelatorioNoturnoDiarioViewModel)DataContext;
            await vm.AtualizarRelatorioAsync((OperacionalRelatorioNoturnoModel)e.Row.DataContext);
        }
        catch (Exception ex)
        {
            Operacional.ErrorDialog.Show(ex, "Erro");
        }
    }

}

public partial class RelatorioNoturnoDiarioViewModel : ObservableObject
{
    private readonly DataBaseSettings _dataBaseSettings = DataBaseSettings.Instance;

    [ObservableProperty]
    private ObservableCollection<ProducaoAprovadoModel> aprovados;

    [ObservableProperty]
    private ObservableCollection<OperacionalRelatorioNoturnoModel> relatorios;

    [ObservableProperty]
    private ObservableCollection<OperacionalRelatorioNoturnoDeptoModel> deptos;

    [ObservableProperty]
    private ObservableCollection<string> urgencias = ["1", "2", "3"];

    [ObservableProperty]
    private bool isBusy;


    public async Task LoadAprovados()
    {
        using var connection = new NpgsqlConnection(_dataBaseSettings.ConnectionString);
        var list = await connection.QueryAsync<ProducaoAprovadoModel>(
            @"SELECT *
              FROM producao.t_aprovados
              ORDER BY nome;");

        Aprovados = new ObservableCollection<ProducaoAprovadoModel>(list);
    }

    public async Task LoadRelatorios()
    {
        using var connection = new NpgsqlConnection(_dataBaseSettings.ConnectionString);
        var list = await connection.QueryAsync<OperacionalRelatorioNoturnoModel>(
            @"SELECT *
              FROM operacional.tbl_relatorio_noturno
              ORDER BY data DESC, sigla;");

        Relatorios = new ObservableCollection<OperacionalRelatorioNoturnoModel>(list);
    }

    public async Task LoadDeptos()
    {
        using var connection = new NpgsqlConnection(_dataBaseSettings.ConnectionString);
        var list = await connection.QueryAsync<OperacionalRelatorioNoturnoDeptoModel>(
            @"SELECT *
              FROM operacional.tbl_relatorio_noturno_depto
              ORDER BY depto;");

        Deptos = new ObservableCollection<OperacionalRelatorioNoturnoDeptoModel>(list);
    }

    public async Task AtualizarRelatorioAsync(OperacionalRelatorioNoturnoModel model)
    {
        using var connection = new NpgsqlConnection(_dataBaseSettings.ConnectionString);

        if (model.cod_relatorio_noturno is null or <= 0)
        {
            model.cod_relatorio_noturno = await connection.ExecuteScalarAsync<long>(@"
                INSERT INTO operacional.tbl_relatorio_noturno
                (sigla, data, depto, detalhe, classificacao_detalhe, noite,
                 coordenador, grau_de_urgencia, retorno_informacao, inserido_por, inserido_em)
                VALUES
                (@sigla, @data, @depto, @detalhe, @classificacao_detalhe, @noite,
                 @coordenador, @grau_de_urgencia, @retorno_informacao, @inserido_por, @inserido_em)
                RETURNING cod_relatorio_noturno;",
                model);

            return;
        }

        var linhas = await connection.ExecuteAsync(@"
            UPDATE operacional.tbl_relatorio_noturno
            SET
                sigla = @sigla,
                data = @data,
                depto = @depto,
                detalhe = @detalhe,
                classificacao_detalhe = @classificacao_detalhe,
                noite = @noite,
                coordenador = @coordenador,
                grau_de_urgencia = @grau_de_urgencia,
                retorno_informacao = @retorno_informacao,
                inserido_por = @inserido_por,
                inserido_em = @inserido_em
            WHERE cod_relatorio_noturno = @cod_relatorio_noturno;",
            model);

        if (linhas != 1)
            {
                throw new InvalidOperationException("O registro nao existe mais ou foi alterado por outro usuario. Recarregue a tela; nenhum novo registro foi criado.");
            }
    }
}
