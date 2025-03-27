using Microsoft.EntityFrameworkCore;
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
                MessageBox.Show(ex.InnerException.Message);
                Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
            }
        }

        private async void RadGridView_RowValidating(object sender, Telerik.Windows.Controls.GridViewRowValidatingEventArgs e)
        {
            try
            {

                TransporteMontagemViewModel vm = (TransporteMontagemViewModel)DataContext;
                if (!e.Row.IsInEditMode)
                    return;

                if (e.Row.Item is QryTransporteDTO item)
                {
                    //MessageBox.Show($"Linha alterada: {item.SiglaServ}, {item.numero_de_caminhoes}");
                    var novoTransporte = new TransporteMontagemModel
                    {
                        SiglaServ = item.SiglaServ,
                        DataDeExpedicao = item.data_de_expedicao,
                        VolumeDaCarga = item.volume_da_carga,
                        NumeroDeCaminhoes = item.numero_de_caminhoes,
                        Transportadora = item.transportadora,
                    };
                    bool sucesso = await vm.AtualizarTransporteMontagem(novoTransporte);
                    if (sucesso == false)
                    {
                        e.IsValid = false; // Impede que a linha seja confirmada
                        MessageBox.Show("Erro ao salvar no banco! Verifique os dados.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    else
                    {
                        if (item.numero_de_caminhoes != e.OldValues["numero_de_caminhoes"] as int?)
                        {
                            await vm.SincronizarCaminhoes(item.SiglaServ, item.numero_de_caminhoes, item.data_de_expedicao);
                            item.Cargas = await vm.CaminhoesSigla(item.SiglaServ);
                            radGridView.Rebind(); // Recarrega todos os dados da grade

                        }

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

        private async void RadGridViewFilho_RowValidating(object sender, GridViewRowValidatingEventArgs e)
        {
            try
            {
                TransporteMontagemViewModel vm = (TransporteMontagemViewModel)DataContext;
                if (!e.Row.IsInEditMode)
                    return;
                if (e.Row.Item is QryCargaMontagemDTO c) //tbl_cargas_montagem
                {
                    var carga = new tbl_cargas_montagem 
                    {
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
                    bool sucesso = await vm.UpsertcargaMontagem(carga);
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

    class TransporteMontagemViewModel : INotifyPropertyChanged
    {

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
                using Context context = new();
                // Carrega a lista principal
                var qryList = await context.qryfrmtransps.ToListAsync();
                // Carrega a lista de cargas relacionadas
                var cargasList = await context.cargasmontagens.ToListAsync();
                // Associa os dados manualmente

                // Método auxiliar para mapear cargas
                ObservableCollection<QryCargaMontagemDTO> MapearCargas(IEnumerable<tbl_cargas_montagem> cargas, string siglaServ)
                {
                    return new ObservableCollection<QryCargaMontagemDTO>(
                        cargas.Where(c => c.siglaserv == siglaServ)
                              .OrderBy(c => c.num_caminhao)
                              .Select(c => new QryCargaMontagemDTO
                              {
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
                using Context context = new();
                var transporteExistente = await context.transporteMontagens.FindAsync(transporteAtualizado.SiglaServ);
                if (transporteExistente == null)
                return false; // Registro não encontrado
                // Atualiza apenas os campos que foram modificados
                context.Entry(transporteExistente).CurrentValues.SetValues(transporteAtualizado);
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

        public async Task SincronizarCaminhoes(string siglaServ, int totalCaminhoes, DateTime? data)
        {
            try
            {
                using Context context = new();
                // Obtém os caminhões já cadastrados
                var caminhõesExistentes = await context.cargasmontagens
                    .Where(c => c.siglaserv == siglaServ)
                    .OrderBy(c => c.num_caminhao)
                    .ToListAsync();

                int caminhõesAtuais = caminhõesExistentes.Count;

                // **Se precisar adicionar caminhões**
                if (caminhõesAtuais < totalCaminhoes)
                {
                    for (int i = caminhõesAtuais + 1; i <= totalCaminhoes; i++)
                    {
                        var novoCaminhao = new tbl_cargas_montagem
                        {
                            siglaserv = siglaServ,
                            num_caminhao = i.ToString().PadLeft(2, '0'), // Formato "01", "02", etc.
                            data = data.Value.AddDays(i-1),
                            placa_caminhao = null // Ou alguma lógica para definir a placa
                        };
                        await context.cargasmontagens.AddAsync(novoCaminhao);
                    }
                }
                // **Se precisar remover caminhões excedentes**
                else if (caminhõesAtuais > totalCaminhoes)
                {
                    var caminhõesParaRemover = caminhõesExistentes.Skip(totalCaminhoes).ToList();
                    context.cargasmontagens.RemoveRange(caminhõesParaRemover);
                }

                // **Salvar as alterações no banco**
                await context.SaveChangesAsync();
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
                using Context context = new();
                // Obtém os caminhões já cadastrados
                var caminhoesExistentes = await context.cargasmontagens
                    .Where(c => c.siglaserv == siglaServ)
                    .OrderBy(c => c.num_caminhao)
                    .Select(c => new QryCargaMontagemDTO
                    {
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
                        obs_frete_contratado = c.obs_frete_contratado,
                    })
                    .ToListAsync();
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
                using Context context = new();
                var cargaExistente = await context.cargasmontagens.FindAsync(cargaMontagem.siglaserv, cargaMontagem.num_caminhao);

                if (cargaExistente == null)
                {
                    // Registro não existe, então insere um novo
                    await context.cargasmontagens.AddAsync(cargaMontagem);
                }
                else
                {
                    // Atualiza apenas os valores modificados
                    context.Entry(cargaExistente).CurrentValues.SetValues(cargaMontagem);
                }
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
