using Microsoft.EntityFrameworkCore;
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
                MessageBox.Show(ex.InnerException.Message);
                Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
            }
        }

        private async void RGVFuncionario_RowValidating(object sender, Telerik.Windows.Controls.GridViewRowValidatingEventArgs e)
        {
            try
            {
                CadastroFuncionarioViewModel vm = (CadastroFuncionarioViewModel)DataContext;
                if (!e.Row.IsInEditMode)
                    return;

                if (e.Row.Item is OperacionalTDespFuncionarioModel funcionario)
                {
                    funcionario.DadosBancarios = new ObservableCollection<OperacionalTblDespDadoBancarioModel>
                    {
                        new() {
                            titular_conta = funcionario.nome_func,
                        },
                    };
                    bool sucesso = await vm.AdcionarFuncionario(funcionario);
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
                MessageBox.Show($"Erro: {ex.InnerException?.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void RGVBanco_RowValidating(object sender, Telerik.Windows.Controls.GridViewRowValidatingEventArgs e)
        {
            try
            {
                CadastroFuncionarioViewModel vm = (CadastroFuncionarioViewModel)DataContext;
                if (!e.Row.IsInEditMode)
                    return;

                if (e.Row.Item is OperacionalTblDespDadoBancarioModel dadosBancario)
                {

                    bool sucesso = await vm.AdcionarDadosBancarioFuncionario(dadosBancario);
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
                MessageBox.Show($"Erro: {ex.InnerException?.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    public partial class CadastroFuncionarioViewModel : INotifyPropertyChanged
    {
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
                using Context context = new();

                // Supondo que seu DbContext se chame "SeuDbContext"
                var funcionariosComBancos = await context.DespFuncionarios
                    .Include(f => f.DadosBancarios)
                    .ToListAsync();

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
                using Context context = new();
                var funcionarioExistente = await context.DespFuncionarios.FindAsync(funcionario.cod_func);
                if (funcionarioExistente == null)
                    context.DespFuncionarios.Add(funcionario);
                else
                    context.Entry(funcionarioExistente).CurrentValues.SetValues(funcionario);

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

        public async Task<bool> AdcionarDadosBancarioFuncionario(OperacionalTblDespDadoBancarioModel dadosBancario)
        {
            try
            {
                using Context context = new();
                var dadosBancarioExistente = await context.DespDadoBancarios.FindAsync(dadosBancario.cod_func);
                if (dadosBancarioExistente == null)
                    context.DespDadoBancarios.Add(dadosBancario);
                else
                    context.Entry(dadosBancarioExistente).CurrentValues.SetValues(dadosBancario);

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

        public async Task<ObservableCollection<HtFuncionarioModel>> GetHtFuncionariosAsync()
        {
            try
            {
                using Context context = new();
                var funcionariosComBancos = await context.HtFuncionarios
                    .Where(f => f.data_demissao == null)
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

        public async Task<ObservableCollection<ComprasEmpresaModel>> GetEmpresasAsync()
        {
            try
            {
                using Context context = new();
                var empresas = await context.ComprasEmpresas
                    .ToListAsync();

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
