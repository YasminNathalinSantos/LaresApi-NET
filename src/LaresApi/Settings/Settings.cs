namespace LaresApi.Settings
{
    public class JwtSettings
    {
        public string Chave { get; set; } = string.Empty;
        public string Emissor { get; set; } = "LaresApi";
        public string Audiencia { get; set; } = "LaresApp";
        public int ExpiracaoHoras { get; set; } = 24;
    }

    public class LaresDatabaseSettings
    {
        public string DatabaseName { get; set; } = "LaresDb";
        public bool UsarEmMemoria { get; set; }
        public bool InicializarNaPartida { get; set; } = true;
    }

    public class RateLimitingSettings
    {
        public int OcorrenciasPorJanela { get; set; } = 5;
        public int AutenticacaoPorJanela { get; set; } = 10;
        public int JanelaSegundos { get; set; } = 60;
    }
}
