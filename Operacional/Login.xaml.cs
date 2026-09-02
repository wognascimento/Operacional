using Operacional.DataBase;
using System;
using System.Configuration;
using System.DirectoryServices.AccountManagement;
using System.Windows;
using Telerik.Windows.Controls;

namespace Operacional
{
    public partial class Login : RadWindow
    {
        private readonly DataBaseSettings BaseSettings = DataBaseSettings.Instance;

        public Login()
        {
            InitializeComponent();
            txtLogin.Focus();
        }

        private void OnSair(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void OnLogar(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtLogin.Text) || string.IsNullOrWhiteSpace(txtSenha.Password))
                return;

            try
            {
                using var ctx = new PrincipalContext(
                    ContextType.Domain,
                    "192.168.0.254",
                    "cipodominio.com.br");

                if (!ctx.ValidateCredentials(txtLogin.Text, txtSenha.Password))
                    throw new Exception("Credenciais invalidas.");

                var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                if (config.AppSettings.Settings["Username"] == null)
                    config.AppSettings.Settings.Add("Username", txtLogin.Text);
                else
                    config.AppSettings.Settings["Username"].Value = txtLogin.Text;

                config.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection("appSettings");

                BaseSettings.Username = txtLogin.Text;
                BaseSettings.RefreshConnectionString();

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                Operacional.ErrorDialog.Show(ex, "Falha na autenticacao");
            }
        }
    }
}
