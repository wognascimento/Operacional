using Microsoft.EntityFrameworkCore;
using Operacional.DataBase;
using Operacional.DataBase.Models;
using Operacional.Views;
using Operacional.Views.Cronograma;
using Operacional.Views.Despesa;
using Operacional.Views.Transporte;
using Producao;
using Syncfusion.SfSkinManager;
using Syncfusion.Windows.Tools.Controls;
using Syncfusion.XlsIO;
using System.Collections.Specialized;
using System.Configuration;
using System.Diagnostics;
using System.Drawing;
using System.Windows;
using System.Windows.Input;
using Telerik.Windows.Controls;
using SizeMode = Syncfusion.SfSkinManager.SizeMode;

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

            //StyleManager.ApplicationTheme = new Windows11Theme();

            VisualStyles visualStyle = VisualStyles.Default;
            Enum.TryParse("Metro", out visualStyle);
            if (visualStyle != VisualStyles.Default)
            {
                SfSkinManager.ApplyStylesOnApplication = true;
                SfSkinManager.SetVisualStyle(this, visualStyle);
                SfSkinManager.ApplyStylesOnApplication = false;
            }

            SizeMode sizeMode = SizeMode.Default;
            Enum.TryParse("Default", out sizeMode);
            if (sizeMode != SizeMode.Default)
            {
                SfSkinManager.ApplyStylesOnApplication = true;
                SfSkinManager.SetSizeMode(this, sizeMode);
                SfSkinManager.ApplyStylesOnApplication = false;
            }

            var appSettings = ConfigurationManager.GetSection("appSettings") as NameValueCollection;
            if (appSettings[0].Length > 0)
                BaseSettings.Username = appSettings[0];

            txtUsername.Text = BaseSettings.Username;
            txtDataBase.Text = BaseSettings.Database;
        }


        private void OnAlterarUsuario(object sender, MouseButtonEventArgs e)
        {
            Login window = new();
            window.ShowDialog();

            try
            {
                var appSettings = ConfigurationManager.GetSection("appSettings") as NameValueCollection;
                BaseSettings.Username = appSettings[0];
                txtUsername.Text = BaseSettings.Username;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
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
                        txtDataBase.Text = BaseSettings.Database;
                        _mdi.Items.Clear();
                    }
                }
            });
        }

        private void _mdi_CloseAllTabs(object sender, CloseTabEventArgs e)
        {
            _mdi.Items.Clear();
        }

        private void _mdi_CloseButtonClick(object sender, CloseButtonEventArgs e)
        {
            var tab = (DocumentContainer)sender;
            _mdi.Items.Remove(tab.ActiveDocument);
        }

        public void adicionarFilho(object filho, string title, string name)
        {
            var doc = ExistDocumentInDocumentContainer(name);
            if (doc == null)
            {
                doc = (FrameworkElement?)filho;
                DocumentContainer.SetHeader(doc, title);
                doc.Name = name.ToLower();
                _mdi.Items.Add(doc);
            }
            else
            {
                //_mdi.RestoreDocument(doc as UIElement);
                _mdi.ActiveDocument = doc;
            }
        }

        private FrameworkElement ExistDocumentInDocumentContainer(string name_)
        {
            foreach (FrameworkElement element in _mdi.Items)
            {
                if (name_.ToLower() == element.Name)
                {
                    return element;
                }
            }
            return null;
        }

        private void OnTransporteMontagemClick(object sender, RoutedEventArgs e)
        {
            adicionarFilho(new TransporteMontagem(), "TRANSPORTES MONTAGEM", "TRANSPORTE_MONTAGEM");
        }

        private void OnTransporteDesmontagemClick(object sender, RoutedEventArgs e)
        {

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

                using Context context = new();
                var retorno = await context.QryCargasMontagem.AsNoTracking().ToListAsync();
                

                using ExcelEngine excelEngine = new();
                IApplication application = excelEngine.Excel;

                application.DefaultVersion = ExcelVersion.Xlsx;

                //Create a workbook
                IWorkbook workbook = application.Workbooks.Create(1);
                IWorksheet worksheet = workbook.Worksheets[0];
                //worksheet.IsGridLinesVisible = false;
                worksheet.ImportData(retorno, 1, 1, true);


                // Obter o intervalo da coluna "Data" e "data_de_expedicao"
                int rowCount = retorno.Count;
                // Colunas que devem mudar de cor quando A2 <> B2
                string[] columnsToFormat = { "A", "B", "C" };

                foreach (string col in columnsToFormat)
                {
                    string range = $"{col}2:{col}{rowCount + 1}"; // Exemplo: "A2:A6"

                    // Criar a formatação condicional para cada coluna
                    IConditionalFormats conditionalFormats = worksheet.Range[range].ConditionalFormats;
                    IConditionalFormat condition = conditionalFormats.AddCondition();

                    // Definir o tipo como Fórmula
                    condition.FormatType = ExcelCFType.Formula;
                    condition.FirstFormula = $"=$A2<>$B2";  // Baseando-se na diferença entre A2 e B2

                    // Definir a cor de fundo quando a condição for verdadeira
                    condition.BackColorRGB = Color.Yellow;
                }

                workbook.SaveAs("Impressos/QUERY_CARGAS_MONTAGEM.xlsx");
                Process.Start(new ProcessStartInfo("Impressos\\QUERY_CARGAS_MONTAGEM.xlsx")
                {
                    UseShellExecute = true
                });

                Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });



            }
            catch (DbUpdateException ex)
            {
                Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
                MessageBox.Show($"Erro: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)  // Para qualquer outro erro
            {
                Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
                MessageBox.Show($"Erro: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void OnQryCargaDesmontagemClick(object sender, RoutedEventArgs e)
        {
            try
            {
                using Context context = new();
                var retorno = await context.QryCargasDesmontagem.AsNoTracking().ToListAsync();


                using ExcelEngine excelEngine = new();
                IApplication application = excelEngine.Excel;

                application.DefaultVersion = ExcelVersion.Xlsx;

                //Create a workbook
                IWorkbook workbook = application.Workbooks.Create(1);
                IWorksheet worksheet = workbook.Worksheets[0];
                //worksheet.IsGridLinesVisible = false;
                worksheet.ImportData(retorno, 1, 1, true);

                workbook.SaveAs("Impressos/QUERY_CARGAS_DESMONTAGEM.xlsx");
                Process.Start(new ProcessStartInfo("Impressos\\QUERY_CARGAS_DESMONTAGEM.xlsx")
                {
                    UseShellExecute = true
                });

                Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });



            }
            catch (DbUpdateException ex)
            {
                Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
                MessageBox.Show($"Erro: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)  // Para qualquer outro erro
            {
                Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
                MessageBox.Show($"Erro: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
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

        private void OnOpenCadastroDespesaClick(object sender, RoutedEventArgs e)
        {
            adicionarFilho(new CadastroDespesa(), "CADASTRO DESPESAS", "CADASTRO_DESPESAS");
        }

        private void OnCronogramaClick(object sender, RoutedEventArgs e)
        {
            adicionarFilho(new Cronograma(), "CRONOGRAMA", "CRONOGRAMA");
        }
    }
}