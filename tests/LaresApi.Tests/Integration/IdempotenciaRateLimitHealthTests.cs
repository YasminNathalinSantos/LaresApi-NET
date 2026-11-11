using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using LaresApi.DTOs;

namespace LaresApi.Tests.Integration
{
    public class IdempotenciaEndpointsTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
    {
        private static HttpRequestMessage Post(OcorrenciaRequest corpo, string chave)
        {
            var mensagem = new HttpRequestMessage(HttpMethod.Post, "/ocorrencias") { Content = JsonContent.Create(corpo) };
            mensagem.Headers.Add("Idempotency-Key", chave);
            return mensagem;
        }

        [Fact]
        public async Task MesmaChave_DevolveMesmaResposta_SemCriarOutroRegistro()
        {
            var cidadao = await ApiHelper.ClienteCidadaoNovoAsync(factory);
            var chave = Guid.NewGuid().ToString();
            var corpo = ApiHelper.OcorrenciaValida();

            var primeira = await cidadao.SendAsync(Post(corpo, chave));
            var segunda = await cidadao.SendAsync(Post(corpo, chave));

            Assert.Equal(HttpStatusCode.Created, primeira.StatusCode);
            Assert.Equal(HttpStatusCode.Created, segunda.StatusCode);
            Assert.Equal("true", segunda.Headers.GetValues("Idempotency-Replayed").Single());

            var a = await primeira.Content.ReadFromJsonAsync<OcorrenciaResponse>();
            var b = await segunda.Content.ReadFromJsonAsync<OcorrenciaResponse>();
            Assert.Equal(a!.Id, b!.Id);

            var minhas = await cidadao.GetFromJsonAsync<PaginaResponse<OcorrenciaResponse>>("/ocorrencias/minhas");
            Assert.Equal(1, minhas!.TotalItens);
        }

        [Fact]
        public async Task MesmaChave_ComOutroCorpo_Retorna422()
        {
            var cidadao = await ApiHelper.ClienteCidadaoNovoAsync(factory);
            var chave = Guid.NewGuid().ToString();

            await cidadao.SendAsync(Post(ApiHelper.OcorrenciaValida("Poste apagado"), chave));
            var resposta = await cidadao.SendAsync(Post(ApiHelper.OcorrenciaValida("Lixo na calçada"), chave));

            Assert.Equal(HttpStatusCode.UnprocessableEntity, resposta.StatusCode);
        }

        [Fact]
        public async Task ChaveComErroDeValidacao_PodeSerReutilizadaDepois()
        {
            var cidadao = await ApiHelper.ClienteCidadaoNovoAsync(factory);
            var chave = Guid.NewGuid().ToString();

            var invalida = await cidadao.SendAsync(Post(new OcorrenciaRequest("", "", null, null), chave));
            var valida = await cidadao.SendAsync(Post(ApiHelper.OcorrenciaValida(), chave));

            Assert.Equal(HttpStatusCode.BadRequest, invalida.StatusCode);
            Assert.Equal(HttpStatusCode.Created, valida.StatusCode);
        }
    }

    public class RateLimitingTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
    {
        [Fact]
        public async Task SextoPostNoMesmoMinuto_Retorna429ComRetryAfter()
        {
            var cidadao = await ApiHelper.ClienteCidadaoNovoAsync(factory);

            for (var i = 1; i <= 5; i++)
            {
                var ok = await cidadao.PostAsJsonAsync("/ocorrencias", ApiHelper.OcorrenciaValida($"Ocorrência {i}"));
                Assert.Equal(HttpStatusCode.Created, ok.StatusCode);
            }

            var bloqueada = await cidadao.PostAsJsonAsync("/ocorrencias", ApiHelper.OcorrenciaValida("Ocorrência 6"));

            Assert.Equal(HttpStatusCode.TooManyRequests, bloqueada.StatusCode);
            Assert.True(bloqueada.Headers.Contains("Retry-After"));
            var erro = await bloqueada.Content.ReadFromJsonAsync<ErroResponse>();
            Assert.Equal(429, erro!.Status);
        }

        [Fact]
        public async Task LimiteEhPorUsuario_OutroCidadaoNaoEhBloqueado()
        {
            var primeiro = await ApiHelper.ClienteCidadaoNovoAsync(factory);
            var segundo = await ApiHelper.ClienteCidadaoNovoAsync(factory);

            for (var i = 0; i < 6; i++)
                await primeiro.PostAsJsonAsync("/ocorrencias", ApiHelper.OcorrenciaValida());

            var resposta = await segundo.PostAsJsonAsync("/ocorrencias", ApiHelper.OcorrenciaValida());

            Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        }
    }

    public class HealthCheckTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
    {
        [Fact]
        public async Task Health_SemToken_Retorna200ComMongoHealthy()
        {
            var client = factory.CreateClient();

            var resposta = await client.GetAsync("/health");

            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
            using var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
            var verificacoes = json.RootElement.GetProperty("verificacoes").EnumerateArray().ToList();
            var banco = verificacoes.Single(v => v.GetProperty("nome").GetString() == "mongodb");
            Assert.Equal("Healthy", banco.GetProperty("status").GetString());
        }

        [Fact]
        public async Task HealthLive_Retorna200()
        {
            var client = factory.CreateClient();

            var resposta = await client.GetAsync("/health/live");

            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        }
    }
}
