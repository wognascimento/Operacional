using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dapper;
using Npgsql;
using Operacional.DataBase;
using Operacional.DataBase.Models;
using Operacional.DataBase.Models.DTOs;
using Operacional.DataBase.Models.DTOs.Api;
using Operacional.Views.EquipeExterna.Consultas;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Telerik.Windows.Controls;
using static System.Net.WebRequestMethods;

namespace Operacional.Views.EquipeExterna;

/// <summary>
/// Interação lógica para RelatorioDiarioWeb.xam
/// </summary>
public partial class RelatorioDiarioWeb : UserControl
{
    public RelatorioDiarioWeb()
    {
        InitializeComponent();
        DataContext = new RelatorioDiarioWebViewModel();
        this.Loaded += RelatorioDiarioWeb_Loaded;
    }

    private async void RelatorioDiarioWeb_Loaded(object sender, RoutedEventArgs e)
    {
        RelatorioDiarioWebViewModel vm = (RelatorioDiarioWebViewModel)DataContext;
        vm.IsBusy = true;
        var resultado = await vm.GetAllAsync();
        await vm.InsertBatchAsync(resultado);
        await vm.RelatoriosAsync();
        //Console.WriteLine(resultado.Message);
        vm.IsBusy = false;
    }
}

public partial class RelatorioDiarioWebViewModel : ObservableObject
{
    DataBaseSettings BaseSettings = DataBaseSettings.Instance;
    public ICommand AbrirDetalhesCommand { get; }

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private ObservableCollection<RelatorioDiarioModel> relatorios;

    [ObservableProperty]
    private RelatorioDiarioModel? relatorio;

    public RelatorioDiarioWebViewModel()
    {
        AbrirDetalhesCommand = new RelayCommand<RelatorioDiarioModel>(AbrirDetalhes);
    }

    private void AbrirDetalhes(RelatorioDiarioModel item)
    {
        Relatorio = item;
        var ownerWindow = Application.Current.MainWindow;
        RadWindow dlg = new FormularioRelatorioDiarioWeb(this)
        {
            Header = $"Detalhes do Relatório {item.sigla_serv}",
            DataContext = this,
            Owner = ownerWindow,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            
        };
        dlg.ShowDialog();
    }

    public async Task<List<RelatorioWebDto>> GetAllAsync(CancellationToken ct = default)
    {
        HttpClient _http = new();
        var url = "https://rest-api.cipolatti.com.br/api/relatorios/all";
        using var res = await _http.GetAsync(url, ct);
        res.EnsureSuccessStatusCode();

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        await using var stream = await res.Content.ReadAsStreamAsync(ct);
        var list = await JsonSerializer.DeserializeAsync<List<RelatorioWebDto>>(stream, options, ct);
        return list ?? [];
    }

    public async Task InsertBatchAsync(List<RelatorioWebDto> items, CancellationToken ct = default)
    {
        const string upsertSql = @"
        INSERT INTO equipe_externa.tblrelatorio_diario (
            id, user_id, assistente, coordenador, data, descricao, externa_lider1, externa_lider2, externa_lider3, externa_pessoas1, externa_pessoas2, externa_pessoas3, fase, interna_entrada, interna_pessoas, interna_saida, mensagem, noite, id_aprovado, sigla_serv, tipo, created_at, updated_at
        ) VALUES (
            @id, @user_id, @assistente, @coordenador, @data, @descricao, @externa_lider1, @externa_lider2, @externa_lider3, @externa_pessoas1, @externa_pessoas2, @externa_pessoas3, @fase, @interna_entrada, @interna_pessoas, @interna_saida, @mensagem, @noite, @id_aprovado, @sigla_serv, @tipo, @created_at, @updated_at
        );";

        const string sql = @"SELECT COALESCE(MAX(id), 0) FROM equipe_externa.tblrelatorio_diario;";

        await using var conn = new NpgsqlConnection(BaseSettings.ConnectionString);
        await conn.OpenAsync(ct);

        await using var tran = await conn.BeginTransactionAsync(ct);
        try
        {
            var cmd = new CommandDefinition(sql, cancellationToken: ct);
            var maxId = await conn.QuerySingleAsync<int>(cmd);

            foreach (var item in items.Where(w => w.id > maxId))
            {
                await conn.QueryAsync(upsertSql, item, transaction: tran);
            }
            await tran.CommitAsync(ct);
        }
        catch
        {
            await tran.RollbackAsync(ct);
            throw;
        }
    }

    public async Task RelatoriosAsync(CancellationToken ct = default)
    {

        const string sql = @"SELECT     
                                    id, 
                                    user_id, 
                                    assistente, 
                                    coordenador, 
                                    data, 
                                    descricao, 
                                    externa_lider1, 
                                    externa_lider2, 
                                    externa_lider3, 
                                    externa_pessoas1, 
                                    externa_pessoas2, 
                                    externa_pessoas3, 
                                    fase, 
                                    interna_entrada, 
                                    interna_pessoas, 
                                    interna_saida, 
                                    mensagem, 
                                    noite, 
                                    id_aprovado, 
                                    sigla_serv, 
                                    tipo, 
                                    enviado, 
                                    ""inseridoCipolatti"", 
                                    created_at, 
                                    updated_at
	                         FROM equipe_externa.tblrelatorio_diario
                             ORDER BY id;";

        await using var conn = new NpgsqlConnection(BaseSettings.ConnectionString);
        await conn.OpenAsync(ct);

        await using var tran = await conn.BeginTransactionAsync(ct);
        try
        {
            var cmd = new CommandDefinition(sql, cancellationToken: ct);
            Relatorios = new ObservableCollection<RelatorioDiarioModel>(await conn.QueryAsync<RelatorioDiarioModel>(cmd));


            await tran.CommitAsync(ct);
        }
        catch
        {
            await tran.RollbackAsync(ct);
            throw;
        }
    }
}