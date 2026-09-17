using BibliotecasSIG;
using Operacional.DataBase;
using Operacional.Localization;
using System.Diagnostics;
using System.Globalization;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Windows;
using System.Windows.Markup;
using Telerik.Windows.Controls;

namespace Operacional
{
    public partial class App : Application
    {
        private readonly DataBaseSettings BaseSettings = DataBaseSettings.Instance;
        private readonly string CURRENT_VERSION = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0.0";

        public string CurrentVersion => CURRENT_VERSION;

        public App()
        {
            BaseSettings.LoadFromConfiguration();
            DapperTypeHandlers.Register();
            GridFilterDefaults.Register();
            StyleManager.ApplicationTheme = new Office2016Theme();
            LocalizationManager.Manager = new LocalizationManager
            {
                ResourceManager = GridViewResources.ResourceManager
            };

            DispatcherUnhandledException += OnDispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            CultureInfo culture = new("pt-BR");
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;

            FrameworkElement.LanguageProperty.OverrideMetadata(
                typeof(FrameworkElement),
                new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(culture.IetfLanguageTag)));

            await CheckForUpdatesAsync();
        }

        public async Task CheckForUpdatesAsync(bool showUpToDate = false)
        {
            if (string.IsNullOrWhiteSpace(BaseSettings.UpdateInfoUrl))
                return;

            try
            {
                var updateChecker = new UpdateChecker(BaseSettings.UpdateInfoUrl, CURRENT_VERSION);
                var updateInfo = await updateChecker.CheckForUpdatesAsync();

                if (updateInfo == null)
                {
                    if (showUpToDate)
                        MessageBox.Show($"O sistema ja esta atualizado.\n\nVersao atual: {CURRENT_VERSION}", "Atualizacao do sistema", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var result = MessageBox.Show(
                    $"Nova versao disponivel!\n\n" +
                    $"Versao atual: {CURRENT_VERSION}\n" +
                    $"Nova versao: {updateInfo.updateVersion}\n\n" +
                    "Changelog:\n" +
                    string.Join("\n", updateInfo.changelog) +
                    "\n\nDeseja baixar a atualizacao?",
                    "Atualizacao Disponivel",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Information);

                if (result != MessageBoxResult.Yes)
                    return;

                string jsonData = JsonSerializer.Serialize(updateInfo);
                string arguments = $"\"{jsonData.Replace("\"", "\\\"")}\" \"Operacional.exe\"";
                Process.Start("Update.exe", arguments);
                Shutdown();
            }
            catch (HttpRequestException ex)
            {
                ErrorDialog.Show(ex, "Erro ao verificar atualizacoes");
            }
            catch (Exception ex)
            {
                ErrorDialog.Show(ex, "Erro ao verificar atualizacoes");
            }
        }

        private void OnDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            ErrorDialog.Show(e.Exception, "Erro inesperado");
            e.Handled = true;
        }

        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                ErrorDialog.Show(ex, "Erro critico", MessageBoxImage.Stop);
            }
        }
    }
}
