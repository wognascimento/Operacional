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

    private DataBaseSettings BaseSettings = DataBaseSettings.Instance;

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
}
