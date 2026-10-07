using CommunityToolkit.Mvvm.ComponentModel;
using Dapper;
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
            Operacional.ErrorDialog.Show(ex, "Erro inesperado");
            Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
        }
    }

    private void UsuarioRowValidated(object sender, Telerik.Windows.Controls.GridViewRowValidatingEventArgs e)
    {
        if (e.Row?.IsInEditMode != true) return;
        if (e.Row.Item is not EquipeExternaUsuarioModel item) return;
        if (string.IsNullOrWhiteSpace(item.nome))
        {
            e.IsValid = false;
            e.ValidationResults.Add(new Telerik.Windows.Controls.GridViewCellValidationResult
            { ErrorMessage = "Preencha os campos obrigatorios." });
            return;
        }
        ValidatedGridSave.Save(sender, e, () => ((CadastroUsuarioViewModel)DataContext).AddUsuarioAsync(item));
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

    private bool enviandoAplicativo;

    private async void OnEnviarAplicativoClick(object sender, Telerik.Windows.RadRoutedEventArgs e)
    {
        if (enviandoAplicativo || radUsuarios.SelectedItem is not EquipeExternaUsuarioModel usuarioSelecionado) return;
        enviandoAplicativo = true;
        var enviados = new List<string>();
        string? pendente = null;
        string? idCriado = null;
        bool cadastroSolicitado = false;
        try
        {
            Mouse.OverrideCursor = Cursors.Wait;
            var service = new ClientesAplicativoService();
            var vm = (CadastroUsuarioViewModel)DataContext;
            if (usuarioSelecionado.id is null or <= 0)
                throw new InvalidOperationException("Salve o usuario antes de enviar ao aplicativo.");
            // Read the persisted identity instead of submitting uncommitted grid edits.
            var usuario = await vm.GetUsuarioAplicativoAsync(usuarioSelecionado.id.Value);
            if (!string.IsNullOrWhiteSpace(usuarioSelecionado.id_aplicativo) || !string.IsNullOrWhiteSpace(usuario.id_aplicativo))
            {
                var id = usuarioSelecionado.id_aplicativo ?? usuario.id_aplicativo!;
                await vm.SalvarIdAplicativoAsync(usuario.id.Value, id);
                usuarioSelecionado.id_aplicativo = id;
                MessageBox.Show("Usuario ja cadastrado no aplicativo. ID: " + id, "Enviar Aplicativo");
                return;
            }
            if (string.IsNullOrWhiteSpace(usuario.nome) ||
                !System.Net.Mail.MailAddress.TryCreate(usuario.email, out var endereco) || endereco.Address != usuario.email)
                throw new InvalidOperationException("Preencha e salve um nome e e-mail validos antes de enviar.");
            vm.ClienteIdsAplicativo = Array.Empty<string>();
            vm.FuncoesAplicativo = await vm.GetFuncoesAplicativoAsync(usuario.id_equipe);
            if (vm.FuncoesAplicativo.Count == 0 || vm.FuncoesAplicativo.Any(f => string.IsNullOrWhiteSpace(f.funcao) || f.valor < 0))
                throw new InvalidOperationException("A equipe precisa ter funcoes validas e valores nao negativos.");
            var clientes = await service.ConsultarPendentesAsync(BaseSettings.ConnectionString);
            Mouse.OverrideCursor = null;
            if (MessageBox.Show($"Solicitar cadastro de {usuario.nome} ({usuario.email}) com {vm.FuncoesAplicativo.Count} funcao(oes)?\n\nSe o e-mail ja existir com senha provisoria, a API reenviara a senha SEM atualizar funcoes ou clientes. Se houver duvida, cancele e confira o cadastro com o administrador.\n\nClientes novos: {clientes.Count(c => c.Novo)}, com coordenadas zero e raio 200. Clientes alterados: {clientes.Count(c => !c.Novo)}, preservando coordenadas e raio.",
                "Enviar Aplicativo", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;
            Mouse.OverrideCursor = Cursors.Wait;
            foreach (var cliente in clientes)
            {
                pendente = cliente.sigla;
                await service.EnviarAsync(cliente);
                if (cliente.Novo) enviados.Add(cliente.sigla);
                pendente = null;
            }
            vm.ClienteIdsAplicativo = await service.ConsultarIdsEquipeAsync(BaseSettings.ConnectionString, usuario.id_equipe);
            cadastroSolicitado = true;
            idCriado = await new UsuarioAplicativoService().CadastrarAsync(usuario.nome, usuario.email, vm.FuncoesAplicativo, vm.ClienteIdsAplicativo);
            usuarioSelecionado.id_aplicativo = idCriado;
            await vm.SalvarIdAplicativoAsync(usuario.id.Value, idCriado);
            var avisoCoordenadas = enviados.Count == 0 ? "" :
                "\n\nCorrigir latitude e longitude das siglas cadastradas com coordenadas zero:\n" + string.Join(", ", enviados);
            MessageBox.Show("Solicitacao aceita pela API. ID vinculado: " + idCriado +
                "\nSe o usuario ja possuia senha provisoria, o e-mail foi reenviado e as funcoes/clientes anteriores foram mantidos. A resposta nao distingue esse caso de um novo cadastro." + avisoCoordenadas,
                "Enviar Aplicativo", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            var aviso = enviados.Count == 0 ? "" :
                "\n\nCadastros confirmados com latitude e longitude zero. Corrigir as coordenadas das siglas:\n" + string.Join(", ", enviados);
            if (pendente != null)
                aviso += $"\n\nConfira o cadastro de {pendente} na API antes de tentar novamente: o resultado do envio nao foi confirmado.";
            if (idCriado != null)
                aviso += "\n\nUsuario criado na API, mas o ID nao foi salvo localmente: " + idCriado +
                    ". Tente novamente nesta tela para salvar somente o vinculo, sem cadastrar novamente.";
            else if (cadastroSolicitado)
                aviso += "\n\nConfira se o usuario foi criado na API antes de reenviar, para evitar novo cadastro ou e-mail.";
            MessageBox.Show("Envio interrompido. " + ex.Message + aviso,
                "Enviar Aplicativo", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        finally
        {
            Mouse.OverrideCursor = null;
            enviandoAplicativo = false;
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

    public IReadOnlyList<FuncaoAplicativoDto> FuncoesAplicativo { get; internal set; } = Array.Empty<FuncaoAplicativoDto>();
    public IReadOnlyList<string> ClienteIdsAplicativo { get; internal set; } = Array.Empty<string>();

    public async Task<EquipeExternaUsuarioModel> GetUsuarioAplicativoAsync(long id)
    {
        using var connection = new NpgsqlConnection(BaseSettings.ConnectionString);
        return await connection.QuerySingleOrDefaultAsync<EquipeExternaUsuarioModel>(
            "SELECT * FROM equipe_externa.tblusuario WHERE id = @id", new { id })
            ?? throw new InvalidOperationException("Usuario nao encontrado. Recarregue a tela.");
    }

    public async Task SalvarIdAplicativoAsync(long id, string idAplicativo)
    {
        using var connection = new NpgsqlConnection(BaseSettings.ConnectionString);
        var affected = await connection.ExecuteAsync(@"
            UPDATE equipe_externa.tblusuario SET id_aplicativo = @idAplicativo
            WHERE id = @id AND (NULLIF(BTRIM(id_aplicativo), '') IS NULL OR id_aplicativo = @idAplicativo);",
            new { id, idAplicativo });
        if (affected != 1)
            throw new InvalidOperationException("Usuario removido ou vinculado a outro ID de aplicativo. Recarregue a tela.");
    }

    public async Task<IReadOnlyList<FuncaoAplicativoDto>> GetFuncoesAplicativoAsync(long id_equipe)
    {
        using var connection = new NpgsqlConnection(BaseSettings.ConnectionString);
        var result = await connection.QueryAsync<FuncaoAplicativoDto>(@"
            SELECT funcao,
                   MAX(COALESCE(valor_ano_atual, 0)
                       + COALESCE(lanche, 0)
                       + COALESCE(transporte, 0)) AS valor
            FROM equipe_externa.qry_previsao_valores_cronograma
            WHERE id_equipe = @id_equipe
            GROUP BY funcao
            ORDER BY funcao;", new { id_equipe });
        return result.ToList();
    }

    [ObservableProperty]
    private ObservableCollection<EquipeExternaEquipeModel> equipes;

    [ObservableProperty]
    private ObservableCollection<EquipeExternaUsuarioModel> usuarios;

    public async Task<ObservableCollection<EquipeExternaUsuarioModel>> GetUsuariosAsync()
    {
        using var connection = new NpgsqlConnection(BaseSettings.ConnectionString);
        var result = await connection.QueryAsync<EquipeExternaUsuarioModel>(
            @"SELECT *
              FROM equipe_externa.tblusuario
              ORDER BY nome;");

        return new ObservableCollection<EquipeExternaUsuarioModel>(result);
    }

    public async Task AddUsuarioAsync(EquipeExternaUsuarioModel usuario)
    {
        using var connection = new NpgsqlConnection(BaseSettings.ConnectionString);

        if (usuario.id is null or <= 0)
        {
            usuario.id = await connection.ExecuteScalarAsync<long>(@"
                INSERT INTO equipe_externa.tblusuario
                (id_equipe, nome, email, aux)
                VALUES (@id_equipe, @nome, @email, @aux)
                RETURNING id;",
                usuario);

            return;
        }

        var linhas = await connection.ExecuteAsync(@"
            UPDATE equipe_externa.tblusuario
            SET id_equipe = @id_equipe,
                nome = @nome,
                email = @email,
                aux = @aux
            WHERE id = @id;",
            usuario);

        if (linhas != 1)
            {
                throw new InvalidOperationException("O registro nao existe mais ou foi alterado por outro usuario. Recarregue a tela; nenhum novo registro foi criado.");
            }
    }

    public async Task<ObservableCollection<EquipeExternaEquipeModel>> GetEquipesAsync()
    {
        using var connection = new NpgsqlConnection(BaseSettings.ConnectionString);
        var result = await connection.QueryAsync<EquipeExternaEquipeModel>(
            @"SELECT *
              FROM equipe_externa.tblequipesext
              ORDER BY equipe_e;");

        return new ObservableCollection<EquipeExternaEquipeModel>(result);
    }
}
