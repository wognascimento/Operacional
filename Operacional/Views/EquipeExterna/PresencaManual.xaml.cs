using Dapper;
using Npgsql;
using NpgsqlTypes;
using Operacional.DataBase;
using System.Data;
using System.Windows;
using System.Windows.Controls;

namespace Operacional.Views.EquipeExterna;

public partial class PresencaManual : UserControl
{
    private bool inicializado;
    private bool salvando;
    private int versaoEquipe;
    private static readonly string[] Fases = ["MONTAGEM", "MANUTENÇÃO PROGRAMADA", "MANUTENÇÃO CORRETIVA", "DESMONTAGEM"];

    public PresencaManual()
    {
        InitializeComponent();
        fase.ItemsSource = Fases;
        data.SelectedDate = DateTime.Today;
    }

    private static NpgsqlConnection Conectar() => new(DataBaseSettings.Instance.ConnectionString);

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (inicializado) return;
        inicializado = true;
        formulario.IsEnabled = false;
        grid.IsBusy = true;
        try
        {
            await using var db = Conectar();
            equipe.ItemsSource = (await db.QueryAsync<EquipeOpcao>("""
                SELECT e.equipe_e, ve.id_equipe
                FROM equipe_externa.tbl_valores_previsao_equipe ve
                JOIN equipe_externa.tblequipesext e ON ve.id_equipe = e.id
                GROUP BY e.equipe_e, ve.id_equipe
                ORDER BY e.equipe_e;
                """)).ToList();
            funcao.ItemsSource = (await db.QueryAsync<string>(
                "SELECT funcao FROM operacional.tblfuncoes_cronograma ORDER BY funcao;")).ToList();
        }
        catch (Exception ex)
        {
            inicializado = false;
            MostrarErro(ex);
        }
        finally
        {
            formulario.IsEnabled = true;
            grid.IsBusy = false;
        }
    }

    private async void OnEquipeChanged(object sender, SelectionChangedEventArgs e)
    {
        var versao = ++versaoEquipe;
        cliente.ItemsSource = null;
        grid.ItemsSource = null;
        if (equipe.SelectedItem is not EquipeOpcao selecionada) return;
        cliente.IsEnabled = false;
        grid.IsBusy = true;
        try
        {
            await using var db = Conectar();
            var clientes = (await db.QueryAsync<ClienteOpcao>("""
                SELECT ve.id_equipe, ap.id_aprovado, ve.cliente
                FROM equipe_externa.tbl_valores_previsao_equipe ve
                JOIN equipe_externa.tblequipesext e ON ve.id_equipe = e.id
                JOIN producao.t_aprovados ap ON ve.cliente = ap.sigla_serv
                WHERE ve.id_equipe = @id_equipe
                GROUP BY ve.id_equipe, ap.id_aprovado, ve.cliente
                ORDER BY ve.cliente;
                """, new { selecionada.id_equipe })).ToList();
            if (versao != versaoEquipe) return;
            cliente.ItemsSource = clientes;
            await CarregarGridAsync(selecionada.id_equipe, versao);
        }
        catch (Exception ex)
        {
            if (versao == versaoEquipe) MostrarErro(ex);
        }
        finally
        {
            if (versao == versaoEquipe)
            {
                cliente.IsEnabled = true;
                grid.IsBusy = false;
            }
        }
    }

    private async Task CarregarGridAsync(int idEquipe, int versao)
    {
        await using var db = Conectar();
        await db.OpenAsync();
        await using var command = new NpgsqlCommand("""
            SELECT p.*,
                (SELECT a.sigla_serv FROM producao.t_aprovados a
                 WHERE a.id_aprovado = p.id_aprovado) AS sigla
            FROM equipe_externa.tbl_presenca_equipe p
            WHERE p.id_equipe = @equipe ORDER BY p.data DESC, p.id DESC;
            """, db);
        command.Parameters.AddWithValue("equipe", idEquipe);
        await using var reader = await command.ExecuteReaderAsync();
        var tabela = new DataTable();
        tabela.Load(reader);
        if (versao == versaoEquipe) grid.ItemsSource = tabela.DefaultView;
    }

    private async void OnAtualizar(object sender, RoutedEventArgs e)
    {
        if (salvando || equipe.SelectedItem is not EquipeOpcao selecionada) return;
        try { await CarregarGridAsync(selecionada.id_equipe, versaoEquipe); }
        catch (Exception ex) { MostrarErro(ex); }
    }

    private async void OnExcluirSelecionadas(object sender, RoutedEventArgs e)
    {
        if (salvando || grid.IsBusy || equipe.SelectedItem is not EquipeOpcao eq) return;
        var ids = grid.SelectedItems.OfType<DataRowView>()
            .Select(item => item.Row.Field<int>("id")).Distinct().ToArray();
        if (ids.Length == 0)
        {
            MessageBox.Show("Selecione as presenças que deseja excluir.", "Presença manual");
            return;
        }
        if (MessageBox.Show($"Excluir os registros selecionados da equipe {eq.equipe_e}?\n\nTotal: {ids.Length}",
            "Excluir presenças", MessageBoxButton.YesNo, MessageBoxImage.Warning,
            MessageBoxResult.No) != MessageBoxResult.Yes) return;

        salvando = true;
        formulario.IsEnabled = false;
        grid.IsBusy = true;
        var excluido = false;
        try
        {
            await using var db = Conectar();
            await db.OpenAsync();
            await using (var transacao = await db.BeginTransactionAsync())
            {
                var linhas = await db.ExecuteAsync("""
                    DELETE FROM equipe_externa.tbl_presenca_equipe
                    WHERE id_equipe = @equipe AND id = ANY(@ids);
                    """, new { equipe = eq.id_equipe, ids }, transacao);
                if (linhas != ids.Length)
                    throw new InvalidOperationException("A seleção mudou no banco. Atualize a lista antes de tentar novamente.");
                await transacao.CommitAsync();
            }
            excluido = true;
            await CarregarGridAsync(eq.id_equipe, versaoEquipe);
        }
        catch (Exception ex)
        {
            MessageBox.Show((excluido ? "Registros excluídos. Atualize a lista." : "Não foi possível excluir a seleção.")
                + "\n\n" + ex.Message, "Presença manual", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            salvando = false;
            formulario.IsEnabled = true;
            grid.IsBusy = false;
        }
    }

    private async void OnSalvar(object sender, RoutedEventArgs e)
    {
        if (salvando) return;
        if (equipe.SelectedItem is not EquipeOpcao eq ||
            cliente.SelectedItem is not ClienteOpcao cli || cli.id_aprovado <= 0 ||
            fase.SelectedItem is not string faseEscolhida ||
            funcao.SelectedItem is not string funcaoEscolhida ||
            string.IsNullOrWhiteSpace(funcaoEscolhida) ||
            data.SelectedDate is not DateTime dia ||
            numeroPessoas.Value is not double quantidade || quantidade < 1 || quantidade > int.MaxValue ||
            quantidade != Math.Truncate(quantidade))
        {
            MessageBox.Show("Selecione equipe, cliente, fase, função e data e informe uma quantidade de pessoas maior que zero.",
                "Presença manual", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        salvando = true;
        formulario.IsEnabled = false;
        grid.IsBusy = true;
        var salvo = false;
        try
        {
            await using var db = Conectar();
            await db.OpenAsync();
            await using (var transaction = await db.BeginTransactionAsync())
            {
                // A tabela não possui sequence/identity; serializa a geração do ID e a inserção.
                await using var command = new NpgsqlCommand("""
                    LOCK TABLE equipe_externa.tbl_presenca_equipe IN EXCLUSIVE MODE;
                    INSERT INTO equipe_externa.tbl_presenca_equipe
                        (id, id_equipe, id_aprovado, fase, funcao, data, pessoas, extra, created_at, updated_at)
                    SELECT COALESCE(MAX(id), 0) + 1, @equipe, @cliente, @fase, @funcao,
                           @data, @pessoas, @extra, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP
                    FROM equipe_externa.tbl_presenca_equipe;
                    """, db, transaction);
                command.Parameters.AddWithValue("equipe", eq.id_equipe);
                command.Parameters.AddWithValue("cliente", cli.id_aprovado);
                command.Parameters.AddWithValue("fase", faseEscolhida);
                command.Parameters.AddWithValue("funcao", funcaoEscolhida);
                command.Parameters.AddWithValue("data", NpgsqlDbType.Date, dia.Date);
                command.Parameters.AddWithValue("pessoas", (int)quantidade);
                command.Parameters.AddWithValue("extra", extra.IsChecked == true);
                await command.ExecuteNonQueryAsync();
                await transaction.CommitAsync();
            }
            salvo = true;
            numeroPessoas.Value = null;
            extra.IsChecked = false;
            await CarregarGridAsync(eq.id_equipe, versaoEquipe);
        }
        catch (Exception ex)
        {
            MessageBox.Show((salvo
                ? "Presença salva, mas não foi possível atualizar a lista. Não lance novamente.\n\n"
                : "Não foi possível salvar a presença.\n\n") + ex.Message,
                "Presença manual", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            salvando = false;
            formulario.IsEnabled = true;
            grid.IsBusy = false;
            if (salvo) numeroPessoas.Focus();
        }
    }

    private static void MostrarErro(Exception ex) =>
        MessageBox.Show("Não foi possível carregar os dados.\n\n" + ex.Message,
            "Presença manual", MessageBoxButton.OK, MessageBoxImage.Error);

    public sealed class EquipeOpcao
    {
        public int id_equipe { get; set; }
        public string equipe_e { get; set; } = "";
    }

    public sealed class ClienteOpcao
    {
        public int id_aprovado { get; set; }
        public string cliente { get; set; } = "";
    }
}
