using System.Configuration;
using System.DirectoryServices.AccountManagement;
using System.Windows;
using Telerik.Windows.Controls;

namespace Producao
{
    /// <summary>
    /// Interação lógica para Login.xam
    /// </summary>
    public partial class Login : RadWindow
    {
        public Login()
        {
            InitializeComponent();
            txtLogin.Focus();
        }

        private void OnSair(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void OnLogar(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(txtLogin.Text) && !string.IsNullOrWhiteSpace(txtSenha.Password))
            {
                try
                {
                    // ContextType.Domain já usa seu domínio padrão ou especifique "cipodominio.com.br"
                    using var ctx = new PrincipalContext(
                           ContextType.Domain,
                           "cipodominio.com.br");
                    if (!ctx.ValidateCredentials(txtLogin.Text, txtSenha.Password))
                        throw new Exception("Credenciais inválidas.");

                    // Atualiza config e fecha
                    var config = ConfigurationManager.OpenExeConfiguration("Operacional.dll");
                    config.AppSettings.Settings["Username"].Value = txtLogin.Text;
                    config.Save(ConfigurationSaveMode.Modified);
                    ConfigurationManager.RefreshSection("appSettings");

                    this.DialogResult = true;
                    this.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Falha na autenticação: {ex.Message}");
                }
            }
        }
    }
}
