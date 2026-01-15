using CommunityToolkit.Mvvm.ComponentModel;
using Dapper;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Npgsql;
using Operacional.DataBase;
using Operacional.DataBase.Models;
using Operacional.DataBase.Models.DTOs.Api;
using SharpDX;
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
    DataBaseSettings BaseSettings = DataBaseSettings.Instance;

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

    private void RadContextMenu_Opening(object sender, Telerik.Windows.RadRoutedEventArgs e)
    {
        var menu = (Telerik.Windows.Controls.RadContextMenu)sender;

        // Verifica em qual linha o menu foi aberto
        var row = menu.GetClickedElement<Telerik.Windows.Controls.GridView.GridViewRow>();
        if (row != null)
        {
            radUsuarios.SelectedItem = row.Item;
        }
        else
        {
            // Cancela se não clicar em uma linha
            e.Handled = true;
        }
    }

    private async void OnEnviarWebClick(object sender, Telerik.Windows.RadRoutedEventArgs e)
    {
        //var selectedItem = radUsuarios.CurrentCellInfo.Item;
        //var dataObject = selectedItem as EquipeExternaUsuarioModel;

        if (radUsuarios.SelectedItem is not EquipeExternaUsuarioModel itemSelecionado) return;

        await EnviarUsuárioAsync(itemSelecionado);
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
                await PostDadosEquipeWebAsync(long.Parse(dataObject.aux), dataObject.id_equipe);
                MessageBox.Show($"Usuário {dataObject.nome} cadastrado com sucesso!\nID: {result.Usuario.Id}", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else if ((int)response.StatusCode == 409)
            {
                string errorResponse = await response.Content.ReadAsStringAsync();
                ErrorResponse error = JsonConvert.DeserializeObject<ErrorResponse>(errorResponse);
                MessageBox.Show($"{error.Message}", "Erro ao cadastrar usuário", MessageBoxButton.OK, MessageBoxImage.Error);

                if(dataObject.aux == null)
                {
                    Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
                    MessageBox.Show($"Dados da equipe inválidos ou alterado após orçamento.", "Erro ao cadastrar usuário", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                await PostDadosEquipeWebAsync(long.Parse(dataObject.aux), dataObject.id_equipe);
                
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

    public async Task PostDadosEquipeWebAsync(long id_user, long id_equipe)
    {
        using var connection = new NpgsqlConnection(BaseSettings.ConnectionString);
        var parametros = new { id_user, id_equipe };
        /*clientes_fase*/
        var clientesFase = new List<ClienteFaseDto>();
        if (id_equipe == 237)
        {
            clientesFase = connection.Query<ClienteFaseDto>(
                @"SELECT  
                    qry_previsao_valores_cronograma.id_aprovado AS id_aprovado,
                    qry_previsao_valores_cronograma.sigla AS sigla_serv,
                    t_data_efetiva.data_inicio_montagem AS data_inicio,
                    t_data_efetiva.data_inicio_montagem + max(qry_previsao_valores_cronograma.qtd_noites)::INTEGER AS data_fim,
                    qry_previsao_valores_cronograma.fase AS fase,
                    @id_user AS id_user
                FROM equipe_externa.qry_previsao_valores_cronograma
                INNER JOIN operacional.t_data_efetiva 
                    ON qry_previsao_valores_cronograma.sigla = t_data_efetiva.siglaserv
                WHERE qry_previsao_valores_cronograma.fase = 'MONTAGEM'
                GROUP BY 
                    qry_previsao_valores_cronograma.id_aprovado, 
                    qry_previsao_valores_cronograma.sigla, 
                    qry_previsao_valores_cronograma.fase, 
                    t_data_efetiva.data_inicio_montagem
                UNION
                SELECT  
                    qry_previsao_valores_cronograma.id_aprovado AS id_aprovado,
                    qry_previsao_valores_cronograma.sigla AS sigla_serv,
                    t_data_efetiva.data_inicio_desmontagem AS data_inicio,
                    t_data_efetiva.data_inicio_desmontagem + max(qry_previsao_valores_cronograma.qtd_noites)::INTEGER AS data_fim,
                    qry_previsao_valores_cronograma.fase AS fase,
                    @id_user AS id_user
                FROM equipe_externa.qry_previsao_valores_cronograma
                INNER JOIN operacional.t_data_efetiva 
                    ON qry_previsao_valores_cronograma.sigla = t_data_efetiva.siglaserv
                WHERE qry_previsao_valores_cronograma.fase = 'DESMONTAGEM'
                GROUP BY 
                    qry_previsao_valores_cronograma.id_aprovado, 
                    qry_previsao_valores_cronograma.sigla, 
                    qry_previsao_valores_cronograma.fase, 
                    t_data_efetiva.data_inicio_desmontagem;", parametros).ToList();
        }
        else
        {
            clientesFase = connection.Query<ClienteFaseDto>(
                @"SELECT  
                    qry_previsao_valores_cronograma.id_aprovado AS id_aprovado,
                    qry_previsao_valores_cronograma.sigla AS sigla_serv,
                    t_data_efetiva.data_inicio_montagem AS data_inicio,
                    t_data_efetiva.data_inicio_montagem + max(qry_previsao_valores_cronograma.qtd_noites)::INTEGER AS data_fim,
                    qry_previsao_valores_cronograma.fase AS fase,
                    @id_user AS id_user
                FROM equipe_externa.qry_previsao_valores_cronograma
                INNER JOIN operacional.t_data_efetiva 
                    ON qry_previsao_valores_cronograma.sigla = t_data_efetiva.siglaserv
                WHERE qry_previsao_valores_cronograma.fase = 'MONTAGEM' AND id_equipe = @id_equipe
                GROUP BY 
                    qry_previsao_valores_cronograma.id_aprovado, 
                    qry_previsao_valores_cronograma.sigla, 
                    qry_previsao_valores_cronograma.fase, 
                    t_data_efetiva.data_inicio_montagem
                UNION
                SELECT  
                    qry_previsao_valores_cronograma.id_aprovado AS id_aprovado,
                    qry_previsao_valores_cronograma.sigla AS sigla_serv,
                    t_data_efetiva.data_inicio_desmontagem AS data_inicio,
                    t_data_efetiva.data_inicio_desmontagem + max(qry_previsao_valores_cronograma.qtd_noites)::INTEGER AS data_fim,
                    qry_previsao_valores_cronograma.fase AS fase,
                    @id_user AS id_user
                FROM equipe_externa.qry_previsao_valores_cronograma
                INNER JOIN operacional.t_data_efetiva 
                    ON qry_previsao_valores_cronograma.sigla = t_data_efetiva.siglaserv
                WHERE qry_previsao_valores_cronograma.fase = 'DESMONTAGEM' AND id_equipe = @id_equipe
                GROUP BY 
                    qry_previsao_valores_cronograma.id_aprovado, 
                    qry_previsao_valores_cronograma.sigla, 
                    qry_previsao_valores_cronograma.fase, 
                    t_data_efetiva.data_inicio_desmontagem;", parametros).ToList();
        }
        /*liberacao_equipe*/
        var liberacaoEquipe = connection.Query<LiberacaoEquipeDto>(
            @"SELECT 
                qry_previsao_valores_cronograma.id_aprovado AS id_aprovado,
                qry_previsao_valores_cronograma.sigla AS sigla_serv,
                qry_previsao_valores_cronograma.fase AS fase,
                qry_previsao_valores_cronograma.funcao AS funcao,
                qry_previsao_valores_cronograma.qtd_pessoas AS qtd_pessoas,
                qry_previsao_valores_cronograma.valor_ano_atual AS valor_ano_atual,
                qry_previsao_valores_cronograma.lanche AS lanche,
                qry_previsao_valores_cronograma.transporte AS transporte,
                @id_user AS id_user
            FROM equipe_externa.qry_previsao_valores_cronograma
            WHERE (fase = 'MONTAGEM' OR fase = 'DESMONTAGEM') 
              AND valor_ano_atual > 0 
              AND id_equipe = @id_equipe;", parametros).ToList();

        /*liberacao_manutencao_equipe*/
        var liberacaoManutencaoEquipe = connection.Query<LiberacaoManutencaoEquipeDto>(
            @"SELECT 
                id_aprovado AS id_aprovado, 
                sigla AS sigla_serv, 
                fase AS fase, 
                funcao AS funcao, 
                qtd_pessoas AS qtd_pessoas, 
                valor_ano_atual AS valor_ano_atual, 
                lanche AS lanche, 
                transporte AS transporte, 
                data AS data,
                @id_user AS id_user
            FROM equipe_externa.qry_funcoes_equipe_usuario_manutencao
            WHERE id_equipe = @id_equipe;", parametros).ToList();

        BulkPayload payload = new();

        if(liberacaoManutencaoEquipe.Count == 0)
        {
            payload = new BulkPayload
            {
                clientes_fase = clientesFase,
                liberacao_equipe = liberacaoEquipe
            };
        }
        else
        {
            payload = new BulkPayload
            {
                clientes_fase = clientesFase,
                liberacao_equipe = liberacaoEquipe,
                liberacao_manutencao_equipe = liberacaoManutencaoEquipe
            };
        }

        var json = JsonConvert.SerializeObject(payload, Formatting.Indented);

        using var httpClient = new HttpClient();
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await httpClient.PostAsync("https://rest-api.cipolatti.com.br/api/bulk/all", content);

        if (response.IsSuccessStatusCode)
        {
            Console.WriteLine("✅ Dados enviados com sucesso!");
            MessageBox.Show($"Dados da equipe enviados com sucesso!", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        else
        {
            Console.WriteLine($"❌ Erro: {response.StatusCode}");
            var error = await response.Content.ReadAsStringAsync();
            Console.WriteLine(error);
            MessageBox.Show($"Erro ao enviar dados da equipe: {response.StatusCode}\n{error}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
        }

    }

    
    /*
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
*/
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
    /*
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
    */
}
