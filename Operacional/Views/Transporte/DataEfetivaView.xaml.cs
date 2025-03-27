using Microsoft.EntityFrameworkCore;
using Npgsql;
using Operacional.DataBase.Models;
using Operacional.DataBase.Models.DTOs;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.Common;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Operacional.Views.Transporte
{
    /// <summary>
    /// Interação lógica para DataEfetivaView.xam
    /// </summary>
    public partial class DataEfetivaView : UserControl
    {
        public DataEfetivaView()
        {
            InitializeComponent();
            DataContext = new DataEfetivaViewModel();
        }

        private async void UserControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            DataEfetivaViewModel vm = (DataEfetivaViewModel)DataContext;
            try
            {
                Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = Cursors.Wait; });
                await vm.InserirDataEfetivaAsync();
                vm.DatasEfetiva = await vm.GetDataEfetivaAsync();
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

        private async void radGridView_RowValidating(object sender, Telerik.Windows.Controls.GridViewRowValidatingEventArgs e)
        {
            try
            {

                DataEfetivaViewModel vm = (DataEfetivaViewModel)DataContext;
                if (!e.Row.IsInEditMode)
                    return;

                if (e.Row.Item is QryDataEfetivaModel item)
                {
                    //MessageBox.Show($"Linha alterada: {item.SiglaServ}, {item.numero_de_caminhoes}");
                    var dataEfetiva = new DataEfetivaModel
                    {
                        siglaserv = item.siglaserv,
                        prazotransportecliente = item.prazotransportecliente,
                        data_inicio_montagem = item.data_inicio_montagem,
                        data_inauguracao = item.data_inauguracao,
                        data_termino_montagem = item.data_termino_montagem,
                        data_combinada_mo_inicio = item.data_combinada_mo_inicio,
                        data_combinada_mo_fim = item.data_combinada_mo_fim,
                        data_informada_cliente = item.data_informada_cliente,
                        obs_data_inicio_montagem = item.obs_data_inicio_montagem,
                        data_inicio_desmontagem = item.data_inicio_desmontagem,
                        obs_data_termino_montagem = item.obs_data_termino_montagem,
                        data_final_desmontagem =item.data_final_desmontagem,
                        obs_desmontagem = item.obs_desmontagem,
                        data_libera_area_desmontagem = item.data_libera_area_desmontagem
                    };
                    bool sucesso = await vm.AtualizarDataEfetiva(dataEfetiva);
                    if (sucesso == false)
                    {
                        e.IsValid = false; // Impede que a linha seja confirmada
                        MessageBox.Show("Erro ao salvar no banco! Verifique os dados.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }

            }
            catch (DbUpdateException ex)
            {
                e.IsValid = false;
                MessageBox.Show($"Erro: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                //MessageBox.Show(ex.InnerException.Message);
            }
        }
    }

    class DataEfetivaViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void RaisePropertyChanged(string propName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));

        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        private ObservableCollection<QryDataEfetivaModel> datasEfetiva;
        public ObservableCollection<QryDataEfetivaModel> DatasEfetiva
        {
            get => datasEfetiva;
            //set { datasEfetiva = value; RaisePropertyChanged("DatasEfetiva"); }
            set { datasEfetiva = value; OnPropertyChanged(nameof(DatasEfetiva)); }
        }


        public async Task InserirDataEfetivaAsync()
        {
            try
            {
                using Context db = new();

                await db.Database.ExecuteSqlRawAsync(@"
                    INSERT INTO operacional.t_data_efetiva (siglaserv, data_inicio_montagem, prazotransportecliente)
                    SELECT c.siglaserv, '2025-12-25', 0
                    FROM operacional.t_transportes_mont c
                    LEFT JOIN operacional.t_data_efetiva d 
                        ON c.siglaserv = d.siglaserv
                    WHERE d.siglaserv IS NULL;
                ");
            }
            catch (DbUpdateException)
            {
                throw;
            }
            catch (Exception ex)  // Para qualquer outro erro
            {
                throw new Exception("Erro inesperado.", ex);
            }
        }

        public async Task<ObservableCollection<QryDataEfetivaModel>> GetDataEfetivaAsync()
        {
            using Context context = new();
            try
            {
                var retorno = await context.QryDatasEfetiva.AsNoTracking().ToListAsync();
                return new ObservableCollection<QryDataEfetivaModel>(retorno);
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

        public async Task<bool> AtualizarDataEfetiva(DataEfetivaModel dataEfetiva)
        {
            try
            {
                using Context context = new();
                var dataEfetivaExistente = await context.DatasEfetiva.FindAsync(dataEfetiva.siglaserv);
                if (dataEfetivaExistente == null)
                    return false; // Registro não encontrado
                // Atualiza apenas os campos que foram modificados
                context.Entry(dataEfetivaExistente).CurrentValues.SetValues(dataEfetiva);
                // Salva as mudanças no banco de dados
                await context.SaveChangesAsync();

                return true;
            }
            catch (DbUpdateException)
            {
                throw;
            }
            catch (Exception ex)  // Para qualquer outro erro
            {
                throw new Exception("Erro inesperado.", ex);
            }
        }
    }
}
