using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Operacional.DataBase;
using Operacional.DataBase.Models;
using Operacional.DataBase.Models.DTOs.Api;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Telerik.Windows.Controls;


namespace Operacional.Views.EquipeExterna;

/// <summary>
/// Interação lógica para CadastroUsuario.xam
/// </summary>
public partial class CadastroUsuario : UserControl
{
    public CadastroUsuario()
    {
        InitializeComponent();
        DataContext = new CadastroUsuarioViewModel();
    }

    private async void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = Cursors.Wait; });
            CadastroUsuarioViewModel vm = (CadastroUsuarioViewModel)DataContext;
            // Carregar usuários ou outras operações iniciais
            vm.Usuarios = await vm.GetUsuariosAsync();
            vm.Equipes = await vm.GetEquipesAsync();
            Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro inesperado: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
        }
    }

    private async void UsuarioRowValidated(object sender, GridViewRowValidatedEventArgs e)
    {
        try
        {
            Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = Cursors.Wait; });
            CadastroUsuarioViewModel vm = (CadastroUsuarioViewModel)DataContext;
            var usuario = e.Row.Item as EquipeExternaUsuarioModel;
            if (usuario != null)
            {
                // Adicionar ou atualizar o usuário no banco de dados
                await vm.AddUsuarioAsync(usuario);
            }
            Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
        }
        catch (DbUpdateException ex)
        {
            MessageBox.Show($"Erro ao cadastrar usuário: {ex.InnerException?.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
        }
    }

    private async void OnEnviarWebClick(object sender, Telerik.Windows.RadRoutedEventArgs e)
    {
        
        var selectedItem = radUsuarios.CurrentItem;
        var dataObject = selectedItem as EquipeExternaUsuarioModel;

        await EnviarUsuárioAsync(dataObject);
    }

    private async Task EnviarUsuárioAsync(EquipeExternaUsuarioModel dataObject)
    {
        CadastroUsuarioViewModel vm = (CadastroUsuarioViewModel)DataContext;
        using HttpClient client = new();
        // URL da API
        string url = "https://rest-api.cipolatti.com.br/api/usuarios";

        // Dados a serem enviados
        var userData = new
        {
            name = dataObject.nome,
            dataObject.email,
            url_origem = "https://momades.cipolatti.com.br"
        };

        // Serializar os dados para JSON
        string json = JsonConvert.SerializeObject(userData);
        StringContent content = new(json, Encoding.UTF8, "application/json");

        try
        {
            Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = Cursors.Wait; });
            // Enviar a solicitação POST
            HttpResponseMessage response = await client.PostAsync(url, content);
            // Certifique-se de que a resposta seja bem-sucedida
            //response.EnsureSuccessStatusCode();
            if (response.IsSuccessStatusCode)
            {
                string responseData = await response.Content.ReadAsStringAsync();
                ApiResponse result = JsonConvert.DeserializeObject<ApiResponse>(responseData);

                dataObject.aux = result.Usuario.Id.ToString();
                await vm.AddUsuarioAsync(dataObject);

                //public async Task<ObservableCollection<EquipeExternaValoresPrevisaoEquipeModel>> GetEquipePrevisoesAsync(long id_equipe)

                

                

                //await PostClientesFasesAsync("https://rest-api.cipolatti.com.br", payload);

            }
            else if ((int)response.StatusCode == 409)
            {
                string errorResponse = await response.Content.ReadAsStringAsync();
                ErrorResponse error = JsonConvert.DeserializeObject<ErrorResponse>(errorResponse);
                MessageBox.Show($"{error.Message}", "Erro ao cadastrar usuário", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
            {
                MessageBox.Show($"{response.StatusCode}", "Erro ao cadastrar usuário", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
        }
        catch (HttpRequestException ex)
        {
            MessageBox.Show($"{ex.Message}", "Erro ao cadastrar usuário", MessageBoxButton.OK, MessageBoxImage.Error);
            Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
        }
    }

    public async Task PostClientesFasesAsync(string baseUrl, BulkRequest payload)
    {
        using var http = new HttpClient();
        http.BaseAddress = new Uri(baseUrl);
        http.DefaultRequestHeaders.Accept.Clear();
        http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var options = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
            // Não alterar a PropertyNamingPolicy pois usamos JsonPropertyName para snake_case
            PropertyNameCaseInsensitive = true
        };

        string json = System.Text.Json.JsonSerializer.Serialize(payload, options);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        HttpResponseMessage response = null;
        try
        {
            response = await http.PostAsync("/api/clientes-fases", content);

            var respBody = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                // Espera 201 conforme seu controller
                Console.WriteLine($"Sucesso ({(int)response.StatusCode}): {respBody}");
            }
            else
            {
                // Lidando com erros comuns (422 validação, 401, 403, 500)
                Console.WriteLine($"Erro ({(int)response.StatusCode}): {respBody}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Erro de requisição: " + ex.Message);
        }
    }
}

public partial class CadastroUsuarioViewModel : ObservableObject
{
    DataBaseSettings BaseSettings = DataBaseSettings.Instance;

    [ObservableProperty]
    private ObservableCollection<EquipeExternaEquipeModel> equipes;

    [ObservableProperty]
    private ObservableCollection<EquipeExternaUsuarioModel> usuarios;

    public async Task<ObservableCollection<EquipeExternaUsuarioModel>> GetUsuariosAsync()
    {
        using var _db = new Context();
        var result = await _db.EquipeExternaUsuarios
            .OrderBy(f => f.nome)
            .ToListAsync();
        return new ObservableCollection<EquipeExternaUsuarioModel>(result);
    }

    public async Task AddUsuarioAsync(EquipeExternaUsuarioModel usuario)
    {
        using var db = new Context();
        var usuarioExistente = await db.EquipeExternaUsuarios.FindAsync(usuario.id);
        if (usuarioExistente == null)
            await db.EquipeExternaUsuarios.AddAsync(usuario);
        else
            db.Entry(usuarioExistente).CurrentValues.SetValues(usuario);

        await db.SaveChangesAsync();
    }

    public async Task<ObservableCollection<EquipeExternaEquipeModel>> GetEquipesAsync()
    {
        /*
        using var connection = new NpgsqlConnection(BaseSettings.ConnectionString);

        string sql = @"
            SELECT 
	            tbl_valores_previsao_equipe.id_equipe, 
	            tblequipesext.equipe_e 
            FROM equipe_externa.tblequipesext 
            JOIN equipe_externa.tbl_valores_previsao_equipe ON equipe_externa.tblequipesext.id = equipe_externa.tbl_valores_previsao_equipe.id_equipe
            GROUP BY tbl_valores_previsao_equipe.id_equipe, tblequipesext.equipe_e 
            ORDER BY tblequipesext.equipe_e;
        ";
        var result = await connection.QueryAsync<EquipeDTO>(sql);

        return new ObservableCollection<EquipeDTO>([.. result]);
        */
        using var _db = new Context();
        var result = await _db.Equipes
            .OrderBy(f => f.equipe_e)
            .ToListAsync();
        return new ObservableCollection<EquipeExternaEquipeModel>(result);
    }

    public async Task<ObservableCollection<EquipeExternaValoresPrevisaoEquipeModel>> GetEquipePrevisoesAsync(long id_equipe)
    {
        using var _db = new Context();
        var result = await _db.EquipePrevisoes
            .OrderBy(f => f.cliente)
            .ThenBy(f => f.fase)
            .Where(f => f.id_equipe == id_equipe)
            .ToListAsync();


        var payload = new BulkRequest
        {
            Items =
                    [
                        new ClienteFaseDto {
                            IdAprovado = 1,
                            SiglaServ = "ABC",
                            DataInicio = DateTime.UtcNow.Date,
                            DataFim = null,
                            Fase = "inicial",
                            IdUser = 10
                        },
                        new ClienteFaseDto {
                            IdAprovado = 2,
                            SiglaServ = "XYZ",
                            DataInicio = null,
                            DataFim = DateTime.Parse("2025-09-01"),
                            Fase = "final",
                            IdUser = 11
                        }
                    ]
        };


        return new ObservableCollection<EquipeExternaValoresPrevisaoEquipeModel>(result);
    }

}
