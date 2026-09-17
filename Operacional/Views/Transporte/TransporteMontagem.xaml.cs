using Dapper;
using Npgsql;
using Operacional.DataBase;
using Operacional.DataBase.Models;
using Operacional.DataBase.Models.DTOs;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Telerik.Windows.Controls;

namespace Operacional.Views
{
    /// <summary>
    /// Interação lógica para TransporteMontagem.xam
    /// </summary>
    public partial class TransporteMontagem : UserControl
    {
        public TransporteMontagem()
        {
            InitializeComponent();
            this.DataContext = new TransporteMontagemViewModel();
        }

        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = Cursors.Wait; });
                TransporteMontagemViewModel vm = (TransporteMontagemViewModel)DataContext;
                vm.Transportes = await vm.GetTransportesAsync();

                Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
            }
            catch (DbUpdateException ex)
            {
                Operacional.ErrorDialog.Show(ex, "Erro de banco de dados");
                Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
            }
        }

        private void RadGridView_RowValidating(object sender, GridViewRowValidatingEventArgs e)
        {
            if (e.Row?.IsInEditMode != true) return;
            if (e.Row.Item is not QryTransporteDTO item) return;
            if (item.numero_de_caminhoes < 0 || string.IsNullOrWhiteSpace(item.SiglaServ))
            { e.IsValid = false; return; }
            var vm = (TransporteMontagemViewModel)DataContext;
            ValidatedGridSave.Save(sender, e, async () =>
            {
                await vm.AtualizarTransporteMontagem(new TransporteMontagemModel
                {
                    SiglaServ = item.SiglaServ, DataDeExpedicao = item.data_de_expedicao,
                    VolumeDaCarga = item.volume_da_carga, NumeroDeCaminhoes = item.numero_de_caminhoes,
                    Transportadora = item.transportadora
                });
            }, async () =>
            {
                item.Cargas = await vm.CaminhoesSigla(item.SiglaServ);
                radGridView.Rebind();
            });
        }

        private void RadGridViewFilho_RowValidating(object sender, GridViewRowValidatingEventArgs e)
        {
            if (e.Row?.IsInEditMode != true) return;
            if (e.Row.Item is not QryCargaMontagemDTO c) return;
            if (string.IsNullOrWhiteSpace(c.siglaserv) || !int.TryParse(c.num_caminhao, out var numero) || numero <= 0)
            { e.IsValid = false; return; }
            ValidatedGridSave.Save(sender, e, async () =>
            {
                    var carga = new tbl_cargas_montagem 
                    {
                        id = c.id,
                        siglaserv = c.siglaserv,
                        data = c.data,
                        num_caminhao = c.num_caminhao,
                        placa_caminhao = c.placa_caminhao,
                        m3_contratado = c.m3_contratado,
                        m3_utilizado = c.m3_utilizado,
                        hora_saida = c.hora_saida,
                        obs = c.obs,
                        local_carga = c.local_carga,
                        obscarga = c.obscarga,
                        trasnportadora = c.trasnportadora,
                        veiculo_programado = c.veiculo_programado,
                        data_chegada = c.data_chegada,
                        data_chegada_efetiva = c.data_chegada_efetiva,
                        obs_saida = c.obs_saida,
                        valor_frete_contratado_caminhao = c.valor_frete_contratado_caminhao,
                        noite_montagem = c.noite_montagem,
                        obs_externas = c.obs_externas,
                        obs_frete_contratado = c.obs_frete_contratado
                    };

                await ((TransporteMontagemViewModel)DataContext).UpsertcargaMontagem(carga);
                c.id = carga.id;
            });
        }
    }

    class TransporteMontagemViewModel : INotifyPropertyChanged
    {
        private readonly DataBaseSettings _dataBaseSettings = DataBaseSettings.Instance;

        public event PropertyChangedEventHandler PropertyChanged;
        public void RaisePropertyChanged(string propName) => 
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));

        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        private ObservableCollection<QryTransporteDTO>? transportes;
        public ObservableCollection<QryTransporteDTO> Transportes
        {
            get { return transportes; }
            //set { transportes = value; RaisePropertyChanged("Transportes"); }
            set { transportes = value; OnPropertyChanged(nameof(Transportes)); }
        }


        private QryTransporteDTO? transporte;
        public QryTransporteDTO Transporte
        {
            get { return transporte; }
            //set { transporte = value; RaisePropertyChanged("Transporte"); }
            set { transporte = value; OnPropertyChanged(nameof(Transporte)); }
        }


        public async Task<ObservableCollection<QryTransporteDTO>> GetTransportesAsync()
        {
            try
            {
                using var connection = new NpgsqlConnection(_dataBaseSettings.ConnectionString);
                var qryList = (await connection.QueryAsync<qryfrmtransp>("SELECT * FROM operacional.qryfrmtransp;")).ToList();
                var cargasList = (await connection.QueryAsync<tbl_cargas_montagem>("SELECT * FROM operacional.tbl_cargas_montagem;")).ToList();
                // Associa os dados manualmente

                // Método auxiliar para mapear cargas
                ObservableCollection<QryCargaMontagemDTO> MapearCargas(IEnumerable<tbl_cargas_montagem> cargas, string siglaServ)
                {
                    return new ObservableCollection<QryCargaMontagemDTO>(
                        cargas.Where(c => c.siglaserv == siglaServ)
                              .OrderBy(c => c.num_caminhao)
                              .Select(c => new QryCargaMontagemDTO
                              {
                                  id = c.id,
                                  siglaserv = c.siglaserv,
                                  data = c.data,
                                  num_caminhao = c.num_caminhao,
                                  placa_caminhao = c.placa_caminhao,
                                  m3_contratado = c.m3_contratado,
                                  m3_utilizado = c.m3_utilizado,
                                  hora_saida = c.hora_saida,
                                  obs = c.obs,
                                  local_carga = c.local_carga,
                                  obscarga = c.obscarga,
                                  trasnportadora = c.trasnportadora,
                                  veiculo_programado = c.veiculo_programado,
                                  data_chegada = c.data_chegada,
                                  data_chegada_efetiva = c.data_chegada_efetiva,
                                  obs_saida = c.obs_saida,
                                  valor_frete_contratado_caminhao = c.valor_frete_contratado_caminhao,
                                  noite_montagem = c.noite_montagem,
                                  obs_externas = c.obs_externas,
                                  obs_frete_contratado = c.obs_frete_contratado
                              })
                    );
                }

                // Mapeia a lista principal
                var resultado = new ObservableCollection<QryTransporteDTO>(
                    qryList.Select(q => new QryTransporteDTO
                    {
                        SiglaServ = q.siglaserv,
                        data_de_expedicao = q.data_de_expedicao,
                        cubagem_por_produto = q.cubagem_por_produto,
                        volume_da_carga = q.volume_da_carga,
                        cubagem_expedida = q.cubagem_expedida,
                        perc_shop = q.perc_shop,
                        volume_informado = q.volume_informado,
                        numero_de_caminhoes = q.numero_de_caminhoes,
                        distancia = q.distancia,
                        transporte = q.transporte,
                        cidade = q.cidade,
                        regiao = q.regiao,
                        Origem = q.origem,
                        transportadora = q.transportadora,
                        OK = q.ok,
                        valor_frete_contratado = q.valor_frete_contratado,
                        AlteradoPor = q.alteradopor,
                        DataAltera = q.dataaltera,
                        Cargas = MapearCargas(cargasList, q.siglaserv) // Utiliza a função auxiliar
                    }).ToList()
                );

                return resultado;
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


        public async Task<bool> AtualizarTransporteMontagem(TransporteMontagemModel transporteAtualizado)
        {
            try
            {
                using var connection = new NpgsqlConnection(_dataBaseSettings.ConnectionString);
                var sql = @"
                    UPDATE operacional.t_transportes_mont
                    SET
                        data_de_expedicao = @DataDeExpedicao,
                        volume_da_carga = @VolumeDaCarga,
                        numero_de_caminhoes = @NumeroDeCaminhoes,
                        transportadora = @Transportadora
                    WHERE siglaserv = @SiglaServ;";

                await connection.OpenAsync();
                await using var transaction = await connection.BeginTransactionAsync();
                if (await connection.ExecuteAsync(sql, transporteAtualizado, transaction) != 1)
                    throw new InvalidOperationException("Transporte nao encontrado. Recarregue a tela.");
                await SincronizarCaminhoes(transporteAtualizado.SiglaServ, transporteAtualizado.NumeroDeCaminhoes,
                    transporteAtualizado.DataDeExpedicao, connection, transaction);
                await transaction.CommitAsync();
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

        private async Task SincronizarCaminhoes(string siglaServ, int totalCaminhoes, DateTime? data, NpgsqlConnection connection, NpgsqlTransaction transaction)
        {
            try
            {
                if (totalCaminhoes < 0) throw new InvalidOperationException("Quantidade de caminhoes invalida.");

                var caminhõesExistentes = (await connection.QueryAsync<tbl_cargas_montagem>(
                    @"SELECT * FROM operacional.tbl_cargas_montagem
                      WHERE siglaserv = @siglaServ
                      ORDER BY num_caminhao FOR UPDATE;",
                    new { siglaServ },
                    transaction)).ToList();

                int caminhõesAtuais = caminhõesExistentes.Count;
                var numeros = caminhõesExistentes.Select(c => int.TryParse(c.num_caminhao, out var n) ? n : 0).ToHashSet();
                int proximoNumero = 1;

                // **Se precisar adicionar caminhões**
                if (caminhõesAtuais < totalCaminhoes)
                {
                    for (int i = caminhõesAtuais + 1; i <= totalCaminhoes; i++)
                    {
                        while (numeros.Contains(proximoNumero)) proximoNumero++;
                        numeros.Add(proximoNumero);
                        var novoCaminhao = new tbl_cargas_montagem
                        {
                            siglaserv = siglaServ,
                            num_caminhao = proximoNumero.ToString().PadLeft(2, '0'), // Formato "01", "02", etc.
                            data = data?.AddDays(i-1),
                            placa_caminhao = null // Ou alguma lógica para definir a placa
                        };

                        await connection.ExecuteAsync(
                            @"INSERT INTO operacional.tbl_cargas_montagem
                              (siglaserv, num_caminhao, data, placa_caminhao)
                              VALUES (@siglaserv, @num_caminhao, @data, @placa_caminhao);",
                            novoCaminhao,
                            transaction);
                    }
                }
                // **Se precisar remover caminhões excedentes**
                else if (caminhõesAtuais > totalCaminhoes)
                {
                    throw new InvalidOperationException("A reducao excluiria cargas existentes. Revise as cargas antes de reduzir a quantidade.");
                }


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

        public async Task<ObservableCollection<QryCargaMontagemDTO>> CaminhoesSigla(string siglaServ)
        {
            try
            {
                using var connection = new NpgsqlConnection(_dataBaseSettings.ConnectionString);
                var caminhoesExistentes = await connection.QueryAsync<QryCargaMontagemDTO>(
                    @"SELECT
                        id, siglaserv, data, num_caminhao, placa_caminhao, m3_contratado,
                        m3_utilizado, hora_saida, obs, local_carga, obscarga, trasnportadora,
                        veiculo_programado, data_chegada, data_chegada_efetiva, obs_saida,
                        valor_frete_contratado_caminhao, noite_montagem, obs_externas,
                        obs_frete_contratado
                      FROM operacional.tbl_cargas_montagem
                      WHERE siglaserv = @siglaServ
                      ORDER BY num_caminhao;",
                    new { siglaServ });
                return new ObservableCollection<QryCargaMontagemDTO>(caminhoesExistentes);
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


        public async Task<bool> UpsertcargaMontagem(tbl_cargas_montagem cargaMontagem)
        {
            try
            {
                using var connection = new NpgsqlConnection(_dataBaseSettings.ConnectionString);

                if (cargaMontagem.id <= 0)
                {
                    cargaMontagem.id = await connection.ExecuteScalarAsync<long>(@"
                        INSERT INTO operacional.tbl_cargas_montagem
                        (siglaserv, data, num_caminhao, placa_caminhao, m3_contratado, m3_utilizado,
                         hora_saida, obs, local_carga, obscarga, trasnportadora, veiculo_programado,
                         data_chegada, data_chegada_efetiva, obs_saida, valor_frete_contratado_caminhao,
                         noite_montagem, obs_externas, obs_frete_contratado)
                        VALUES
                        (@siglaserv, @data, @num_caminhao, @placa_caminhao, @m3_contratado, @m3_utilizado,
                         @hora_saida, @obs, @local_carga, @obscarga, @trasnportadora, @veiculo_programado,
                         @data_chegada, @data_chegada_efetiva, @obs_saida, @valor_frete_contratado_caminhao,
                         @noite_montagem, @obs_externas, @obs_frete_contratado)
                        RETURNING id;",
                        cargaMontagem);

                    return true;
                }

                var linhas = await connection.ExecuteAsync(@"
                    UPDATE operacional.tbl_cargas_montagem
                    SET
                        siglaserv = @siglaserv,
                        data = @data,
                        num_caminhao = @num_caminhao,
                        placa_caminhao = @placa_caminhao,
                        m3_contratado = @m3_contratado,
                        m3_utilizado = @m3_utilizado,
                        hora_saida = @hora_saida,
                        obs = @obs,
                        local_carga = @local_carga,
                        obscarga = @obscarga,
                        trasnportadora = @trasnportadora,
                        veiculo_programado = @veiculo_programado,
                        data_chegada = @data_chegada,
                        data_chegada_efetiva = @data_chegada_efetiva,
                        obs_saida = @obs_saida,
                        valor_frete_contratado_caminhao = @valor_frete_contratado_caminhao,
                        noite_montagem = @noite_montagem,
                        obs_externas = @obs_externas,
                        obs_frete_contratado = @obs_frete_contratado
                    WHERE id = @id;",
                    cargaMontagem);

                if (linhas != 1)
                    throw new InvalidOperationException("Carga nao encontrada. Recarregue a tela.");

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
