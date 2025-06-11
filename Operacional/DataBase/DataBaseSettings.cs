namespace Operacional.DataBase
{
    public sealed class DataBaseSettings
    {
        private static readonly DataBaseSettings instance = new();
        public string? Host { get; set; }
        public string? Database { get; set; }
        public string? Username { get; set; }
        public string? Password { get; set; }
        public string CaminhoSistema { get; set; } = $@"C:\SIG\Operacional S.I.G\";
        public static DataBaseSettings Instance => instance;
    }
}
