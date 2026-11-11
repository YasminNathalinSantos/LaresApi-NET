namespace LaresApi.Domain
{
    public static class Papeis
    {
        public const string Cidadao = "CIDADAO";
        public const string Gestor = "GESTOR";
    }

    public static class StatusOcorrencia
    {
        public const string Recebida = "RECEBIDA";
        public const string Triada = "TRIADA";
        public const string EmAndamento = "EM_ANDAMENTO";
        public const string Resolvida = "RESOLVIDA";

        public static readonly string[] Todos = [Recebida, Triada, EmAndamento, Resolvida];

        public static bool EhValido(string? status) => status is not null && Todos.Contains(status);
    }

    public static class Prioridades
    {
        public const string Baixa = "BAIXA";
        public const string Media = "MEDIA";
        public const string Alta = "ALTA";

        public static readonly string[] Todas = [Baixa, Media, Alta];

        public static bool EhValida(string? prioridade) => prioridade is not null && Todas.Contains(prioridade);

        public static int Ordem(string? prioridade) => prioridade switch
        {
            Alta => 0,
            Media => 1,
            Baixa => 2,
            _ => 3
        };
    }

    public static class Politicas
    {
        public const string Cidadao = "SomenteCidadao";
        public const string Gestor = "SomenteGestor";
    }

    public static class PoliticasRateLimit
    {
        public const string CriarOcorrencia = "criar-ocorrencia";
        public const string Autenticacao = "autenticacao";
    }
}
