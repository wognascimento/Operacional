using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.EntityFrameworkCore;
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
            MessageBox.Show(ex.Message, "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
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
            MessageBox.Show(ex.Message, "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

}

public partial class RelatorioNoturnoDiarioViewModel : ObservableObject
{
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
        using Context context = new();
        var list = await context.ProducaoAprovados
            .OrderBy(x => x.nome)
            .ToListAsync();
        Aprovados = new ObservableCollection<ProducaoAprovadoModel>(list);
    }

    public async Task LoadRelatorios()
    {
        using Context context = new();
        var list = await context.OperacionalRelatorioNoturnos
            .OrderByDescending(x => x.data)
            .ThenBy(x => x.sigla)
            .ToListAsync();
        Relatorios = new ObservableCollection<OperacionalRelatorioNoturnoModel>(list);
    }

    public async Task LoadDeptos()
    {
        using Context context = new();
        var list = await context.OperacionalRelatorioNoturnoDptos
            .OrderBy(x => x.depto)
            .ToListAsync();
        Deptos = new ObservableCollection<OperacionalRelatorioNoturnoDeptoModel>(list);
    }

    public async Task AtualizarRelatorioAsync(OperacionalRelatorioNoturnoModel model)
    {
        using var db = new Context();
        var modelExistente = await db.OperacionalRelatorioNoturnos.FindAsync(model.cod_relatorio_noturno);
        if (modelExistente == null)
            await db.OperacionalRelatorioNoturnos.AddAsync(model);
        else
            db.Entry(modelExistente).CurrentValues.SetValues(model);

        await db.SaveChangesAsync();
    }
}
