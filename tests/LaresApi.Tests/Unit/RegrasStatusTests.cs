using LaresApi.Domain;

namespace LaresApi.Tests.Unit
{
    public class RegrasStatusTests
    {
        [Theory]
        [InlineData("RECEBIDA", "TRIADA")]
        [InlineData("RECEBIDA", "EM_ANDAMENTO")]
        [InlineData("TRIADA", "EM_ANDAMENTO")]
        [InlineData("EM_ANDAMENTO", "RESOLVIDA")]
        public void TransicoesPermitidas(string atual, string novo)
        {
            Assert.True(RegrasStatus.TransicaoPermitida(atual, novo));
        }

        [Theory]
        [InlineData("RECEBIDA", "RESOLVIDA")]
        [InlineData("RECEBIDA", "RECEBIDA")]
        [InlineData("TRIADA", "RECEBIDA")]
        [InlineData("TRIADA", "RESOLVIDA")]
        [InlineData("EM_ANDAMENTO", "TRIADA")]
        [InlineData("RESOLVIDA", "EM_ANDAMENTO")]
        [InlineData("RESOLVIDA", "RECEBIDA")]
        public void TransicoesProibidas(string atual, string novo)
        {
            Assert.False(RegrasStatus.TransicaoPermitida(atual, novo));
        }

        [Theory]
        [InlineData("RECEBIDA", true)]
        [InlineData("TRIADA", true)]
        [InlineData("EM_ANDAMENTO", false)]
        [InlineData("RESOLVIDA", false)]
        public void CidadaoSoAlteraAntesDoAtendimento(string status, bool esperado)
        {
            Assert.Equal(esperado, RegrasStatus.CidadaoPodeAlterar(status));
        }
    }
}
