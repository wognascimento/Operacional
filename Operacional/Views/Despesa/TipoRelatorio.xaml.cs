using Microsoft.EntityFrameworkCore;
using Npgsql;
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
                MessageBox.Show(ex.Message);
                Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
            }
        }

        private void radGridView_RowValidating(object sender, Telerik.Windows.Controls.GridViewRowValidatingEventArgs e)
        {

        }
    }

    public partial class TipoRelatorioViewModel : INotifyPropertyChanged
    {
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

        public async Task<ObservableCollection<OperacionalDespRelatorioModel>> GetTiposRelatorioAsync()
        {
            using Context context = new();
            try
            {
                var retorno = await context.DespRelatorios.AsNoTracking().ToListAsync();
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