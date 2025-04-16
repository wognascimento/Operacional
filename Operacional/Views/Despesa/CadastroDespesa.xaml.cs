using Microsoft.EntityFrameworkCore;
using Operacional.DataBase.Models;
using Syncfusion.UI.Xaml.Grid;
using Syncfusion.UI.Xaml.Utility;
using Syncfusion.XlsIO;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

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
                Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = Cursors.Wait; });

                vm.BaseCustos = [.. await vm.GetBaseCustosAsync()];
                vm.DescricoesPorTipo = await vm.GetDescricoesAsync();
                RegistroDespesa.BuscarDescricoesExternamente = vm.ObterDescricoesPorTipo;

                //vm.BaseCustos = new ObservableCollection<OperacionalBaseCustoModel>(await vm.GetBaseCustosAsync());
                vm.TiposClassificacao = [.. vm.BaseCustos.Select(b => b.tipo).Distinct()];

                var relatorios = await vm.GetRelatoriosAsync();
                
                foreach (var relatorio in relatorios)
                {
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
                            var r = new RegistroDespesa(x, relatorio)
                            {
                                DescricoesDisponiveis = [.. vm.ObterDescricoesPorTipo(x.classificacao) ?? []]
                            };
                            return r;
                        })
                        .ToList() : [ new RegistroDespesa(relatorio) ];
                    
                    relatorio.RelatorioDespesaDetalhesEditaveis = [.. registros];
                    
                }

                // Atualiza a propriedade no ViewModel
                vm.Relatorios = relatorios;

                // Atribui ao ViewModel
                //m.Relatorios = relatorios;
                vm.Funcionarios = await vm.GetFuncionariosAsync();
                vm.DespsRelatorio = await vm.GetTiposRelatorioAsync();
                vm.Clientes = await vm.GetClientesAsync();
                vm.Empresas = await vm.GetEmpresasAsync();
                vm.Fases = await vm.GetEtapasAsync();
                Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
            }
            catch (DbUpdateException ex)
            {
                MessageBox.Show(ex.InnerException?.Message);
                Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
            }
        }

        private async void dGRelatorio_RowValidating(object sender, Syncfusion.UI.Xaml.Grid.RowValidatingEventArgs e)
        {
            CadastroDespesaViewModel vm = (CadastroDespesaViewModel)DataContext;
            try
            {
                Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = Cursors.Wait; });
                var relatorio = e.RowData as OperacionalRelatorioDespesaModel;
                relatorio.RelatorioObservacao = new ObservableCollection<OperacionalRelatorioObservacaoModel> {
                    new() 
                    {
                        observacao = "",
                    }
                };
                relatorio.RelatorioAdiantamento = new ObservableCollection<OperacionalAdiantamentoModel> {
                    new()
                    {
                        emitido_por = "",
                        emitido_data = DateTime.Now.Date,
                    }
                };
                bool sucesso = await vm.AdcionarRelatorio(relatorio);
                if (sucesso == false)
                {
                    Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
                    e.IsValid = false; // Impede que a linha seja confirmada
                    MessageBox.Show("Erro ao salvar no banco! Verifique os dados.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (DbUpdateException ex)
            {
                Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
                MessageBox.Show($"Erro: {ex.InnerException?.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                e.IsValid = false; // Impede que a linha seja confirmada
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
                MessageBox.Show($"Erro: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                e.IsValid = false; // Impede que a linha seja confirmada
            }
            finally
            {
                Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
            }
        }

        private async void dGRelatorioObservacao_RowValidating(object sender, Syncfusion.UI.Xaml.Grid.RowValidatingEventArgs e)
        {
            CadastroDespesaViewModel vm = (CadastroDespesaViewModel)DataContext;
            try
            {
                Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = Cursors.Wait; });
                var relatorio = e.RowData as OperacionalRelatorioObservacaoModel;

                bool sucesso = await vm.AdcionarRelatorioObservacao(relatorio);
                if (sucesso == false)
                {
                    Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
                    e.IsValid = false; // Impede que a linha seja confirmada
                    MessageBox.Show("Erro ao salvar no banco! Verifique os dados.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (DbUpdateException ex)
            {
                Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
                MessageBox.Show($"Erro: {ex.InnerException?.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                e.IsValid = false; // Impede que a linha seja confirmada
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
                MessageBox.Show($"Erro: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                e.IsValid = false; // Impede que a linha seja confirmada
            }
            finally
            {
                Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
            }
        }

        private async void dGRelatorioAdiantamento_RowValidating(object sender, Syncfusion.UI.Xaml.Grid.RowValidatingEventArgs e)
        {
            CadastroDespesaViewModel vm = (CadastroDespesaViewModel)DataContext;
            try
            {
                Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = Cursors.Wait; });
                var relatorio = e.RowData as OperacionalAdiantamentoModel;

                bool sucesso = await vm.AdcionarRelatorioAdiantamento(relatorio);
                if (sucesso == false)
                {
                    Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
                    e.IsValid = false; // Impede que a linha seja confirmada
                    MessageBox.Show("Erro ao salvar no banco! Verifique os dados.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (DbUpdateException ex)
            {
                Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
                MessageBox.Show($"Erro: {ex.InnerException?.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                e.IsValid = false; // Impede que a linha seja confirmada
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
                MessageBox.Show($"Erro: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                e.IsValid = false; // Impede que a linha seja confirmada
            }
            finally
            {
                Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
            }
        }

        private async void dGRelatorioDetalhes_RowValidating(object sender, RowValidatingEventArgs e)
        {
            CadastroDespesaViewModel vm = (CadastroDespesaViewModel)DataContext;
            var grid = ((SfDataGrid)sender);
            var vModel = grid.DataContext;
            var pai = vm.Relatorio; //((RegistroDespesa)e.RowData).RelatorioPai; //{Operacional.DataBase.Models.OperacionalRelatorioDespesaModel}
            try
            {
                Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = Cursors.Wait; });

                var registro = e.RowData as RegistroDespesa ?? throw new InvalidOperationException("Linha inválida, não é um RegistroDespesa.");
                var model = registro.ToModel(); // Aqui você converte para o model de banco
                model.cod_relatorio = pai.cod_relatorio;

                bool sucesso = await vm.AdcionarRelatorioDetalhes(model);
                if (!sucesso)
                {
                    e.IsValid = false;
                    MessageBox.Show("Erro ao salvar no banco! Verifique os dados.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (DbUpdateException ex)
            {
                File.WriteAllText("erro_log.txt", ex.ToString());
                MessageBox.Show($"Erro: {ex.InnerException?.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                e.IsValid = false;
            }
            catch (Exception ex)
            {
                File.WriteAllText("erro_log.txt", ex.ToString());
                MessageBox.Show($"Erro: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                e.IsValid = false;
            }
            finally
            {
                Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
            }
        }

        private void dGRelatorioDetalhes_AddNewRowInitiating(object sender, Syncfusion.UI.Xaml.Grid.AddNewRowInitiatingEventArgs e)
        {
            //CadastroDespesaViewModel vm = (CadastroDespesaViewModel)DataContext;

            //(e.NewObject as RegistroDespesa).cod_relatorio = vm.Relatorios.cod_relatorio;
        }
    }

    public partial class CadastroDespesaViewModel : INotifyPropertyChanged
    {
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
                using Context context = new();
                var funcionariosComBancos = await context.DespFuncionarios
                    .Include(f => f.DadosBancarios)
                    .ToListAsync();
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
                using Context context = new();
                var relatorios = await context.RelatorioDespesas
                    .Include(r => r.RelatorioAdiantamento)
                    .Include(r => r.RelatorioObservacao)
                    .Include(r => r.RelatorioDespesaDetalhes)
                    .ToListAsync();
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

        public async Task<ObservableCollection<ComercialClienteModel>> GetClientesAsync()
        {
            using Context context = new();
            try
            {
                var retorno = await context.ComercialClientes.AsNoTracking().ToListAsync();
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
            using Context context = new();
            try
            {
                return await context.OperacionalBaseCustos.AsNoTracking().ToListAsync();
            }
            catch (Exception ex)
            {
                throw new Exception("Erro ao buscar base de custos", ex);
            }
        }

        public async Task<ObservableCollection<string>> GetEtapasAsync()
        {
            using Context context = new();
            try
            {
                var retorno = await context.OperacionalFases.AsNoTracking().Select(e => e.fase).ToListAsync();
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
            using Context context = new();
            try
            {
                var retorno = await context.OperacionalBaseCustos.AsNoTracking().ToListAsync();
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
            using Context context = new();
            try
            {
                var retorno = await context.OperacionalEmpresas.AsNoTracking().ToListAsync();
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

        public async Task<bool> AdcionarRelatorio(OperacionalRelatorioDespesaModel relatorio)
        {
            try
            {
                using Context context = new();
                var relatorioExistente = await context.RelatorioDespesas.FindAsync(relatorio.cod_relatorio);
                if (relatorioExistente == null)
                    context.RelatorioDespesas.Add(relatorio);
                else
                    context.Entry(relatorioExistente).CurrentValues.SetValues(relatorio);

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

        public async Task<bool> AdcionarRelatorioObservacao(OperacionalRelatorioObservacaoModel relatorio)
        {
            try
            {
                using Context context = new();
                var relatorioExistente = await context.RelatorioDespesaObservacoes.FindAsync(relatorio.cod_relatorio);
                if (relatorioExistente == null)
                    context.RelatorioDespesaObservacoes.Add(relatorio);
                else
                    context.Entry(relatorioExistente).CurrentValues.SetValues(relatorio);

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

        public async Task<bool> AdcionarRelatorioAdiantamento(OperacionalAdiantamentoModel relatorio)
        {
            try
            {
                using Context context = new();
                var relatorioExistente = await context.RelatorioDespesaAdiantamentos.FindAsync(relatorio.cod_relatorio);
                if (relatorioExistente == null)
                    context.RelatorioDespesaAdiantamentos.Add(relatorio);
                else
                    context.Entry(relatorioExistente).CurrentValues.SetValues(relatorio);

                await context.SaveChangesAsync();

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

        public async Task<bool> AdcionarRelatorioDetalhes(OperacionalRelatorioDespesasDetalheModel relatorio)
        {
            try
            {
                using var context = new Context();

                // Busca o objeto existente pela chave primária
                var relatorioExistente = await context.RelatorioDespesasDetalhes.FindAsync(relatorio.cod_linha_detalhe);

                if (relatorioExistente == null)
                {
                    context.RelatorioDespesasDetalhes.Add(relatorio);
                }
                else
                {
                    context.Entry(relatorioExistente).CurrentValues.SetValues(relatorio);
                }

                await context.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateException ex)
            {
                // Aqui você pode logar ou mostrar algo mais específico se quiser
                throw;
            }
            catch (Exception ex)
            {
                throw;
            }
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

    public static class CadastroDespesaContextMenuCommands
    {
        static BaseCommand? imprimir;
        public static BaseCommand Imprimir
        {
            get
            {
                imprimir ??= new BaseCommand(OnImprimirClicked);
                return imprimir;
            }
        }

        private async static void OnImprimirClicked(object obj)
        {
            var record = ((GridRecordContextMenuInfo)obj).Record as OperacionalRelatorioDespesaModel;
            var grid = ((GridRecordContextMenuInfo)obj).DataGrid;
            //var item = grid.SelectedItem as OperacionalRelatorioDespesaModel;
            CadastroDespesaViewModel vm = (CadastroDespesaViewModel)grid.DataContext;
            var nomeEmpresa = vm.Empresas.FirstOrDefault(e => e.codigo_empresa == record.codigo_empresa)?.nome_empresa;
            var nomeFuncionario = vm.Funcionarios.FirstOrDefault(e => e.cod_func == record.codigo_funcionario)?.nome_func;
            var valorReembolso = record.RelatorioDespesaDetalhesEditaveis.Where(x => x.classificacao == "FINANCEIRO" && x.descricao == "REEMBOLSO").Sum(x => x.valor);
            var valorAcerto = record.RelatorioDespesaDetalhesEditaveis.Where(x => x.classificacao == "FINANCEIRO" && x.descricao == "ACERTO FINANCEIRO").Sum(x => x.valor);

            try
            {
                //701 Cipolatti Artes - Instalação e Montagem de Bens Móveis Ltda - Epp

                using (ExcelEngine excelEngine = new())
                {
                    IApplication application = excelEngine.Excel;
                    application.DefaultVersion = ExcelVersion.Excel2016;

                    // Abre o arquivo modelo
                    FileStream inputStream = new(@"C:\SIG\Operacional S.I.G\Modelos\MODELO-RELATORIO-DESPESA.xlsx", FileMode.Open, FileAccess.Read);
                    IWorkbook workbook = application.Workbooks.Open(inputStream);
                    IWorksheet worksheet = workbook.Worksheets[0];
                    // Preenche células fixas
                    worksheet.Range["A3"].Number = record.cod_relatorio;
                    worksheet.Range["A4"].Text = @$"{record.codigo_empresa} {nomeEmpresa}";
                    worksheet.Range["C5"].DateTime = record.data.Value;
                    worksheet.Range["C6"].Text = nomeFuncionario;
                    worksheet.Range["C7"].Text = record.nome_relatorio;
                    worksheet.Range["C8"].Text = record.localidade;
                    

                    // Campos de valores calculados
                    worksheet.Range["total_adiantamento"].Number = Convert.ToDouble(record.RelatorioAdiantamento.FirstOrDefault().total_adiantamento);
                    worksheet.Range["valor_adiantamento"].Number = Convert.ToDouble(record.RelatorioAdiantamento.FirstOrDefault().valor_adiantamento);
                    worksheet.Range["valor_reembolso"].Number = Convert.ToDouble(valorReembolso);
                    worksheet.Range["valor_acerto"].Number = Convert.ToDouble(valorAcerto);

                    int linhaInicial = 11; // Inserir a partir da linha 11

                    foreach (var item in record.RelatorioDespesaDetalhesEditaveis)
                    {
                        worksheet.Range["A" + linhaInicial].DateTime = item.data.Value;
                        worksheet.Range["B" + linhaInicial].Text = item.sigla;
                        worksheet.Range["C" + linhaInicial].Number = Convert.ToDouble(item.quantidade);
                        worksheet.Range["D" + linhaInicial].Text = item.etapa;
                        // Mescla E e F, insere valor
                        worksheet.Range["E" + linhaInicial + ":F" + linhaInicial].Merge();
                        worksheet.Range["E" + linhaInicial].Text = item.classificacao;
                        // Mescla H e I, insere valor
                        worksheet.Range["G" + linhaInicial + ":H" + linhaInicial].Merge();
                        worksheet.Range["G" + linhaInicial].Text = item.descricao;
                        worksheet.Range["I" + linhaInicial].Text = item.documento;
                        worksheet.Range["J" + linhaInicial].Number = Convert.ToDouble(item.valor);
                        linhaInicial++;
                        worksheet.InsertRow(linhaInicial, 1, ExcelInsertOptions.FormatAsBefore);
                    }
                    linhaInicial+=3;
                    worksheet.Range[@$"A{linhaInicial}"].Text = record.RelatorioObservacao.FirstOrDefault().observacao;
                    // Salva como novo arquivo
                    FileStream outputStream = new(@$"C:\SIG\Operacional S.I.G\Impressos\RELATORIO-DESPESA-{record.cod_relatorio}.xlsx", FileMode.Create, FileAccess.Write);
                    workbook.SaveAs(outputStream);

                    workbook.Close();
                    inputStream.Close();
                    outputStream.Close();

                    Process.Start("explorer", @$"C:\SIG\Operacional S.I.G\Impressos\RELATORIO-DESPESA-{record.cod_relatorio}.xlsx");
                }

                Console.WriteLine("Arquivo gerado com sucesso!");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}
