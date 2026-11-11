using LaresApi.Domain;
using LaresApi.DTOs;
using LaresApi.Services;
using LaresApi.Tests.Fakes;

namespace LaresApi.Tests.Unit
{
    public class IdempotenciaServiceTests
    {
        private readonly FakeIdempotenciaStore _store = new();
        private readonly IdempotenciaService _service;
        private int _execucoes;

        public IdempotenciaServiceTests()
        {
            _service = new IdempotenciaService(_store, TimeProvider.System);
        }

        private static readonly OcorrenciaRequest Corpo = new("Poste apagado", "Moema", null, null);

        private Task<RespostaOperacao> Operacao()
        {
            _execucoes++;
            return Task.FromResult(new RespostaOperacao(201, new { id = _execucoes }, $"/ocorrencias/{_execucoes}"));
        }

        [Fact]
        public async Task PrimeiraChamada_ExecutaAOperacao()
        {
            var r = await _service.ExecutarAsync("chave-1", "1", Corpo, Operacao, CancellationToken.None);

            Assert.False(r.Repetida);
            Assert.Equal(201, r.StatusCode);
            Assert.Equal(1, _execucoes);
        }

        [Fact]
        public async Task MesmaChaveMesmoCorpo_NaoExecutaDeNovo_EDevolveAMesmaResposta()
        {
            var primeira = await _service.ExecutarAsync("chave-1", "1", Corpo, Operacao, CancellationToken.None);
            var segunda = await _service.ExecutarAsync("chave-1", "1", Corpo, Operacao, CancellationToken.None);

            Assert.Equal(1, _execucoes);
            Assert.True(segunda.Repetida);
            Assert.Equal(primeira.CorpoJson, segunda.CorpoJson);
            Assert.Equal(primeira.Localizacao, segunda.Localizacao);
        }

        [Fact]
        public async Task MesmaChaveOutroCorpo_Lanca422()
        {
            await _service.ExecutarAsync("chave-1", "1", Corpo, Operacao, CancellationToken.None);

            var ex = await Assert.ThrowsAsync<IdempotenciaConflitoException>(() =>
                _service.ExecutarAsync("chave-1", "1", Corpo with { Bairro = "Pinheiros" }, Operacao, CancellationToken.None));

            Assert.Equal(422, ex.StatusCode);
        }

        [Fact]
        public async Task MesmaChaveOutroUsuario_SaoIndependentes()
        {
            await _service.ExecutarAsync("chave-1", "1", Corpo, Operacao, CancellationToken.None);
            var r = await _service.ExecutarAsync("chave-1", "2", Corpo, Operacao, CancellationToken.None);

            Assert.False(r.Repetida);
            Assert.Equal(2, _execucoes);
        }

        [Fact]
        public async Task OperacaoFalha_LiberaAChave()
        {
            await Assert.ThrowsAsync<ValidacaoException>(() =>
                _service.ExecutarAsync("chave-1", "1", Corpo,
                    () => throw new ValidacaoException(new Dictionary<string, string> { ["descricao"] = "erro" }),
                    CancellationToken.None));

            Assert.Empty(_store.Registros);
        }

        [Fact]
        public async Task ChaveEmProcessamento_Lanca409()
        {
            await _service.ExecutarAsync("chave-1", "1", Corpo, Operacao, CancellationToken.None);
            var registro = _store.Registros.Values.Single();
            registro.StatusCode = null;
            registro.Resposta = null;

            await Assert.ThrowsAsync<ConflitoException>(() =>
                _service.ExecutarAsync("chave-1", "1", Corpo, Operacao, CancellationToken.None));
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task ChaveVazia_Lanca400(string chave)
        {
            await Assert.ThrowsAsync<ValidacaoException>(() =>
                _service.ExecutarAsync(chave, "1", Corpo, Operacao, CancellationToken.None));
        }

        [Fact]
        public async Task ChaveMuitoLonga_Lanca400()
        {
            await Assert.ThrowsAsync<ValidacaoException>(() =>
                _service.ExecutarAsync(new string('x', 101), "1", Corpo, Operacao, CancellationToken.None));
        }
    }
}
