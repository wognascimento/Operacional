using System.DirectoryServices;
using System;
using Telerik.Windows.Controls;
using System.Windows;
using System.Configuration;
using System.Collections.Specialized;

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

        private void OnSair(object sender, System.Windows.RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void OnLogar(object sender, System.Windows.RoutedEventArgs e)
        {

            if (!string.IsNullOrWhiteSpace(txtLogin.Text) && !string.IsNullOrWhiteSpace(txtSenha.Password))
            {
                try
                {
                    DirectoryEntry directoryEntry = new DirectoryEntry("LDAP://cipodominio.com.br:389", txtLogin.Text, txtSenha.Password);
                    DirectorySearcher directorySearcher = new DirectorySearcher(directoryEntry);
                    directorySearcher.Filter = "(SAMAccountName=" + txtLogin.Text + ")";
                    SearchResult searchResult = directorySearcher.FindOne();
    
                    Configuration config = ConfigurationManager.OpenExeConfiguration("Operacional.dll");
                    config.AppSettings.Settings["Username"].Value = txtLogin.Text;
                    config.Save(ConfigurationSaveMode.Modified);
                    ConfigurationManager.RefreshSection("appSettings");
                    this.DialogResult = true;
                    this.Close();

                }
                catch (Exception)
                {
                    MessageBox.Show("Usuário não encontrado!");
                }
            }
        }
    }
}
