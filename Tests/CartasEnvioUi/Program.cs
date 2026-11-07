using ClosedXML.Excel;
using Operacional.DataBase;
using Operacional.Views.Cartas;
using System.Collections;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Telerik.Windows.Controls;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        if (args.Length != 2) throw new ArgumentException("Informe modelo e diretorio de saida.");
        var output = Path.GetFullPath(args[1]);
        Directory.CreateDirectory(output);
        var modelo = Path.Combine(output, "modelo.docx");
        File.Copy(args[0], modelo, true);
        var excel = Path.Combine(output, "dados.xlsx");
        using (var book = new XLWorkbook())
        {
            book.AddWorksheet("dados").Cell(1, 1).Value = "sigla";
            book.SaveAs(excel);
        }
        DataBaseSettings.Instance.Database = "2026";
        DataBaseSettings.Instance.Username = "TESTE";
        DataBaseSettings.Instance.ConnectionString = "Host=127.0.0.1;Port=1;Database=teste;Username=teste;Timeout=1";
        _ = new Application();
        StyleManager.ApplicationTheme = new Office2016Theme();
        var window = new EnvioCartasWindow(modelo, excel, "INICIO_MONTAGEM");
        ((TextBox)window.FindName("Remetente")).Text = "usuario@example.com";
        ((TextBox)window.FindName("Resposta")).Text = "equipe@example.com";
        ((TextBlock)window.FindName("Situacao")).Text = "3 destinatarios. 1 pendente selecionado.";
        var grid = (RadGridView)window.FindName("Destinatarios");
        var tipo = typeof(EnvioCartasWindow).Assembly.GetType("Operacional.Utils.Cartas.CartaDestinatario")!;
        var lista = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(tipo))!;
        foreach (var status in new[] { "pendente", "enviado", "incerto" })
        {
            var item = Activator.CreateInstance(tipo)!;
            foreach (var (nome, valor) in new (string, object)[]
            {
                ("Sigla", "SIG TESTE"), ("Email", status + "@example.com"), ("Contato", "Contato de teste"),
                ("Status", status), ("Selecionado", status == "pendente"), ("Copia", status == "enviado" ? "salva" : ""),
                ("Detalhe", status == "incerto" ? "Confira o resultado antes de reenviar." : "")
            })
                tipo.GetProperty(nome)!.SetValue(item, valor);
            lista.Add(item);
        }
        grid.ItemsSource = lista;
        // Nunca mostrar a janela: Loaded consultaria o banco. Renderizar apenas o conteudo desconectado.
        var content = (FrameworkElement)window.Content;
        window.Content = null;
        ((Panel)content).Background = Brushes.White;
        foreach (var (width, height) in new[] { (1180, 700), (680, 600) })
        {
            content.Measure(new Size(width, height));
            content.Arrange(new Rect(0, 0, width, height));
            content.UpdateLayout();
            content.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.ApplicationIdle);
            content.UpdateLayout();
            var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(content);
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using var file = File.Create(Path.Combine(output, $"cartas-{width}.png"));
            encoder.Save(file);
        }
        Console.WriteLine("Screenshots criadas sem consulta ao banco ou conexao de email: " + output);
    }
}
