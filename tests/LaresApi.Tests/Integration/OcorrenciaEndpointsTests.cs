using System.Net;
using System.Net.Http.Json;
using LaresApi.DTOs;

namespace LaresApi.Tests.Integration
{
    public class OcorrenciaEndpointsTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
    {
        [Fact]
        public async Task Criar_SemToken_Retorna401()
        {
            var client = factory.CreateClient();

            var resposta = await client.PostAsJsonAsync("/ocorrencias", ApiHelper.OcorrenciaValida());

            Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
            var erro = await resposta.Content.ReadFromJsonAsync<ErroResponse>();
            Assert.Equal(401, erro!.Status);
        }

        [Fact]
        public async Task Criar_Valida_Retorna201ComoRecebidaSemPrioridade()
        {
            var cidadao = await ApiHelper.ClienteCidadaoNovoAsync(factory);

            var resposta = await cidadao.PostAsJsonAsync("/ocorrencias", ApiHelper.OcorrenciaValida());

            Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
            Assert.NotNull(resposta.Headers.Location);
            var ocorrencia = await resposta.Content.ReadFromJsonAsync<OcorrenciaResponse>();
            Assert.Equal("RECEBIDA", ocorrencia!.Status);
            Assert.Null(ocorrencia.Prioridade);
            Assert.Null(ocorrencia.PrazoEstimadoDias);
            Assert.Null(ocorrencia.Categoria);
        }

        [Fact]
        public async Task Criar_SemDescricao_Retorna400()
        {
            var cidadao = await ApiHelper.ClienteCidadaoNovoAsync(factory);

            var resposta = await cidadao.PostAsJsonAsync("/ocorrencias",
                new OcorrenciaRequest("", "Vila Mariana", null, null));

            Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
            var erro = await resposta.Content.ReadFromJsonAsync<ErroResponse>();
            Assert.Contains("descricao", erro!.Campos!.Keys);
        }

        [Fact]
        public async Task Criar_ComoGestor_Retorna403()
        {
            var gestor = await ApiHelper.ClienteGestorAsync(factory);

            var resposta = await gestor.PostAsJsonAsync("/ocorrencias", ApiHelper.OcorrenciaValida());

            Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
        }

        [Fact]
        public async Task Detalhe_TemHistoricoDaAbertura()
        {
            var cidadao = await ApiHelper.ClienteCidadaoNovoAsync(factory);
            var criada = await ApiHelper.CriarOcorrenciaAsync(cidadao);

            var detalhe = await cidadao.GetFromJsonAsync<OcorrenciaResponse>($"/ocorrencias/{criada.Id}");

            var linha = Assert.Single(detalhe!.Historico!);
            Assert.Null(linha.StatusAnterior);
            Assert.Equal("RECEBIDA", linha.StatusNovo);
        }

        [Fact]
        public async Task Detalhe_Inexistente_Retorna404()
        {
            var cidadao = await ApiHelper.ClienteCidadaoNovoAsync(factory);

            var resposta = await cidadao.GetAsync("/ocorrencias/000000000000000000000000");

            Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
        }

        [Fact]
        public async Task Gestor_TriaComPrioridade_UsaPrazoPadraoDaCategoria()
        {
            var cidadao = await ApiHelper.ClienteCidadaoNovoAsync(factory);
            var gestor = await ApiHelper.ClienteGestorAsync(factory);
            var criada = await ApiHelper.CriarOcorrenciaAsync(cidadao);

            var resposta = await gestor.PatchAsJsonAsync($"/ocorrencias/{criada.Id}/status",
                new AlterarStatusRequest("TRIADA", "Perto de escola", "BURACO", "ALTA", null));

            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
            var triada = await resposta.Content.ReadFromJsonAsync<OcorrenciaResponse>();
            Assert.Equal("TRIADA", triada!.Status);
            Assert.Equal("ALTA", triada.Prioridade);
            Assert.Equal(15, triada.PrazoEstimadoDias);
            Assert.Equal("BURACO", triada.Categoria!.Codigo);
            Assert.Equal(2, triada.Historico!.Count);
        }

        [Fact]
        public async Task Gestor_DefinePrazoManual_SobrescrevePadrao()
        {
            var cidadao = await ApiHelper.ClienteCidadaoNovoAsync(factory);
            var gestor = await ApiHelper.ClienteGestorAsync(factory);
            var criada = await ApiHelper.CriarOcorrenciaAsync(cidadao);

            var resposta = await gestor.PatchAsJsonAsync($"/ocorrencias/{criada.Id}/status",
                new AlterarStatusRequest("EM_ANDAMENTO", "Equipe enviada", "ILUMINACAO", "MEDIA", 3));

            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
            var atualizada = await resposta.Content.ReadFromJsonAsync<OcorrenciaResponse>();
            Assert.Equal("EM_ANDAMENTO", atualizada!.Status);
            Assert.Equal(3, atualizada.PrazoEstimadoDias);
            Assert.Equal("ILUMINACAO", atualizada.Categoria!.Codigo);
        }

        [Fact]
        public async Task Gestor_TriaSemPrioridade_Retorna400()
        {
            var cidadao = await ApiHelper.ClienteCidadaoNovoAsync(factory);
            var gestor = await ApiHelper.ClienteGestorAsync(factory);
            var criada = await ApiHelper.CriarOcorrenciaAsync(cidadao);

            var resposta = await gestor.PatchAsJsonAsync($"/ocorrencias/{criada.Id}/status",
                new AlterarStatusRequest("TRIADA", null, "BURACO", null, null));

            Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
            var erro = await resposta.Content.ReadFromJsonAsync<ErroResponse>();
            Assert.Contains("prioridade", erro!.Campos!.Keys);
        }

        [Fact]
        public async Task Gestor_TransicaoInvalida_Retorna409()
        {
            var cidadao = await ApiHelper.ClienteCidadaoNovoAsync(factory);
            var gestor = await ApiHelper.ClienteGestorAsync(factory);
            var criada = await ApiHelper.CriarOcorrenciaAsync(cidadao);

            var resposta = await gestor.PatchAsJsonAsync($"/ocorrencias/{criada.Id}/status",
                new AlterarStatusRequest("RESOLVIDA", null, "BURACO", "BAIXA", null));

            Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
        }

        [Fact]
        public async Task Cidadao_NaoPodeMudarStatus_Retorna403()
        {
            var cidadao = await ApiHelper.ClienteCidadaoNovoAsync(factory);
            var criada = await ApiHelper.CriarOcorrenciaAsync(cidadao);

            var resposta = await cidadao.PatchAsJsonAsync($"/ocorrencias/{criada.Id}/status",
                new AlterarStatusRequest("TRIADA", null, "BURACO", "ALTA", null));

            Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
        }

        [Fact]
        public async Task Cidadao_EditaDepoisDeEmAndamento_Retorna409()
        {
            var cidadao = await ApiHelper.ClienteCidadaoNovoAsync(factory);
            var gestor = await ApiHelper.ClienteGestorAsync(factory);
            var criada = await ApiHelper.CriarOcorrenciaAsync(cidadao);
            await gestor.PatchAsJsonAsync($"/ocorrencias/{criada.Id}/status",
                new AlterarStatusRequest("EM_ANDAMENTO", null, "BURACO", "ALTA", null));

            var resposta = await cidadao.PutAsJsonAsync($"/ocorrencias/{criada.Id}",
                ApiHelper.OcorrenciaValida("Mudei a descrição"));

            Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
        }

        [Fact]
        public async Task Cidadao_EditaOcorrenciaDeOutro_Retorna403()
        {
            var dono = await ApiHelper.ClienteCidadaoNovoAsync(factory);
            var outro = await ApiHelper.ClienteCidadaoNovoAsync(factory);
            var criada = await ApiHelper.CriarOcorrenciaAsync(dono);

            var resposta = await outro.PutAsJsonAsync($"/ocorrencias/{criada.Id}",
                ApiHelper.OcorrenciaValida("Tentando editar"));

            Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
        }

        [Fact]
        public async Task Cidadao_ExcluiRecebida_Retorna204()
        {
            var cidadao = await ApiHelper.ClienteCidadaoNovoAsync(factory);
            var criada = await ApiHelper.CriarOcorrenciaAsync(cidadao);

            var resposta = await cidadao.DeleteAsync($"/ocorrencias/{criada.Id}");

            Assert.Equal(HttpStatusCode.NoContent, resposta.StatusCode);
            var depois = await cidadao.GetAsync($"/ocorrencias/{criada.Id}");
            Assert.Equal(HttpStatusCode.NotFound, depois.StatusCode);
        }

        [Fact]
        public async Task Busca_SortPrioridade_OrdenaAltaMediaBaixa()
        {
            var cidadao = await ApiHelper.ClienteCidadaoNovoAsync(factory);
            var gestor = await ApiHelper.ClienteGestorAsync(factory);
            var bairro = $"Bairro-{Guid.NewGuid():N}"[..20];

            foreach (var prioridade in new[] { "BAIXA", "ALTA", "MEDIA" })
            {
                var criada = await ApiHelper.CriarOcorrenciaAsync(cidadao,
                    new OcorrenciaRequest($"Problema {prioridade}", bairro, null, null));
                await gestor.PatchAsJsonAsync($"/ocorrencias/{criada.Id}/status",
                    new AlterarStatusRequest("TRIADA", null, "OUTROS", prioridade, null));
            }

            var pagina = await gestor.GetFromJsonAsync<PaginaResponse<OcorrenciaResponse>>(
                $"/ocorrencias?bairro={bairro.ToLowerInvariant()}&status=TRIADA&sort=prioridade");

            Assert.Equal(3, pagina!.TotalItens);
            var ordem = pagina.Itens.Select(o => o.Prioridade ?? "").ToArray();
            Assert.Equal(new[] { "ALTA", "MEDIA", "BAIXA" }, ordem);
        }

        [Fact]
        public async Task MinhasOcorrencias_RetornaPaginaNoFormatoDoContrato()
        {
            var cidadao = await ApiHelper.ClienteCidadaoNovoAsync(factory);
            await ApiHelper.CriarOcorrenciaAsync(cidadao);
            await ApiHelper.CriarOcorrenciaAsync(cidadao);

            var pagina = await cidadao.GetFromJsonAsync<PaginaResponse<OcorrenciaResponse>>(
                "/ocorrencias/minhas?page=0&size=1");

            Assert.Equal(2, pagina!.TotalItens);
            Assert.Equal(2, pagina.TotalPaginas);
            Assert.Single(pagina.Itens);
        }
    }
}
