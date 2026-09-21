using ClosedXML.Excel;
using Dapper;
using Npgsql;
using Operacional.DataBase;
using Operacional.Views;
using Operacional.Views.Cronograma;
using Operacional.Views.Despesa;
using Operacional.Views.Documentos;
using Operacional.Views.EquipeExterna;
using Operacional.Views.EquipeExterna.Consultas;
using Operacional.Views.Manutencao;
using Operacional.Views.Transporte;
using Operacional.Utils;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using Telerik.Windows.Controls;

namespace Operacional
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        DataBaseSettings BaseSettings = DataBaseSettings.Instance;

        public MainWindow()
        {
            InitializeComponent();

            //var appSettings = ConfigurationManager.GetSection("appSettings") as NameValueCollection;
            //if (appSettings[0].Length > 0)
            //   BaseSettings.Username = appSettings[0];

            txtUsername.Text = BaseSettings.Username;
            txtDataBase.Text = BaseSettings.Database;
        }


        private void OnAlterarUsuario(object sender, MouseButtonEventArgs e)
        {
            Login window = new();
            window.ShowDialog();

            try
            {
                BaseSettings.LoadFromConfiguration();
                txtUsername.Text = BaseSettings.Username;
            }
            catch (Exception ex)
            {
                Operacional.ErrorDialog.Show(ex, "Erro");
            }
        }

        private void Image_MouseDown(object sender, MouseButtonEventArgs e)
        {
            RadWindow.Prompt(new DialogParameters()
            {
                Header = "Ano Sistema",
                Content = "Alterar o Ano do Sistema",
                Closed = (object sender, WindowClosedEventArgs e) =>
                {
                    if (e.PromptResult != null)
                    {
                        BaseSettings.Database = e.PromptResult;
                        BaseSettings.RefreshConnectionString();
                        txtDataBase.Text = BaseSettings.Database;
                        documentGroup.Items.Clear();
                    }
                }
            });
        }

        public void adicionarFilho(object filho, string title, string name)
        {
            var doc = ExistDocumentInDocumentContainer(name);
            if (doc == null)
            {
                var content = (FrameworkElement?)filho;
                content.Name = name.ToLower();

                var pane = new RadPane
                {
                    Header = title,
                    Name = name.ToLower(),
                    Content = content,
                    CanFloat = false
                };

                documentGroup.Items.Add(pane);
                pane.IsSelected = true;
            }
            else
            {
                doc.IsSelected = true;
            }
        }

        private RadPane ExistDocumentInDocumentContainer(string name_)
        {
            foreach (RadPane element in documentGroup.Items)
            {
                if (name_.ToLower() == element.Name)
                {
                    return element;
                }
            }
            return null;
        }

        private void OnCadastroTransportadoraClick(object sender, RoutedEventArgs e)
        {
            adicionarFilho(new CadastroTransportadora(), "CADASTRO TRANSPORTADORA", "CADASTRO_TRANSPORTADORA");
        }

        private void OnCartaInicioMontagemClick(object sender, RoutedEventArgs e)
        {
            adicionarFilho(new Views.Cartas.CartaInicioMontagem(), "CARTA - INÍCIO MONTAGEM", "CARTA_INICIO_MONTAGEM");
        }

        private void OnCartaApoioMontagemClick(object sender, RoutedEventArgs e)
        {
            adicionarFilho(new Views.Cartas.CartaApoioMontagem(), "CARTA - APOIO MONTAGEM", "CARTA_APOIO_MONTAGEM");
        }

        private void OnTransporteMontagemClick(object sender, RoutedEventArgs e)
        {
            adicionarFilho(new TransporteMontagem(), "TRANSPORTES MONTAGEM", "TRANSPORTE_MONTAGEM");
        }

        private void OnTransporteDesmontagemClick(object sender, RoutedEventArgs e)
        {
            adicionarFilho(new TransporteDesmontagem(), "TRANSPORTES DESMONTAGEM", "TRANSPORTE_DESMONTAGEM");
        }

        private void OnCargaMontagemClick(object sender, RoutedEventArgs e)
        {
            adicionarFilho(new CargaMontagem(), "CARGAS MONTAGEM", "CARGAS_MONTAGEM");
        }


        private void OnCargaDesmontagemClick(object sender, RoutedEventArgs e)
        {
            adicionarFilho(new CargaDesmontagem(), "CARGAS DESMONTAGEM", "CARGAS_DESMONTAGEM");
        }


        private void OnDataEfetivaClick(object sender, RoutedEventArgs e)
        {
            adicionarFilho(new DataEfetivaView(), "DATA EFETIVA", "DATA_EFETIVA");
        }

        private async void OnQryCargaMontgemClick(object sender, RoutedEventArgs e)
        {
            try
            {
                Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = Cursors.Wait; });

                using var connection = new NpgsqlConnection(BaseSettings.ConnectionString);
                var retorno = (await connection.QueryAsync<QryfrmtranspDetalheModel>(
                    @"SELECT *
                      FROM operacional.qryfrmtransp_detalhe
                      ORDER BY data, siglaserv;")).ToList();
                

                var path = SistemaPathResolver.GetImpressosPath("QUERY_CARGAS_MONTAGEM.xlsx");
                ConsultaExcelExporter.Exportar(retorno, path, "QUERY CARGAS MONTAGEM");

                SistemaPathResolver.OpenFile(path);

                Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });

            }
            catch (DbUpdateException ex)
            {
                Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
                Operacional.ErrorDialog.Show(ex, "Erro");
            }
            catch (Exception ex)  // Para qualquer outro erro
            {
                Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
                Operacional.ErrorDialog.Show(ex, "Erro");
            }
        }

        private async void OnDataMontagemClick(object sender, RoutedEventArgs e)
        {
            try
            {


                string sql = @"
                    SELECT 
                        sigla, 
                        siglaserv, 
                        grupo, 
                        nome, 
                        cidade, 
                        post_data_alterado, 
                        data_de_expedicao, 
                        est, 
                        dsl_inicio_montagem, 
                        dsl_termino_montagem, 
                        fecha_data_montagem, 
                        data_inauguracao, 
                        data_informada_cliente, 
                        diarias_cronograma, 
                        data_pedido_cliente_inicio_montagem, 
                        data_pedido_cliente_termino_montagem, 
                        contrato_inicio_mont, 
                        contrato_final_mont, 
                        ano
	                FROM operacional.qry_datas_montagem;";

                Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = Cursors.Wait; });

                using var connection = new NpgsqlConnection(BaseSettings.ConnectionString);
                await connection.OpenAsync();

                var dataTable = new System.Data.DataTable();
                using (var command = new NpgsqlCommand(sql, connection))
                using (var dataAdapter = new NpgsqlDataAdapter(command))
                {
                    dataAdapter.Fill(dataTable);
                }

                await connection.CloseAsync();

                var path = SistemaPathResolver.GetImpressosPath("DATAS-MONTAGEM.xlsx");
                ConsultaExcelExporter.Exportar(dataTable, path, "DATAS MONTAGEM");

                SistemaPathResolver.OpenFile(path);

                Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });

            }
            catch (DbUpdateException ex)
            {
                Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
                Operacional.ErrorDialog.Show(ex, "Erro");
            }
            catch (Exception ex)  // Para qualquer outro erro
            {
                Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
                Operacional.ErrorDialog.Show(ex, "Erro");
            }
        }

        private async void OnQryCargaDesmontagemClick(object sender, RoutedEventArgs e)
        {
            try
            {
                using var conn = new NpgsqlConnection(BaseSettings.ConnectionString);
                var sql = @"SELECT * FROM operacional.qrytranspdesmont_detalhes ORDER BY data_chegada_shopping, sigla_serv";
                var lista = (await conn.QueryAsync<TranspDesmontDetalheModel>(sql)).ToList();
                var retorno = new ObservableCollection<TranspDesmontDetalheModel>(lista);

                var path = SistemaPathResolver.GetImpressosPath("QUERY_CARGAS_DESMONTAGEM.xlsx");
                ConsultaExcelExporter.Exportar(retorno, path, "QUERY CARGAS DESMONTAGEM");

                SistemaPathResolver.OpenFile(path);

                Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });

            }
            catch (DbUpdateException ex)
            {
                Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
                Operacional.ErrorDialog.Show(ex, "Erro");
            }
            catch (Exception ex)  // Para qualquer outro erro
            {
                Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
                Operacional.ErrorDialog.Show(ex, "Erro");
            }
        }

        private void OnOpenTiposRelatoriosClick(object sender, RoutedEventArgs e)
        {
            adicionarFilho(new TipoRelatorio(), "CADASTRO RELATÓRIOS", "CADASTRO_RELATORIOS");
        }

        private void OnOpenCadastroFuncionarioClick(object sender, RoutedEventArgs e)
        {
            adicionarFilho(new CadastroFuncionario(), "CADASTRO FUNCIONÁRIO", "CADASTRO_FUNCIONARIO");
        }

        private void OnOpenCustosClick(object sender, RoutedEventArgs e)
        {
            adicionarFilho(new Custo(), "CUSTOS OPERACIONAL", "CUSTO_OPERACIONAL");
        }

        private void OnOpenCadastroDespesaClick(object sender, RoutedEventArgs e)
        {
            adicionarFilho(new CadastroDespesa(), "CADASTRO DESPESAS", "CADASTRO_DESPESAS");
        }

        private void OnCronogramaClick(object sender, RoutedEventArgs e)
        {
            adicionarFilho(new Cronograma(), "CRONOGRAMA", "CRONOGRAMA");
        }

        private async void OnFuncoesCronogramaClick(object sender, RoutedEventArgs e)
        {
            try
            {
                using var connection = new NpgsqlConnection(BaseSettings.ConnectionString);
                var retorno = (await connection.QueryAsync<OperacionalNoitescronogPessoaFuncaoModel>(
                    @"SELECT *
                      FROM operacional.tblnoitescronog_qtd_pessoa_funcao
                      WHERE qtd_pessoas > 0
                      ORDER BY sigla, fase, funcao;")).ToList();

                var path = SistemaPathResolver.GetImpressosPath("QUERY_FUNCOES_CRONOGRAMA.xlsx");
                ConsultaExcelExporter.Exportar(retorno, path, "FUNCOES CRONOGRAMA");

                SistemaPathResolver.OpenFile(path);

                Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });

            }
            catch (DbUpdateException ex)
            {
                Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
                Operacional.ErrorDialog.Show(ex, "Erro");
            }
            catch (Exception ex)  // Para qualquer outro erro
            {
                Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
                Operacional.ErrorDialog.Show(ex, "Erro");
            }
        }

        private void OnOpenCadastroEquipesClick(object sender, RoutedEventArgs e)
        {
            adicionarFilho(new CadastroEquipe(), "CADASTRO DE EQUIPES", "CADASTRO_EQUIPES");
        }

        private void OnOpenCadastroOrcamentoClick(object sender, RoutedEventArgs e)
        {
            adicionarFilho(new CadastroOrcamento(), "CADASTRO DE ORÇAMENTO", "CADASTRO_ORCAMENTO");
        }

        private void OnOpenUsuarioClick(object sender, RoutedEventArgs e)
        {
            adicionarFilho(new CadastroUsuario(), "CADASTRO DE USUÁRIOS", "CADASTRO_USUARIOS");
        }

        private void OnContatoClick(object sender, RoutedEventArgs e)
        {
            adicionarFilho(new Contato(), "CONTATOS", "EQUIPE_EXTERNA_CONTATOS");
        }

        private async void OnRelatorioPrevisaoValoresClick(object sender, RoutedEventArgs e)
        {
            adicionarFilho(new ValoresCronograma(), "PREVISÃO VALORES CRONOGRAMA", "PREVISAO_VALORES_CRONOGRAMA");
            
            /*
            try
            {
                using var connection = new NpgsqlConnection(BaseSettings.ConnectionString);

                string sql = @"
                    SELECT  
                            sigla, qtd_pessoas, qtd_noites, equipe, 
                            fase, funcao, valor_ano_atual, valor_total, 
                            lanche, transporte, id_equipe, indice_pessoas_noite, 
                            razaosocial, vai_equipe
	                FROM equipe_externa.qry_previsao_valores_cronograma;
                ";

                var result = await connection.QueryAsync<PrevisaoValorCronogramaDTO>(sql);
                using ExcelEngine excelEngine = new();
                IApplication application = excelEngine.Excel;
                application.DefaultVersion = ExcelVersion.Xlsx;
                IWorkbook workbook = application.Workbooks.Create(1);
                IWorksheet worksheet = workbook.Worksheets[0];
                worksheet.ImportData(result, 1, 1, true);
                workbook.SaveAs(@$"{BaseSettings.CaminhoSistema}Impressos\PrevisaoValores.xlsx");
                Process.Start(new ProcessStartInfo(@$"{BaseSettings.CaminhoSistema}Impressos\PrevisaoValores.xlsx")
                {
                    UseShellExecute = true
                });
                Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
            }
            catch (PostgresException ex)
            {
                Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
                MessageBox.Show($"Erro do banco: {ex.MessageText}\nDetalhe: {ex.Detail}\nLocal: {ex.Where}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (NpgsqlException ex)
            {
                Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
                MessageBox.Show($"Erro do banco: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException pgEx)
            {
                Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
                MessageBox.Show($"Erro do banco: {pgEx.MessageText}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
                Operacional.ErrorDialog.Show(ex, "Erro inesperado");
            }
            */
        }

        private void OnComparacaoPrevisaoRealizadoClick(object sender, RoutedEventArgs e)
        {
            adicionarFilho(new ComparacaoPrevisarLancamento(), "COMPARAÇÃO PREVISÃO REALIZADO", "COMPARACAO_PREVISAO_REALIZADO");
        }

        private void OnOpenNotasPagamentoClick(object sender, RoutedEventArgs e)
        {
            adicionarFilho(new NotaPagamento(), "NOTAS PARA PAGAMENTO LANÇAMENTO", "NOTAS_PAGAMENTO_LANCAMENTO");
        }

        private void OnOpenNotasPagamentoConsultaClick(object sender, RoutedEventArgs e)
        {
            adicionarFilho(new NotaPagamentoResumo(0), "NOTAS PARA PAGAMENTO RESUMO", "NOTAS_PAGAMENTO_RESUMO");
        }

        private void OnOpenNotasDespesasClick(object sender, RoutedEventArgs e)
        {
            adicionarFilho(new NotaDespesa(), "NOTAS DE DESPESAS", "NOTAS_DESPESA");
        }

        private void OnOpenProgramacaoClick(object sender, RoutedEventArgs e)
        {
            adicionarFilho(new Programacao(), "PROGRAMAÇÃO MANUTENÇÃO", "PROGRAMACAO_MANUTENCAO");
        }

        private async void OnOpenConsultaGeralClick(object sender, RoutedEventArgs e)
        {
            try
            {


                string sql = @"
                    SELECT 
	                    tbl_programacao_manutencao.id, shopp, cidade, est, data, 
	                    tbl_programacao_manutencao.tipo, funcao, qtd, tbl_solicitacao_manutencao.tipo AS solicitado, 
	                    item, solicitacao, caminho_imagem, resp_atendimento, nome_equipe, qtde_pessoa, obs_retorno, 
	                    cadastrado_por, data_cadastro, alterado_por, data_alteracao 
                    FROM operacional.tbl_programacao_manutencao
                    LEFT JOIN operacional.tbl_pessoas_manutencao ON tbl_programacao_manutencao.id = tbl_pessoas_manutencao.id_programacao
                    LEFT JOIN operacional.tbl_solicitacao_manutencao ON tbl_programacao_manutencao.id = tbl_solicitacao_manutencao.id_programacao
                    LEFT JOIN operacional.tbl_solicitacao_manutencao_foto ON operacional.tbl_solicitacao_manutencao.id = tbl_solicitacao_manutencao_foto.id_solicitacao;
                    ";

                Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = Cursors.Wait; });

                using var connection = new NpgsqlConnection(BaseSettings.ConnectionString);
                await connection.OpenAsync();

                var dataTable = new System.Data.DataTable();
                using (var command = new NpgsqlCommand(sql, connection))
                using (var dataAdapter = new NpgsqlDataAdapter(command))
                {
                    dataAdapter.Fill(dataTable);
                }

                await connection.CloseAsync();

                /*
                using ExcelEngine excelEngine = new();
                IApplication application = excelEngine.Excel;
                application.DefaultVersion = ExcelVersion.Excel2016;

                // Create a workbook
                IWorkbook workbook = application.Workbooks.Create(1);
                IWorksheet worksheet = workbook.Worksheets[0];

                // Import the DataTable
                worksheet.ImportDataTable(dataTable, true, 1, 1);

                workbook.SaveAs(@$"{BaseSettings.CaminhoSistema}Impressos\CONSULTA-GERAL-MANUTENCAO.xlsx");

                Process.Start(new ProcessStartInfo(@$"{BaseSettings.CaminhoSistema}Impressos\CONSULTA-GERAL-MANUTENCAO.xlsx")
                {
                    UseShellExecute = true
                });
                */

                var path = SistemaPathResolver.GetImpressosPath("CONSULTA-GERAL-MANUTENCAO.xlsx");

                // Salva em background (ClosedXML é síncrono)
                await Task.Run(() =>
                {
                    ConsultaExcelExporter.Exportar(dataTable, path, "CONSULTA GERAL MANUTENCAO");
                });

                // abrir
                SistemaPathResolver.OpenFile(path);

                Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });

            }
            catch (DbUpdateException ex)
            {
                Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
                Operacional.ErrorDialog.Show(ex, "Erro");
            }
            catch (Exception ex)  // Para qualquer outro erro
            {
                Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
                Operacional.ErrorDialog.Show(ex, "Erro");
            }
        }

        private void OnRelatorioDiarioWebClick(object sender, RoutedEventArgs e)
        {
            adicionarFilho(new RelatorioDiarioWeb(), "RELATÓRIO DIÁRIO WEB", "RELATORIO_DIARIO_WEB");
        }

        private void OnRelatorioNoturnoDiarioClick(object sender, RoutedEventArgs e)
        {
            adicionarFilho(new RelatorioNoturnoDiario(), "RELATÓRIO NOTURNO DIÁRIO", "RELATORIO_NOTURNO_DIARIO");
        }

        private void OnOpenControleDocumentoClick(object sender, RoutedEventArgs e)
        {
            adicionarFilho(new ControleDocumento(), "CONTROLE DOCUMENTOS", "CONTROLE_DOCUMENTOS");
        }

        private async void OnControleDocumentoPreenchidosClick(object sender, RoutedEventArgs e)
        {
            try
            {
                string sql = @"
                    SELECT 
	                    cli.id,
	                    cli.sigla,
	                    doc.item,
	                    doc.quando_enviar,
	                    doc.responsavel_liberacao,
	                    doc.email_responsavel_liberacao,
	                    cli.fecha,
	                    cli.direcionado_resp,
	                    cli.direcionado_resp_por,
	                    cli.direcionado_resp_em	em_analise,
	                    cli.em_analise_por	em_analise_em,
	                    cli.concluido,
	                    cli.concluido_por,
	                    cli.concluido_em,
	                    cli.enviado,
	                    cli.enviado_por,
	                    cli.enviado_em
                    FROM operacional.tblcontrole_documento doc
                    JOIN operacional.tblcontrole_documento_cliente cli ON doc.id = cli.id_documento
                    ORDER BY cli.sigla, doc.quando_enviar, doc.responsavel_liberacao;
                ";

                Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = Cursors.Wait; });

                using var connection = new NpgsqlConnection(BaseSettings.ConnectionString);
                await connection.OpenAsync();

                var dataTable = new System.Data.DataTable();
                using (var command = new NpgsqlCommand(sql, connection))
                using (var dataAdapter = new NpgsqlDataAdapter(command))
                {
                    dataAdapter.Fill(dataTable);
                }

                await connection.CloseAsync();

                var path = SistemaPathResolver.GetImpressosPath("CONTROLE-DOCUMENTOS.xlsx");

                // Salva em background (ClosedXML é síncrono)
                await Task.Run(() =>
                {
                    ConsultaExcelExporter.Exportar(dataTable, path, "CONTROLE DOCUMENTOS");
                });

                // abrir
                SistemaPathResolver.OpenFile(path);

                Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });

            }
            catch (DbUpdateException ex)
            {
                Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
                Operacional.ErrorDialog.Show(ex, "Erro");
            }
            catch (Exception ex)  // Para qualquer outro erro
            {
                Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
                Operacional.ErrorDialog.Show(ex, "Erro");
            }
        }

    }
}
