using LaresApi.Domain;
using LaresApi.DTOs;
using LaresApi.Services;

namespace LaresApi.Tests.Unit
{
    public class ValidacoesTests
    {
        [Fact]
        public void Ocorrencia_Valida_NaoLanca()
        {
            var ex = Record.Exception(() =>
                Validacoes.ValidarOcorrencia(new OcorrenciaRequest("Poste apagado", "Moema", -23.6m, -46.6m)));

            Assert.Null(ex);
        }

        [Theory]
        [InlineData(null, "Moema", "descricao")]
        [InlineData("   ", "Moema", "descricao")]
        [InlineData("Poste apagado", "", "bairro")]
        public void Ocorrencia_CamposObrigatorios(string? descricao, string? bairro, string campo)
        {
            var ex = Assert.Throws<ValidacaoException>(() =>
                Validacoes.ValidarOcorrencia(new OcorrenciaRequest(descricao, bairro, null, null)));

            Assert.Contains(campo, ex.Campos!.Keys);
        }

        [Fact]
        public void Ocorrencia_DescricaoComMaisDe500_Lanca()
        {
            var ex = Assert.Throws<ValidacaoException>(() =>
                Validacoes.ValidarOcorrencia(new OcorrenciaRequest(new string('a', 501), "Moema", null, null)));

            Assert.Contains("descricao", ex.Campos!.Keys);
        }

        [Fact]
        public void Ocorrencia_LatitudeForaDoIntervalo_Lanca()
        {
            var ex = Assert.Throws<ValidacaoException>(() =>
                Validacoes.ValidarOcorrencia(new OcorrenciaRequest("Poste", "Moema", 95m, -46m)));

            Assert.Contains("latitude", ex.Campos!.Keys);
        }

        [Fact]
        public void Ocorrencia_SoLatitudeSemLongitude_Lanca()
        {
            Assert.Throws<ValidacaoException>(() =>
                Validacoes.ValidarOcorrencia(new OcorrenciaRequest("Poste", "Moema", -23m, null)));
        }

        [Theory]
        [InlineData("CONCLUIDA", null, null, "status")]
        [InlineData("TRIADA", "URGENTE", null, "prioridade")]
        [InlineData("TRIADA", "ALTA", 0, "prazoEstimadoDias")]
        [InlineData("TRIADA", "ALTA", 400, "prazoEstimadoDias")]
        public void Status_CamposInvalidos(string status, string? prioridade, int? prazo, string campo)
        {
            var ex = Assert.Throws<ValidacaoException>(() =>
                Validacoes.ValidarAlteracaoStatus(new AlterarStatusRequest(status, null, null, prioridade, prazo)));

            Assert.Contains(campo, ex.Campos!.Keys);
        }

        [Fact]
        public void Status_MinusculoEhAceito()
        {
            var ex = Record.Exception(() =>
                Validacoes.ValidarAlteracaoStatus(new AlterarStatusRequest("em_andamento", null, "buraco", "alta", 10)));

            Assert.Null(ex);
        }

        [Fact]
        public void Paginacao_TamanhoMaiorQue50_Lanca()
        {
            Assert.Throws<ValidacaoException>(() => Validacoes.ValidarPaginacao(0, 51));
        }
    }
}
