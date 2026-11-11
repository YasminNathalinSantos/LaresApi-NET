namespace LaresApi.Domain
{
    public static class RegrasStatus
    {
        private static readonly Dictionary<string, string[]> Transicoes = new()
        {
            [StatusOcorrencia.Recebida] = [StatusOcorrencia.Triada, StatusOcorrencia.EmAndamento],
            [StatusOcorrencia.Triada] = [StatusOcorrencia.EmAndamento],
            [StatusOcorrencia.EmAndamento] = [StatusOcorrencia.Resolvida],
            [StatusOcorrencia.Resolvida] = []
        };

        public static bool TransicaoPermitida(string statusAtual, string statusNovo) =>
            Transicoes.TryGetValue(statusAtual, out var permitidos) && permitidos.Contains(statusNovo);

        public static bool CidadaoPodeAlterar(string status) =>
            status is StatusOcorrencia.Recebida or StatusOcorrencia.Triada;
    }
}
