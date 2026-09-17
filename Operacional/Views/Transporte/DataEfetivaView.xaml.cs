using Dapper;
using Npgsql;
using Operacional.DataBase;
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
            if (e.Row.Item is not QryDataEfetivaModel item) return;
            ValidatedGridSave.Save(sender, e, async () =>
            {
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

                if (!await ((DataEfetivaViewModel)DataContext).AtualizarDataEfetiva(dataEfetiva))
                    throw new InvalidOperationException("Registro nao encontrado. Recarregue a tela.");
            });
        }
    }

    class DataEfetivaViewModel : INotifyPropertyChanged
    {
        private readonly DataBaseSettings _dataBaseSettings = DataBaseSettings.Instance;
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
                using var connection = new NpgsqlConnection(_dataBaseSettings.ConnectionString);
                await connection.ExecuteAsync(@"
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
            try
            {
                using var connection = new NpgsqlConnection(_dataBaseSettings.ConnectionString);
                var retorno = await connection.QueryAsync<QryDataEfetivaModel>(
                    "SELECT * FROM operacional.qry_data_efetiva;");
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
                using var connection = new NpgsqlConnection(_dataBaseSettings.ConnectionString);
                var sql = @"
                    UPDATE operacional.t_data_efetiva
                    SET
                        data_inicio_montagem = @data_inicio_montagem,
                        data_termino_montagem = @data_termino_montagem,
                        data_inauguracao = @data_inauguracao,
                        data_inicio_desmontagem = @data_inicio_desmontagem,
                        data_final_desmontagem = @data_final_desmontagem,
                        data_libera_area_desmontagem = @data_libera_area_desmontagem,
                        prazotransportecliente = @prazotransportecliente,
                        data_informada_cliente = @data_informada_cliente,
                        obs_data_inicio_montagem = @obs_data_inicio_montagem,
                        obs_data_termino_montagem = @obs_data_termino_montagem,
                        obs_desmontagem = @obs_desmontagem,
                        data_combinada_mo_inicio = @data_combinada_mo_inicio,
                        data_combinada_mo_fim = @data_combinada_mo_fim
                    WHERE siglaserv = @siglaserv;";

                return await connection.ExecuteAsync(sql, dataEfetiva) > 0;
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
