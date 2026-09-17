using Dapper;
using Npgsql;
using Operacional.DataBase;
using Operacional.DataBase.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Operacional.Views.Despesa
{
    /// <summary>
    /// Interação lógica para CadastroFuncionario.xam
    /// </summary>
    public partial class CadastroFuncionario : UserControl
    {
        public CadastroFuncionario()
        {
            InitializeComponent();
            this.DataContext = new CadastroFuncionarioViewModel();
        }

        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = Cursors.Wait; });
                CadastroFuncionarioViewModel vm = (CadastroFuncionarioViewModel)DataContext;
                //vm.HtFuncionarios = await vm.GetHtFuncionariosAsync();
                vm.Empresas = await vm.GetEmpresasAsync();
                vm.Funcionarios = await vm.GetFuncionariosAsync();
                Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
            }
            catch (DbUpdateException ex)
            {
                Operacional.ErrorDialog.Show(ex, "Erro de banco de dados");
                Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
            }
        }

        private void RGVFuncionario_RowValidating(object sender, Telerik.Windows.Controls.GridViewRowValidatingEventArgs e)
        {
            if (e.Row?.IsInEditMode != true) return;
            if (e.Row.Item is not OperacionalTDespFuncionarioModel item) return;
            if (string.IsNullOrWhiteSpace(item.nome_func))
            {
                e.IsValid = false;
                return;
            }
            ValidatedGridSave.Save(sender, e, async () =>
            {
                await ((CadastroFuncionarioViewModel)DataContext).AdcionarFuncionario(item);
                item.DadosBancarios ??= [];
            });
        }

        private void RGVBanco_RowValidating(object sender, Telerik.Windows.Controls.GridViewRowValidatingEventArgs e)
        {
            if (e.Row?.IsInEditMode != true) return;
            if (e.Row.Item is not OperacionalTblDespDadoBancarioModel item) return;
            var pai = (sender as FrameworkElement)?.DataContext as OperacionalTDespFuncionarioModel;
            if (pai?.cod_func is null or <= 0)
            {
                e.IsValid = false;
                return;
            }
            item.cod_func = pai.cod_func;
            ValidatedGridSave.Save(sender, e, () => ((CadastroFuncionarioViewModel)DataContext).AdcionarDadosBancarioFuncionario(item));
        }
    }

    public partial class CadastroFuncionarioViewModel : INotifyPropertyChanged
    {
        private readonly DataBaseSettings _dataBaseSettings = DataBaseSettings.Instance;

        public event PropertyChangedEventHandler PropertyChanged;
        public void RaisePropertyChanged(string propName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        protected void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        private ObservableCollection<OperacionalTDespFuncionarioModel>? funcionarios;
        public ObservableCollection<OperacionalTDespFuncionarioModel> Funcionarios
        {
            get { return funcionarios; }
            //set { funcionarios = value; RaisePropertyChanged("Funcionarios"); }
            set { funcionarios = value; OnPropertyChanged(nameof(Funcionarios)); }
        }

        private OperacionalTDespFuncionarioModel? transporte;
        public OperacionalTDespFuncionarioModel Transporte
        {
            get { return transporte; }
            //set { transporte = value; RaisePropertyChanged("Transporte"); }
            set { transporte = value; OnPropertyChanged(nameof(Transporte)); }
        }

        private ObservableCollection<HtFuncionarioModel>? htFuncionarios;
        public ObservableCollection<HtFuncionarioModel> HtFuncionarios
        {
            get { return htFuncionarios; }
            //set { htFuncionarios = value; RaisePropertyChanged("HtFuncionarios"); }
            set { htFuncionarios = value; OnPropertyChanged(nameof(HtFuncionarios)); }
        }

        private ObservableCollection<ComprasEmpresaModel>? empresas;
        public ObservableCollection<ComprasEmpresaModel> Empresas
        {
            get { return empresas; }
            //set { empresas = value; RaisePropertyChanged("Empresas"); }
            set { empresas = value; OnPropertyChanged(nameof(Empresas)); }
        }

        public ObservableCollection<string> Financeiros { get; set; } = [ "RHE", "DSL" ];

        public async Task<ObservableCollection<OperacionalTDespFuncionarioModel>> GetFuncionariosAsync()
        {
            try
            {
                using var connection = new NpgsqlConnection(_dataBaseSettings.ConnectionString);
                var funcionariosComBancos = (await connection.QueryAsync<OperacionalTDespFuncionarioModel>(
                    @"SELECT *
                      FROM operacional.t_desp_funcionario
                      ORDER BY nome_func;")).ToList();

                var dadosBancarios = (await connection.QueryAsync<OperacionalTblDespDadoBancarioModel>(
                    @"SELECT *
                      FROM operacional.tbl_desp_dados_bancarios
                      ORDER BY cod_func, banco, agencia;")).ToList();

                var bancosPorFuncionario = dadosBancarios
                    .Where(d => d.cod_func is not null)
                    .GroupBy(d => d.cod_func)
                    .ToDictionary(g => g.Key, g => g.ToList());

                foreach (var funcionario in funcionariosComBancos)
                {
                    if (funcionario.cod_func is not null &&
                        bancosPorFuncionario.TryGetValue(funcionario.cod_func, out var bancos))
                    {
                        funcionario.DadosBancarios = new ObservableCollection<OperacionalTblDespDadoBancarioModel>(bancos);
                    }
                    else
                    {
                        funcionario.DadosBancarios = [];
                    }
                }

                return [..funcionariosComBancos];
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

        public async Task<bool> AdcionarFuncionario(OperacionalTDespFuncionarioModel funcionario)
        {
            try
            {
                using var connection = new NpgsqlConnection(_dataBaseSettings.ConnectionString);

                if (funcionario.cod_func is null or <= 0)
                {
                    funcionario.cod_func = await connection.ExecuteScalarAsync<long>(@"
                        INSERT INTO operacional.t_desp_funcionario
                        (nome_func, telefone_func, celular_func, cidade_func, estado_func,
                         observacao, cpf, empresa, tipo_financeiro, cnpj_razao_social)
                        VALUES
                        (@nome_func, @telefone_func, @celular_func, @cidade_func, @estado_func,
                         @observacao, @cpf, @empresa, @tipo_financeiro, @cnpj_razao_social)
                        RETURNING cod_func;",
                        funcionario);

                    return true;
                }

                var linhas = await connection.ExecuteAsync(@"
                    UPDATE operacional.t_desp_funcionario
                    SET
                        nome_func = @nome_func,
                        telefone_func = @telefone_func,
                        celular_func = @celular_func,
                        cidade_func = @cidade_func,
                        estado_func = @estado_func,
                        observacao = @observacao,
                        cpf = @cpf,
                        empresa = @empresa,
                        tipo_financeiro = @tipo_financeiro,
                        cnpj_razao_social = @cnpj_razao_social
                    WHERE cod_func = @cod_func;",
                    funcionario);

                if (linhas == 0)
                {
                    throw new InvalidOperationException("Funcionario nao encontrado. Recarregue a tela.");
                }

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

        public async Task<bool> AdcionarDadosBancarioFuncionario(OperacionalTblDespDadoBancarioModel dadosBancario)
        {
            try
            {
                using var connection = new NpgsqlConnection(_dataBaseSettings.ConnectionString);

                if (dadosBancario.cod_linha_dados_bancarios is null or <= 0)
                {
                    dadosBancario.cod_linha_dados_bancarios = await connection.ExecuteScalarAsync<long>(@"
                        INSERT INTO operacional.tbl_desp_dados_bancarios
                        (cod_func, titular_conta, banco, tipo_conta, agencia, numero_conta,
                         digito_agencia, digito_conta, cpf_conta)
                        VALUES
                        (@cod_func, @titular_conta, @banco, @tipo_conta, @agencia, @numero_conta,
                         @digito_agencia, @digito_conta, @cpf_conta)
                        RETURNING cod_linha_dados_bancarios;",
                        dadosBancario);

                    return true;
                }

                var linhas = await connection.ExecuteAsync(@"
                    UPDATE operacional.tbl_desp_dados_bancarios
                    SET
                        titular_conta = @titular_conta,
                        banco = @banco,
                        tipo_conta = @tipo_conta,
                        agencia = @agencia,
                        numero_conta = @numero_conta,
                        digito_agencia = @digito_agencia,
                        digito_conta = @digito_conta,
                        cpf_conta = @cpf_conta
                    WHERE cod_linha_dados_bancarios = @cod_linha_dados_bancarios AND cod_func = @cod_func;",
                    dadosBancario);

                if (linhas == 0)
                {
                    throw new InvalidOperationException("Dados bancarios nao encontrados. Recarregue a tela.");
                }

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

        public async Task<ObservableCollection<HtFuncionarioModel>> GetHtFuncionariosAsync()
        {
            try
            {
                using var connection = new NpgsqlConnection(_dataBaseSettings.ConnectionString);
                var funcionariosComBancos = await connection.QueryAsync<HtFuncionarioModel>(
                    @"SELECT *
                      FROM ht.view_ht_funcionarios
                      WHERE data_demissao IS NULL
                      ORDER BY nome_apelido;");

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

        public async Task<ObservableCollection<ComprasEmpresaModel>> GetEmpresasAsync()
        {
            try
            {
                using var connection = new NpgsqlConnection(_dataBaseSettings.ConnectionString);
                var empresas = await connection.QueryAsync<ComprasEmpresaModel>(
                    @"SELECT *
                      FROM compras.tblempresa
                      ORDER BY abreviacao;");

                return [.. empresas];
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
