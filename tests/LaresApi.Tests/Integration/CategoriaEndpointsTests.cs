using System.Net;
using System.Net.Http.Json;
using LaresApi.DTOs;

namespace LaresApi.Tests.Integration
{
    public class CategoriaEndpointsTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
    {
        [Fact]
        public async Task Crud_Gestor_CriaConsultaAtualizaEExcluiCategoria()
        {
            var gestor = await ApiHelper.ClienteGestorAsync(factory);
            var codigo = $"T{Guid.NewGuid():N}"[..20].ToUpperInvariant();
            var request = new CategoriaRequest(codigo, "Categoria de teste", "Descricao de teste", 12);

            var criar = await gestor.PostAsJsonAsync("/categorias", request);

            Assert.Equal(HttpStatusCode.Created, criar.StatusCode);
            var criada = await criar.Content.ReadFromJsonAsync<CategoriaResponse>();
            Assert.NotNull(criada);
            Assert.Equal($"/categorias/{criada.Id}", criar.Headers.Location?.OriginalString);
            Assert.Equal(codigo, criada.Codigo);

            var detalhe = await gestor.GetFromJsonAsync<CategoriaResponse>($"/categorias/{criada.Id}");
            Assert.Equal(criada, detalhe);
            var listagem = await gestor.GetFromJsonAsync<List<CategoriaResponse>>("/categorias");
            Assert.Contains(listagem!, c => c.Id == criada.Id);

            var atualizacao = new CategoriaRequest(codigo, "Categoria atualizada", null, 20);
            var atualizar = await gestor.PutAsJsonAsync($"/categorias/{criada.Id}", atualizacao);

            Assert.Equal(HttpStatusCode.OK, atualizar.StatusCode);
            var atualizada = await atualizar.Content.ReadFromJsonAsync<CategoriaResponse>();
            Assert.NotNull(atualizada);
            Assert.Equal("Categoria atualizada", atualizada.Nome);
            Assert.Null(atualizada.Descricao);
            Assert.Equal(20, atualizada.PrazoPadraoDias);

            var excluir = await gestor.DeleteAsync($"/categorias/{criada.Id}");
            Assert.Equal(HttpStatusCode.NoContent, excluir.StatusCode);

            var aposExcluir = await gestor.GetAsync($"/categorias/{criada.Id}");
            Assert.Equal(HttpStatusCode.NotFound, aposExcluir.StatusCode);
        }

        [Fact]
        public async Task Categoria_SemToken_Retorna401()
        {
            var client = factory.CreateClient();

            var resposta = await client.GetAsync("/categorias/000000000000000000000000");

            Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
        }

        [Fact]
        public async Task Escrita_Cidadao_Retorna403()
        {
            var cidadao = await ApiHelper.ClienteCidadaoNovoAsync(factory);

            var resposta = await cidadao.PostAsJsonAsync("/categorias",
                new CategoriaRequest($"T{Guid.NewGuid():N}"[..20], "Categoria", null, 10));

            Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
        }

        [Fact]
        public async Task Criar_CodigoExistente_Retorna409()
        {
            var gestor = await ApiHelper.ClienteGestorAsync(factory);

            var resposta = await gestor.PostAsJsonAsync("/categorias",
                new CategoriaRequest("BURACO", "Outra categoria", null, 10));

            Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
        }

        [Fact]
        public async Task Criar_PrazoInvalido_Retorna400ComCampo()
        {
            var gestor = await ApiHelper.ClienteGestorAsync(factory);

            var resposta = await gestor.PostAsJsonAsync("/categorias",
                new CategoriaRequest($"T{Guid.NewGuid():N}"[..20], "Categoria", null, 0));

            Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
            var erro = await resposta.Content.ReadFromJsonAsync<ErroResponse>();
            Assert.Contains("prazoPadraoDias", erro!.Campos!.Keys);
        }

        [Fact]
        public async Task Excluir_CategoriaAssociadaAOcorrencia_Retorna409()
        {
            var gestor = await ApiHelper.ClienteGestorAsync(factory);
            var cidadao = await ApiHelper.ClienteCidadaoNovoAsync(factory);
            var criada = await gestor.PostAsJsonAsync("/categorias",
                new CategoriaRequest($"T{Guid.NewGuid():N}"[..20], "Categoria relacionada", null, 10));
            var categoria = (await criada.Content.ReadFromJsonAsync<CategoriaResponse>())!;

            var ocorrencia = await ApiHelper.CriarOcorrenciaAsync(cidadao);
            await gestor.PatchAsJsonAsync($"/ocorrencias/{ocorrencia.Id}/status",
                new AlterarStatusRequest("TRIADA", null, categoria.Codigo, "ALTA", null));

            var resposta = await gestor.DeleteAsync($"/categorias/{categoria.Id}");

            Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
        }

        [Fact]
        public async Task OpenApi_ExpõeOsCincoEndpointsDeCategoria()
        {
            var client = factory.CreateClient();

            var swagger = await client.GetAsync("/swagger");
            Assert.Equal(HttpStatusCode.OK, swagger.StatusCode);

            var resposta = await client.GetAsync("/openapi/v1.json");

            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
            using var documento = System.Text.Json.JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
            var paths = documento.RootElement.GetProperty("paths");
            Assert.True(paths.GetProperty("/categorias").TryGetProperty("get", out _));
            Assert.True(paths.GetProperty("/categorias").TryGetProperty("post", out _));

            var item = paths.GetProperty("/categorias/{id}");
            Assert.True(item.TryGetProperty("get", out _));
            Assert.True(item.TryGetProperty("put", out _));
            Assert.True(item.TryGetProperty("delete", out _));
        }
    }
}
