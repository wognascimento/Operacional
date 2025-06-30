using Microsoft.EntityFrameworkCore;

namespace Operacional.DataBase.Models;

public partial class Context : DbContext
{
    public Context(){}

    public Context(DbContextOptions<Context> options): base(options){}

    public virtual DbSet<qryfrmtransp> qryfrmtransps { get; set; }
    public virtual DbSet<tbl_cargas_montagem> cargasmontagens { get; set; }
    public virtual DbSet<tbltranportadora> tbltranportadoras { get; set; }
    public virtual DbSet<TransporteMontagemModel> transporteMontagens { get; set; }
    public virtual DbSet<DataEfetivaModel> DatasEfetiva { get; set; }
    public virtual DbSet<QryDataEfetivaModel> QryDatasEfetiva { get; set; }
    public virtual DbSet<QryfrmtranspDetalheModel> QryCargasMontagem { get; set; }
    public virtual DbSet<QryTranspDesmontDetalheModel> QryCargasDesmontagem { get; set; }
    public virtual DbSet<OperacionalDespRelatorioModel> DespRelatorios { get; set; }
    public virtual DbSet<OperacionalTDespFuncionarioModel> DespFuncionarios { get; set; }
    public virtual DbSet<OperacionalTblDespDadoBancarioModel> DespDadoBancarios { get; set; }
    public virtual DbSet<HtFuncionarioModel> HtFuncionarios { get; set; }
    public virtual DbSet<ComprasEmpresaModel> ComprasEmpresas { get; set; }
    public virtual DbSet<OperacionalRelatorioDespesaModel> RelatorioDespesas { get; set; }
    public virtual DbSet<OperacionalRelatorioObservacaoModel> RelatorioDespesaObservacoes { get; set; }
    public virtual DbSet<OperacionalAdiantamentoModel> RelatorioDespesaAdiantamentos { get; set; }
    public virtual DbSet<OperacionalRelatorioDespesasDetalheModel> RelatorioDespesasDetalhes { get; set; }
    public virtual DbSet<OperacionalFaseModel> OperacionalFases { get; set; }
    public virtual DbSet<OperacionalBaseCustoModel> OperacionalBaseCustos { get; set; }
    public virtual DbSet<OperacionalEmpresaModel> OperacionalEmpresas { get; set; }
    public virtual DbSet<OperacionalNoitescronogPessoaFuncaoModel> OperacionalNoitescronogPessoas { get; set; }
    public virtual DbSet<OperacionalFuncoesCronogramaModel> OperacionalFuncoesCronogramas { get; set; }
    public virtual DbSet<OperacionalNoiteCronogModel> OperacionalNoiteCronogs { get; set; }
    public virtual DbSet<ComercialClienteModel> ComercialClientes { get; set; }
    public virtual DbSet<ProducaoAprovadoModel> ProducaoAprovados { get; set; }
    public virtual DbSet<ViewCronogramaModel> ViewCronogramas { get; set; }

    public virtual DbSet<EquipeExternaEquipeModel> Equipes { get; set; }
    public virtual DbSet<EquipeExternaValoresPrevisaoEquipeModel> EquipePrevisoes { get; set; }
    public virtual DbSet<RelatorioPagamentoModel> RelatorioPagamentos { get; set; }
    public virtual DbSet<RelatorioDetalheModel> RelatorioDetalhes { get; set; }
    public virtual DbSet<EquipeExternaDescricaoServicoModel> EquipeExternaDescricoes { get; set; }

    private readonly DataBaseSettings BaseSettings = DataBaseSettings.Instance;

    static Context() => AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseNpgsql(
            $"host={BaseSettings.Host};" +
            $"user id={BaseSettings.Username};" +
            $"password={BaseSettings.Password};" +
            $"database={BaseSettings.Database};" +
            $"Pooling=false;" +
            $"Timeout=300;" +
            $"CommandTimeout=300;" +
            $"Application Name=SIG Operacional <{BaseSettings.Database}>;",
            options => { options.EnableRetryOnFailure(); }
            );
        optionsBuilder.EnableSensitiveDataLogging();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OperacionalTblDespDadoBancarioModel>()
            .HasOne(b => b.Funcionario)
            .WithMany(f => f.DadosBancarios)
            .HasForeignKey(b => b.cod_func);

        // Relatório 1:N Adiantamentos
        modelBuilder.Entity<OperacionalRelatorioDespesaModel>()
            .HasMany(r => r.RelatorioAdiantamento)
            .WithOne(a => a.RelatorioDespesa)
            .HasForeignKey(a => a.cod_relatorio)
            .OnDelete(DeleteBehavior.Cascade);

        // Relatório 1:N Observações
        modelBuilder.Entity<OperacionalRelatorioDespesaModel>()
            .HasMany(r => r.RelatorioObservacao)
            .WithOne(o => o.RelatorioDespesa)
            .HasForeignKey(o => o.cod_relatorio)
            .OnDelete(DeleteBehavior.Cascade);

        // Relatório 1:N Detalhes
        modelBuilder.Entity<OperacionalRelatorioDespesaModel>()
            .HasMany(r => r.RelatorioDespesaDetalhes)
            .WithOne(d => d.RelatorioDespesa)
            .HasForeignKey(d => d.cod_relatorio)
            .OnDelete(DeleteBehavior.Cascade);

        // Chave composta para detalhes
        //modelBuilder.Entity<OperacionalRelatorioDespesasDetalheModel>()
            //.HasKey(d => new { d.cod_linha_detalhe, d.cod_relatorio });
    }
}
