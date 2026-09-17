using Dapper;
using Npgsql;
using Operacional.DataBase;
using Operacional.DataBase.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.Common;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Operacional.Views.Despesa
{
    /// <summary>
    /// Interação lógica para TipoRelatorio.xam
    /// </summary>
    public partial class TipoRelatorio : UserControl
    {
        public TipoRelatorio()
        {
            InitializeComponent();
            DataContext = new TipoRelatorioViewModel();
        }

        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            TipoRelatorioViewModel vm = (TipoRelatorioViewModel)DataContext;
            try
            {
                Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = Cursors.Wait; });
                vm.DespsRelatorio = await vm.GetTiposRelatorioAsync();
                Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
            }
            catch (NpgsqlException ex)
            {
                Operacional.ErrorDialog.Show(ex, "Erro");
                Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
            }
            catch (Exception ex)
            {
                Operacional.ErrorDialog.Show(ex, "Erro");
                Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
            }
        }

        private void radGridView_RowValidating(object sender, Telerik.Windows.Controls.GridViewRowValidatingEventArgs e)
        {
            if (e.Row?.IsInEditMode != true) return;
            if (e.Row.Item is not OperacionalDespRelatorioModel item) return;
            if (string.IsNullOrWhiteSpace(item.descricao_relatorio))
            {
                e.IsValid = false;
                e.ValidationResults.Add(new Telerik.Windows.Controls.GridViewCellValidationResult
                { PropertyName = nameof(item.descricao_relatorio), ErrorMessage = "Informe a descricao." });
                return;
            }
            ValidatedGridSave.Save(sender, e, () => ((TipoRelatorioViewModel)DataContext).SalvarAsync(item));
        }
    }

    public partial class TipoRelatorioViewModel : INotifyPropertyChanged
    {
        private readonly DataBaseSettings _dataBaseSettings = DataBaseSettings.Instance;

        public event PropertyChangedEventHandler PropertyChanged;
        public void RaisePropertyChanged(string propName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        protected void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        private ObservableCollection<OperacionalDespRelatorioModel> despsRelatorio;
        public ObservableCollection<OperacionalDespRelatorioModel> DespsRelatorio
        {
            get => despsRelatorio;
            //set { despsRelatorio = value; RaisePropertyChanged("DespsRelatorio"); }
            set { despsRelatorio = value; OnPropertyChanged(nameof(DespsRelatorio)); }
        }

        public async Task SalvarAsync(OperacionalDespRelatorioModel item)
        {
            using var connection = new NpgsqlConnection(_dataBaseSettings.ConnectionString);
            if (item.cod_relatorio is null or <= 0)
                item.cod_relatorio = await connection.QuerySingleAsync<long>(
                    @"INSERT INTO operacional.t_desp_relatorios (descricao_relatorio)
                      VALUES (@descricao_relatorio) RETURNING cod_relatorio;", item);
            else if (await connection.ExecuteAsync(
                @"UPDATE operacional.t_desp_relatorios SET descricao_relatorio = @descricao_relatorio
                  WHERE cod_relatorio = @cod_relatorio;", item) != 1)
                throw new InvalidOperationException("Tipo de relatorio nao encontrado. Recarregue a tela.");
        }

        public async Task<ObservableCollection<OperacionalDespRelatorioModel>> GetTiposRelatorioAsync()
        {
            try
            {
                using var connection = new NpgsqlConnection(_dataBaseSettings.ConnectionString);
                var retorno = await connection.QueryAsync<OperacionalDespRelatorioModel>(
                    @"SELECT *
                      FROM operacional.t_desp_relatorios
                      ORDER BY descricao_relatorio;");

                return [.. retorno];
            }
            catch (DbException ex)  // Para erros de banco de dados
            {
                throw new Exception("Erro ao consultar os dados efetivos.", ex);
            }
            catch (Exception ex)  // Para qualquer outro erro
            {
                throw new Exception("Erro inesperado.", ex);
            }
        }
    }
}
