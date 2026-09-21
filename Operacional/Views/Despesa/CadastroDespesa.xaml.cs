using ClosedXML.Excel;
using Dapper;
using Npgsql;
using Operacional.DataBase;
using Operacional.Utils;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.Common;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Telerik.Windows.Controls;

namespace Operacional.Views.Despesa
{
    /// <summary>
    /// Interação lógica para CadastroDespesa.xam
    /// </summary>
    public partial class CadastroDespesa : UserControl
    {
        public CadastroDespesa()
        {
            InitializeComponent();
            this.DataContext = new CadastroDespesaViewModel();
        }

        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            CadastroDespesaViewModel vm = (CadastroDespesaViewModel)DataContext;
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                vm.BaseCustos = [.. await vm.GetBaseCustosAsync()];
                vm.DescricoesPorTipo = await vm.GetDescricoesAsync();
                RegistroDespesa.BuscarDescricoesExternamente = vm.ObterDescricoesPorTipo;

                vm.TiposClassificacao = [.. vm.BaseCustos.Select(b => b.tipo).Distinct()];
                vm.Funcionarios = await vm.GetFuncionariosAsync();
                vm.DespsRelatorio = await vm.GetTiposRelatorioAsync();
                vm.Clientes = await vm.GetClientesAsync();
                vm.Empresas = await vm.GetEmpresasAsync();
                vm.Fases = await vm.GetEtapasAsync();

                var relatorios = await vm.GetRelatoriosAsync();
                
                foreach (var relatorio in relatorios)
                {
                    if (relatorio.RelatorioObservacao.Count == 0)
                    {
                        relatorio.RelatorioObservacao.Add(new OperacionalRelatorioObservacaoModel
                        {
                            cod_relatorio = relatorio.cod_relatorio
                        });
                    }

                    if (relatorio.RelatorioAdiantamento.Count == 0)
                    {
                        relatorio.RelatorioAdiantamento.Add(new OperacionalAdiantamentoModel
                        {
                            cod_relatorio = relatorio.cod_relatorio
                        });
                    }

                    /*
                    var pai = relatorio.RelatorioObservacao
                        .Select(x =>
                        {
                            var r = new RegistroDespesa(relatorio);
                            return r;
                        })
                        .ToList();

                    var registros = relatorio.RelatorioDespesaDetalhes
                        .Select(x =>
                        {
                            Debug.WriteLine($"Criando RegistroDespesa para linha: {x.cod_linha_detalhe}");
                            var r = new RegistroDespesa(x, relatorio)
                            {
                                DescricoesDisponiveis = [.. vm.ObterDescricoesPorTipo(x.classificacao) ?? []]
                            };
                            return r;
                        })
                        .ToList();

                    relatorio.RelatorioDespesaDetalhesEditaveis = [.. registros];
                    */
                    
                    var registros = relatorio.RelatorioDespesaDetalhes.Any()
                        ? relatorio.RelatorioDespesaDetalhes.OrderBy(x => x.documento)
                        .Select(x =>
                        {
                            Debug.WriteLine($"Criando RegistroDespesa para linha: {x.cod_linha_detalhe}");
                            AdicionarOpcaoAusente(vm.TiposClassificacao, x.classificacao);
                            AdicionarOpcaoAusente(vm.Fases, x.etapa);

                            var descricoes = vm.ObterDescricoesPorTipo(x.classificacao);
                            AdicionarOpcaoAusente(descricoes, x.descricao);
                            var r = new RegistroDespesa(x, relatorio)
                            {
                                DescricoesDisponiveis = [.. descricoes]
                            };
                            return r;
                        })
                        .ToList() : [ new RegistroDespesa(relatorio) ];
                    
                    relatorio.RelatorioDespesaDetalhesEditaveis = [.. registros];
                    
                }

                // Atualiza a propriedade no ViewModel
                vm.Relatorios = relatorios;
                Mouse.OverrideCursor = null;
            }
            catch (DbUpdateException ex)
            {
                Operacional.ErrorDialog.Show(ex, "Erro de banco de dados");
                Mouse.OverrideCursor = null;
            }
        }

        private static void AdicionarOpcaoAusente(ICollection<string> opcoes, string? valor)
        {
            if (!string.IsNullOrWhiteSpace(valor) &&
                !opcoes.Contains(valor, StringComparer.OrdinalIgnoreCase))
            {
                opcoes.Add(valor);
            }
        }

        private async void dGRelatorio_CellEditEnded(object sender, GridViewCellEditEndedEventArgs e)
        {
            CadastroDespesaViewModel vm = (CadastroDespesaViewModel)DataContext;
            var registro = e.Cell?.DataContext as OperacionalRelatorioDespesaModel;

            try
            {
                if (e.Cell?.Column?.UniqueName == "codigo_funcionario" && registro is not null)
                {
                    var valueSelecionado = vm.Funcionarios?.FirstOrDefault(f => f.cod_func == registro.codigo_funcionario);
                    if (valueSelecionado is null)
                        return;

                    await using var connection = new NpgsqlConnection(DataBaseSettings.Instance.ConnectionString);
                    var empresa = await connection.QueryFirstOrDefaultAsync<int?>(
                        @"SELECT totvs
                          FROM compras.tblempresa
                          WHERE abreviacao = @empresa
                          LIMIT 1;",
                        new { empresa = valueSelecionado.empresa });

                    registro.codigo_empresa = empresa;
                    dGRelatorio.Rebind();
                }
            }
            catch (DbUpdateException ex)
            {
                Operacional.ErrorDialog.Show(ex, "Erro de banco de dados");
            }

        }

        private void dGRelatorio_RowValidating(object sender, GridViewRowValidatingEventArgs e)
        {
            if (e.Row?.IsInEditMode != true) return;
            if (e.Row.Item is not OperacionalRelatorioDespesaModel item) return;
            
            if (!ValidarRelatorio(item, e)) return;
            ValidatedGridSave.Save(sender, e, async () =>
            {
                await ((CadastroDespesaViewModel)DataContext).AdcionarRelatorio(item);
                item.RelatorioObservacao ??= [];
                item.RelatorioAdiantamento ??= [];
                if (item.RelatorioObservacao.Count == 0)
                    item.RelatorioObservacao.Add(new() { cod_relatorio = item.cod_relatorio });
                if (item.RelatorioAdiantamento.Count == 0)
                    item.RelatorioAdiantamento.Add(new() { cod_relatorio = item.cod_relatorio });

            });
        }

        private void dGRelatorioObservacao_RowValidating(object sender, GridViewRowValidatingEventArgs e)
        {
            if (e.Row?.IsInEditMode != true) return;
            if (e.Row.Item is not OperacionalRelatorioObservacaoModel item) return;
            var pai = (sender as FrameworkElement)?.DataContext as OperacionalRelatorioDespesaModel;
            if (pai == null || pai.cod_relatorio <= 0) { e.IsValid = false; return; }
            item.cod_relatorio = pai.cod_relatorio;
            if (!ValidarObservacao(item, e)) return;
            ValidatedGridSave.Save(sender, e, async () =>
            {
                await ((CadastroDespesaViewModel)DataContext).AdcionarRelatorioObservacao(item);
            });
        }

        private void dGRelatorioAdiantamento_RowValidating(object sender, GridViewRowValidatingEventArgs e)
        {
            if (e.Row?.IsInEditMode != true) return;
            if (e.Row.Item is not OperacionalAdiantamentoModel item) return;
            var pai = (sender as FrameworkElement)?.DataContext as OperacionalRelatorioDespesaModel;
            if (pai == null || pai.cod_relatorio <= 0) { e.IsValid = false; return; }
            item.cod_relatorio = pai.cod_relatorio;
            if (!ValidarAdiantamento(item, e)) return;
            ValidatedGridSave.Save(sender, e, async () =>
            {
                await ((CadastroDespesaViewModel)DataContext).AdcionarRelatorioAdiantamento(item);
            });
        }

        private void dGRelatorioDetalhes_RowValidating(object sender, GridViewRowValidatingEventArgs e)
        {
            if (e.Row?.IsInEditMode != true) return;
            if (e.Row.Item is not RegistroDespesa item) return;
            var pai = item.RelatorioPai ?? (sender as FrameworkElement)?.DataContext as OperacionalRelatorioDespesaModel;
            if (pai == null || pai.cod_relatorio <= 0) { e.IsValid = false; return; }
            item.RelatorioPai = pai;
            item.cod_relatorio = pai.cod_relatorio;
            if (!ValidarDetalhe(item, e)) return;
            ValidatedGridSave.Save(sender, e, async () =>
            {
                var model = item.ToModel();
                await ((CadastroDespesaViewModel)DataContext).AdcionarRelatorioDetalhes(model);
                item.cod_linha_detalhe = model.cod_linha_detalhe;
            });
        }

        private void OnImprimirRelatorioClick(object sender, Telerik.Windows.RadRoutedEventArgs e)
        {
            if (dGRelatorio.SelectedItem is not OperacionalRelatorioDespesaModel record)
            {
                MessageBox.Show("Selecione um relatório para imprimir.", "Imprimir", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            CadastroDespesaRelatorioPrinter.Imprimir(record, (CadastroDespesaViewModel)DataContext);
        }

        private static bool ValidarRelatorio(OperacionalRelatorioDespesaModel relatorio, GridViewRowValidatingEventArgs e)
        {
            var campos = new List<string>();

            AddIfMissing(campos, relatorio.codigo_funcionario, "Funcionário");
            AddIfMissing(campos, relatorio.nome_relatorio, "Relatório");
            AddIfMissing(campos, relatorio.localidade, "Localidade");
            AddIfMissing(campos, relatorio.data, "Data");
            AddIfMissing(campos, relatorio.classif_financeiro, "Financeiro");
            AddIfMissing(campos, relatorio.codigo_empresa, "Empresa");

            return ValidarCamposObrigatorios(campos, e);
        }

        private static bool ValidarObservacao(OperacionalRelatorioObservacaoModel observacao, GridViewRowValidatingEventArgs e)
        {
            var campos = new List<string>();
            AddIfMissing(campos, observacao.observacao, "Observação");
            return ValidarCamposObrigatorios(campos, e);
        }

        private static bool ValidarAdiantamento(OperacionalAdiantamentoModel adiantamento, GridViewRowValidatingEventArgs e)
        {
            var campos = new List<string>();

            AddIfMissing(campos, adiantamento.valor_adiantamento, "Valor adiantamento");
            AddIfMissing(campos, adiantamento.valor_real_peso, "Valor do peso");
            AddIfMissing(campos, adiantamento.valor_cotacao_peso, "Cotação peso");
            AddIfMissing(campos, adiantamento.valor_peso_peso, "Valor peso");
            AddIfMissing(campos, adiantamento.valor_real_dolar, "Valor do US$");
            AddIfMissing(campos, adiantamento.valor_cotacao_dolar, "Cotação US$");
            AddIfMissing(campos, adiantamento.valor_dolar_dolar, "Valor US$");
            AddIfMissing(campos, adiantamento.total_adiantamento, "Total adiantamento");
            AddIfMissing(campos, adiantamento.data_pagamento, "Data pagamento");
            AddIfMissing(campos, adiantamento.forma_pagto, "Forma pagamento");

            return ValidarCamposObrigatorios(campos, e);
        }

        private static bool ValidarDetalhe(RegistroDespesa detalhe, GridViewRowValidatingEventArgs e)
        {
            var campos = new List<string>();

            AddIfMissing(campos, detalhe.data, "Data");
            AddIfMissing(campos, detalhe.sigla, "Sigla");
            AddIfMissing(campos, detalhe.quantidade, "Qtde");
            AddIfMissing(campos, detalhe.etapa, "Etapa");
            AddIfMissing(campos, detalhe.classificacao, "Classificação");
            AddIfMissing(campos, detalhe.descricao, "Descrição");
            AddIfMissing(campos, detalhe.documento, "Documento");
            AddIfMissing(campos, detalhe.valor, "Valor");

            return ValidarCamposObrigatorios(campos, e);
        }

        private static void AddIfMissing(List<string> campos, object? valor, string nomeCampo)
        {
            if (valor is null)
            {
                campos.Add(nomeCampo);
                return;
            }

            if (valor is string texto && string.IsNullOrWhiteSpace(texto))
                campos.Add(nomeCampo);
        }

        private static bool ValidarCamposObrigatorios(List<string> campos, GridViewRowValidatingEventArgs e)
        {
            if (campos.Count == 0)
                return true;

            var mensagem = "Preencha os campos obrigatórios:\n\n- " + string.Join("\n- ", campos);
            e.IsValid = false;
            Application.Current.Dispatcher.BeginInvoke(() =>
                MessageBox.Show(mensagem, "Campos obrigatórios", MessageBoxButton.OK, MessageBoxImage.Warning));
            return false;
        }

    }

    public partial class CadastroDespesaViewModel : INotifyPropertyChanged
    {
        private static readonly DataBaseSettings BaseSettings = DataBaseSettings.Instance;

        public event PropertyChangedEventHandler PropertyChanged;
        public void RaisePropertyChanged(string propName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        protected void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        private ObservableCollection<OperacionalTDespFuncionarioModel>? funcionarios;
        public ObservableCollection<OperacionalTDespFuncionarioModel> Funcionarios
        {
            get { return funcionarios; }
            set { funcionarios = value; RaisePropertyChanged("Funcionarios"); }
        }

        private OperacionalRelatorioDespesaModel? relatorio;
        public OperacionalRelatorioDespesaModel Relatorio
        {
            get => relatorio;
            set { relatorio = value; RaisePropertyChanged(nameof(Relatorio)); }
        }

        private ObservableCollection<OperacionalRelatorioDespesaModel>? relatorios;
        /*public ObservableCollection<OperacionalRelatorioDespesaModel> Relatorios
        {
            get { return relatorios; }
            set { relatorios = value; RaisePropertyChanged("Relatorios"); }
        }*/
        public ObservableCollection<OperacionalRelatorioDespesaModel> Relatorios
        {
            get => relatorios;
            set
            {
                relatorios = value;
                OnPropertyChanged("Relatorios");

                // Garante que todo mundo tem referência pro pai
                foreach (var relatorio in relatorios)
                {
                    foreach (var detalhe in relatorio.RelatorioDespesaDetalhesEditaveis)
                    {
                        detalhe.RelatorioPai = relatorio;
                    }
                }
            }
        }

        private ObservableCollection<OperacionalDespRelatorioModel> despsRelatorio;
        public ObservableCollection<OperacionalDespRelatorioModel> DespsRelatorio
        {
            get => despsRelatorio;
            set { despsRelatorio = value; RaisePropertyChanged(nameof(DespsRelatorio)); }
        }

        public ObservableCollection<string> ClassFinanceiro { get; set; } = ["ADT DESP", "REL DESP"];
        public ObservableCollection<string> FormaPagamento { get; set; } = ["DOC", "TED", "CHEQUE", "DEP CC", "DIN CAIXA", "PIX"];

        private ObservableCollection<ComercialClienteModel> clientes;
        public ObservableCollection<ComercialClienteModel> Clientes
        {
            get => clientes;
            set { clientes = value; RaisePropertyChanged(nameof(Clientes)); }
        }

        private ObservableCollection<string> fases;
        public ObservableCollection<string> Fases
        {
            get => fases;
            set { fases = value; RaisePropertyChanged(nameof(Fases)); }
        }

        private ObservableCollection<string> classificacoes;
        public ObservableCollection<string> Classificacoes
        {
            get => classificacoes;
            set { classificacoes = value; RaisePropertyChanged(nameof(Classificacoes)); }
        }

        private ObservableCollection<OperacionalBaseCustoModel> custos;
        public ObservableCollection<OperacionalBaseCustoModel> Custos
        {
            get => custos;
            set { custos = value; RaisePropertyChanged(nameof(Custos)); }
        }

        private ObservableCollection<OperacionalEmpresaModel> empresas;
        public ObservableCollection<OperacionalEmpresaModel> Empresas
        {
            get => empresas;
            set { empresas = value; RaisePropertyChanged(nameof(Empresas)); }
        }

        public Dictionary<string, List<string>> DescricoesPorTipo { get; set; }

        public ObservableCollection<RegistroDespesa> ListaDespesas { get; set; } = [];

        public ObservableCollection<OperacionalBaseCustoModel> BaseCustos
        {
            get => custos;
            set
            {
                custos = value;
                RaisePropertyChanged(nameof(BaseCustos));
                RaisePropertyChanged(nameof(TiposClassificacao)); // <-- AQUI
            }
        }

        //public ObservableCollection<string> TiposClassificacao => [.. BaseCustos.Select(b => b.tipo).Distinct()];

        private ObservableCollection<string> _tiposClassificacao = [];
        public ObservableCollection<string> TiposClassificacao
        {
            get => _tiposClassificacao;
            set
            {
                _tiposClassificacao = value;
                OnPropertyChanged(nameof(TiposClassificacao));
            }
        }

        public CadastroDespesaViewModel()
        {
        }

        public List<string> ObterDescricoesPorTipo(string tipo)
        {
            return BaseCustos
                .Where(b => b.tipo == tipo)
                .Select(b => b.descr)
                .ToList();
        }

        public async Task<ObservableCollection<OperacionalTDespFuncionarioModel>> GetFuncionariosAsync()
        {
            try
            {
                await using var connection = new NpgsqlConnection(BaseSettings.ConnectionString);
                var funcionariosComBancos = (await connection.QueryAsync<OperacionalTDespFuncionarioModel>(
                    @"SELECT *
                      FROM operacional.t_desp_funcionario
                      ORDER BY nome_func;")).ToList();

                var dadosBancarios = (await connection.QueryAsync<OperacionalTblDespDadoBancarioModel>(
                    @"SELECT *
                      FROM operacional.tbl_desp_dados_bancarios;")).ToLookup(b => b.cod_func);

                foreach (var funcionario in funcionariosComBancos)
                    funcionario.DadosBancarios = dadosBancarios[funcionario.cod_func].ToList();

                return [.. funcionariosComBancos];
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

        public async Task<ObservableCollection<OperacionalRelatorioDespesaModel>> GetRelatoriosAsync()
        {
            try
            {
                await using var connection = new NpgsqlConnection(BaseSettings.ConnectionString);
                var relatorios = (await connection.QueryAsync<OperacionalRelatorioDespesaModel>(
                    @"SELECT *
                      FROM operacional.t_relatorio_despesas
                      ORDER BY cod_relatorio;")).ToList();

                var adiantamentos = (await connection.QueryAsync<OperacionalAdiantamentoModel>(
                    @"SELECT *
                      FROM operacional.t_adiantamento;")).ToLookup(a => a.cod_relatorio);
                var observacoes = (await connection.QueryAsync<OperacionalRelatorioObservacaoModel>(
                    @"SELECT *
                      FROM operacional.t_relatorio_observacao;")).ToLookup(o => o.cod_relatorio);
                var detalhes = (await connection.QueryAsync<OperacionalRelatorioDespesasDetalheModel>(
                    @"SELECT *
                      FROM operacional.t_relatorio_despesas_detalhe
                      ORDER BY cod_linha_detalhe;")).ToLookup(d => d.cod_relatorio);

                foreach (var relatorio in relatorios)
                {
                    relatorio.RelatorioAdiantamento = [.. adiantamentos[relatorio.cod_relatorio]];
                    relatorio.RelatorioObservacao = [.. observacoes[relatorio.cod_relatorio]];
                    relatorio.RelatorioDespesaDetalhes = detalhes[relatorio.cod_relatorio].ToList();
                }

                return [.. relatorios];
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

        public async Task<ObservableCollection<OperacionalDespRelatorioModel>> GetTiposRelatorioAsync()
        {
            try
            {
                await using var connection = new NpgsqlConnection(BaseSettings.ConnectionString);
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

        public async Task<ObservableCollection<ComercialClienteModel>> GetClientesAsync()
        {
            try
            {
                await using var connection = new NpgsqlConnection(BaseSettings.ConnectionString);
                var retorno = await connection.QueryAsync<ComercialClienteModel>(
                    @"SELECT sigla
                      FROM comercial.clientes
                      WHERE sigla IS NOT NULL
                      UNION
                      SELECT sigla
                      FROM operacional.t_relatorio_despesas_detalhe
                      WHERE sigla IS NOT NULL
                      ORDER BY sigla;");
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

        public async Task<List<OperacionalBaseCustoModel>> GetBaseCustosAsync()
        {
            try
            {
                await using var connection = new NpgsqlConnection(BaseSettings.ConnectionString);
                return (await connection.QueryAsync<OperacionalBaseCustoModel>(
                    @"SELECT *
                      FROM operacional.tblbasecustos
                      ORDER BY tipo, descr;")).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception("Erro ao buscar base de custos", ex);
            }
        }

        public async Task<ObservableCollection<string>> GetEtapasAsync()
        {
            try
            {
                await using var connection = new NpgsqlConnection(BaseSettings.ConnectionString);
                var retorno = await connection.QueryAsync<string>(
                    @"SELECT fase AS etapa
                      FROM operacional.tblfases
                      WHERE fase IS NOT NULL
                      UNION
                      SELECT etapa
                      FROM operacional.t_relatorio_despesas_detalhe
                      WHERE etapa IS NOT NULL
                      ORDER BY etapa;");
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

        public async Task<Dictionary<string, List<string>>> GetDescricoesAsync()
        {
            try
            {
                await using var connection = new NpgsqlConnection(BaseSettings.ConnectionString);
                var retorno = await connection.QueryAsync<OperacionalBaseCustoModel>(
                    @"SELECT *
                      FROM operacional.tblbasecustos
                      ORDER BY tipo, descr;");
                var descricoesPorTipo = retorno
                    .GroupBy(x => x.tipo)
                    .ToDictionary(g => g.Key, g => g.Select(x => x.descr)
                    .ToList());

                return descricoesPorTipo;
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

        public async Task<ObservableCollection<OperacionalEmpresaModel>> GetEmpresasAsync()
        {
            try
            {
                await using var connection = new NpgsqlConnection(BaseSettings.ConnectionString);
                var retorno = await connection.QueryAsync<OperacionalEmpresaModel>(
                    @"SELECT *
                      FROM operacional.t_empresas
                      ORDER BY nome_empresa;");
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

        public async Task<bool> AdcionarRelatorio(OperacionalRelatorioDespesaModel item)
        {
            using var connection = new NpgsqlConnection(BaseSettings.ConnectionString);
            if (item.cod_relatorio <= 0)
                item.cod_relatorio = await connection.QuerySingleAsync<long>(@"
                    INSERT INTO operacional.t_relatorio_despesas
                    (data, codigo_funcionario, nome_funcionario, nome_relatorio, localidade,
                     codigo_empresa, emitido_por, emitido_data, cod_conta_corrente, classif_financeiro)
                    VALUES (@data, @codigo_funcionario, @nome_funcionario, @nome_relatorio, @localidade,
                     @codigo_empresa, @emitido_por, @emitido_data, @cod_conta_corrente, @classif_financeiro)
                    RETURNING cod_relatorio;", item);
            else if (await connection.ExecuteAsync(@"
                    UPDATE operacional.t_relatorio_despesas SET
                        data = @data, codigo_funcionario = @codigo_funcionario,
                        nome_funcionario = @nome_funcionario, nome_relatorio = @nome_relatorio,
                        localidade = @localidade, codigo_empresa = @codigo_empresa,
                        classif_financeiro = @classif_financeiro
                    WHERE cod_relatorio = @cod_relatorio;", item) != 1)
                throw new InvalidOperationException("Relatorio nao encontrado. Recarregue a tela.");
            return true;
        }

        public async Task<bool> AdcionarRelatorioObservacao(OperacionalRelatorioObservacaoModel relatorio)
        {
            try
            {
                await using var connection = new NpgsqlConnection(BaseSettings.ConnectionString);
                await connection.ExecuteAsync(@"
                    INSERT INTO operacional.t_relatorio_observacao
                    (cod_relatorio, observacao)
                    VALUES (@cod_relatorio, @observacao)
                    ON CONFLICT (cod_relatorio) DO UPDATE SET
                        observacao = EXCLUDED.observacao;",
                    relatorio);

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

        public async Task<bool> AdcionarRelatorioAdiantamento(OperacionalAdiantamentoModel relatorio)
        {
            try
            {
                await using var connection = new NpgsqlConnection(BaseSettings.ConnectionString);
                const string sql = @"
                    INSERT INTO operacional.t_adiantamento
                    (cod_relatorio, valor_adiantamento, valor_real_peso, valor_cotacao_peso, valor_peso_peso,
                     valor_real_dolar, valor_cotacao_dolar, valor_dolar_dolar, total_adiantamento, total_despesas,
                     saldo_final, emitido_por, emitido_data, alterado_por, alterado_data, aprovado_por,
                     data_aprovacao, data_pagamento, forma_pagto, pagto_realizado_por, data_pagto_realizado, aprovacao)
                    VALUES
                    (@cod_relatorio, @valor_adiantamento, @valor_real_peso, @valor_cotacao_peso, @valor_peso_peso,
                     @valor_real_dolar, @valor_cotacao_dolar, @valor_dolar_dolar, @total_adiantamento, @total_despesas,
                     @saldo_final, @emitido_por, @emitido_data, @alterado_por, @alterado_data, @aprovado_por,
                     @data_aprovacao, @data_pagamento, @forma_pagto, @pagto_realizado_por, @data_pagto_realizado, @aprovacao)
                    ON CONFLICT (cod_relatorio) DO UPDATE SET
                        valor_adiantamento = EXCLUDED.valor_adiantamento,
                        valor_real_peso = EXCLUDED.valor_real_peso,
                        valor_cotacao_peso = EXCLUDED.valor_cotacao_peso,
                        valor_peso_peso = EXCLUDED.valor_peso_peso,
                        valor_real_dolar = EXCLUDED.valor_real_dolar,
                        valor_cotacao_dolar = EXCLUDED.valor_cotacao_dolar,
                        valor_dolar_dolar = EXCLUDED.valor_dolar_dolar,
                        total_adiantamento = EXCLUDED.total_adiantamento,
                        alterado_por = EXCLUDED.alterado_por,
                        alterado_data = EXCLUDED.alterado_data,
                        data_pagamento = EXCLUDED.data_pagamento,
                        forma_pagto = EXCLUDED.forma_pagto;";

                await connection.ExecuteAsync(sql, relatorio);

                return true;
            }
            catch (DbUpdateException)
            {
                throw;
            }
            catch (Exception ex)  // Para qualquer outro erro
            {
                throw;
            }
        }

        public async Task<bool> AdcionarRelatorioDetalhes(OperacionalRelatorioDespesasDetalheModel item)
        {
            if (item.cod_relatorio is null or <= 0)
                throw new InvalidOperationException("Salve o relatorio antes de incluir despesas.");
            using var connection = new NpgsqlConnection(BaseSettings.ConnectionString);
            if (item.cod_linha_detalhe is null or <= 0)
                item.cod_linha_detalhe = await connection.QuerySingleAsync<long>(@"
                    INSERT INTO operacional.t_relatorio_despesas_detalhe
                    (cod_relatorio, data, sigla, quantidade, etapa, classificacao, descricao,
                     valor, codigo_empresa, cod_relatorio_empresa, emitido_por, emitido_data, documento)
                    VALUES (@cod_relatorio, @data, @sigla, @quantidade, @etapa, @classificacao, @descricao,
                     @valor, @codigo_empresa, @cod_relatorio_empresa, @emitido_por, @emitido_data, @documento)
                    RETURNING cod_linha_detalhe;", item);
            else if (await connection.ExecuteAsync(@"
                    UPDATE operacional.t_relatorio_despesas_detalhe SET
                        data = @data, sigla = @sigla, quantidade = @quantidade, etapa = @etapa,
                        classificacao = @classificacao, descricao = @descricao, valor = @valor,
                        codigo_empresa = @codigo_empresa, cod_relatorio_empresa = @cod_relatorio_empresa,
                        alterado_por = @alterado_por, alterado_data = @alterado_data, documento = @documento
                    WHERE cod_linha_detalhe = @cod_linha_detalhe AND cod_relatorio = @cod_relatorio;", item) != 1)
                throw new InvalidOperationException("Despesa nao encontrada neste relatorio. Recarregue a tela.");
            return true;
        }
    }

    public class RegistroDespesa : INotifyPropertyChanged
    {
        private string _classificacao;
        private string _descricao;

        public ObservableCollection<string> DescricoesDisponiveis { get; set; } = [];
        public OperacionalRelatorioDespesaModel RelatorioPai { get; set; }

        public long? cod_linha_detalhe { get; set; }
        public long? cod_relatorio { get; set; }
        public DateTime? data { get; set; }
        public string? sigla { get; set; }
        public double? quantidade { get; set; }
        public string? etapa { get; set; }

        public string classificacao
        {
            get => _classificacao;
            set
            {
                if (_classificacao != value)
                {
                    _classificacao = value;
                    OnPropertyChanged();
                    AtualizarDescricoes();
                }
            }
        }

        public string descricao
        {
            get => _descricao;
            set { _descricao = value; OnPropertyChanged(); }
        }

        public double? valor { get; set; }
        public long? codigo_empresa { get; set; }
        public long? cod_relatorio_empresa { get; set; }
        public string? emitido_por { get; set; }
        public DateTime? emitido_data { get; set; }
        public string? alterado_por { get; set; }
        public DateTime? alterado_data { get; set; }
        public string? documento { get; set; }

        public static Func<string, List<string>> BuscarDescricoesExternamente;

        public RegistroDespesa() 
        {
        }

        public RegistroDespesa(OperacionalRelatorioDespesasDetalheModel model)
        {
            cod_linha_detalhe = model.cod_linha_detalhe;
            cod_relatorio = model.cod_relatorio;
            data = model.data;
            sigla = model.sigla;
            quantidade = model.quantidade;
            etapa = model.etapa;
            classificacao = model.classificacao;
            descricao = model.descricao;
            valor = model.valor;
            codigo_empresa = model.codigo_empresa;
            cod_relatorio_empresa = model.cod_relatorio_empresa;
            emitido_por = model.emitido_por;
            emitido_data = model.emitido_data;
            alterado_por = model.alterado_por;
            alterado_data = model.alterado_data;
            documento = model.documento;
        }

        public RegistroDespesa(OperacionalRelatorioDespesaModel relatorioPai)
        {
            RelatorioPai = relatorioPai;
        }

        public  RegistroDespesa(OperacionalRelatorioDespesasDetalheModel model, OperacionalRelatorioDespesaModel relatorioPai) : this(model) // chama o construtor padrão que faz o mapeamento
        {
            RelatorioPai = relatorioPai;
        }

        // Para salvar depois (opcional)
        public OperacionalRelatorioDespesasDetalheModel ToModel()
        {
            return new OperacionalRelatorioDespesasDetalheModel
            {
                cod_linha_detalhe = cod_linha_detalhe,
                cod_relatorio = cod_relatorio,
                data = data,
                sigla = sigla,
                quantidade = quantidade,
                etapa = etapa,
                classificacao = classificacao,
                descricao = descricao,
                valor = valor,
                codigo_empresa = codigo_empresa,
                cod_relatorio_empresa = cod_relatorio_empresa,
                emitido_por = emitido_por,
                emitido_data = emitido_data,
                alterado_por = alterado_por,
                alterado_data = alterado_data,
                documento = documento
            };
        }


        private void AtualizarDescricoes()
        {
            DescricoesDisponiveis.Clear();

            if (BuscarDescricoesExternamente != null && !string.IsNullOrWhiteSpace(classificacao))
            {
                foreach (var d in BuscarDescricoesExternamente(classificacao))
                    DescricoesDisponiveis.Add(d);
            }

        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public static class CadastroDespesaRelatorioPrinter
    {
        private static readonly DataBaseSettings BaseSettings = DataBaseSettings.Instance;

        public static void Imprimir(OperacionalRelatorioDespesaModel record, CadastroDespesaViewModel vm)
        {
            var nomeEmpresa = vm.Empresas.FirstOrDefault(e => e.codigo_empresa == record.codigo_empresa)?.nome_empresa;
            var nomeFuncionario = vm.Funcionarios.FirstOrDefault(e => e.cod_func == record.codigo_funcionario)?.nome_func;
            var valorReembolso = record.RelatorioDespesaDetalhesEditaveis.Where(x => x.classificacao == "FINANCEIRO" && x.descricao == "REEMBOLSO").Sum(x => x.valor);
            var valorAcerto = record.RelatorioDespesaDetalhesEditaveis.Where(x => x.classificacao == "FINANCEIRO" && x.descricao == "ACERTO FINANCEIRO").Sum(x => x.valor);

            try
            {
                var outputPath = ResolveOutputPath(record.cod_relatorio);
                var modeloPath = ResolveModeloPath("MODELO-RELATORIO-DESPESA.xlsx");

                using var workbook = new XLWorkbook(modeloPath);
                var worksheet = workbook.Worksheet(1);

                worksheet.Cell("A3").Value = record.cod_relatorio;
                worksheet.Cell("A4").Value = @$"{record.codigo_empresa} {nomeEmpresa}";
                worksheet.Cell("C5").Value = record.data;
                worksheet.Cell("C6").Value = nomeFuncionario;
                worksheet.Cell("C7").Value = record.nome_relatorio;
                worksheet.Cell("C8").Value = record.localidade;

                worksheet.Range("total_adiantamento").FirstCell().Value = Convert.ToDouble(record.RelatorioAdiantamento.FirstOrDefault()?.total_adiantamento ?? 0);
                worksheet.Range("valor_adiantamento").FirstCell().Value = Convert.ToDouble(record.RelatorioAdiantamento.FirstOrDefault()?.valor_adiantamento ?? 0);
                worksheet.Range("valor_reembolso").FirstCell().Value = Convert.ToDouble(valorReembolso ?? 0);
                worksheet.Range("valor_acerto").FirstCell().Value = Convert.ToDouble(valorAcerto ?? 0);

                int linhaInicial = 11;

                foreach (var item in record.RelatorioDespesaDetalhesEditaveis.Where(r => r.valor is not null && r.valor != 0))
                {
                    worksheet.Cell("A" + linhaInicial).Value = item.data;
                    worksheet.Cell("B" + linhaInicial).Value = item.sigla;
                    worksheet.Cell("C" + linhaInicial).Value = Convert.ToDouble(item.quantidade);
                    worksheet.Cell("D" + linhaInicial).Value = item.etapa;
                    worksheet.Range("E" + linhaInicial + ":F" + linhaInicial).Merge();
                    worksheet.Cell("E" + linhaInicial).Value = item.classificacao;
                    worksheet.Range("G" + linhaInicial + ":H" + linhaInicial).Merge();
                    worksheet.Cell("G" + linhaInicial).Value = item.descricao;
                    worksheet.Cell("I" + linhaInicial).Value = item.documento;
                    worksheet.Cell("J" + linhaInicial).Value = Convert.ToDouble(item.valor);

                    linhaInicial++;
                    worksheet.Row(linhaInicial).InsertRowsAbove(1);
                }

                linhaInicial += 3;
                worksheet.Cell($"A{linhaInicial}").Value = record.RelatorioObservacao.FirstOrDefault()?.observacao;

                workbook.SaveAs(outputPath);
                Process.Start("explorer", outputPath);
            }
            catch (Exception ex)
            {
                Operacional.ErrorDialog.Show(ex, "Erro");
            }
        }

        private static string ResolveOutputPath(long codRelatorio)
        {
            return SistemaPathResolver.GetImpressosPath($"RELATORIO-DESPESA-{codRelatorio}.xlsx");
        }

        private static string ResolveModeloPath(string fileName)
        {
            return SistemaPathResolver.GetModeloPath(fileName);
        }
    }
}
