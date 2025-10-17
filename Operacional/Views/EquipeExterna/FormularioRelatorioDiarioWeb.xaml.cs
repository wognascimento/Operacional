using Telerik.Windows.Controls;

namespace Operacional.Views.EquipeExterna
{
    /// <summary>
    /// Interação lógica para FormularioRelatorioDiarioWeb.xam
    /// </summary>
    public partial class FormularioRelatorioDiarioWeb : RadWindow
    {
        public FormularioRelatorioDiarioWeb()
        {
            InitializeComponent();
        }

        public FormularioRelatorioDiarioWeb(object viewModel) : this()
        {
            DataContext = viewModel; // ou Content.DataContext = viewModel;
        }
    }
}
