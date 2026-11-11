using System.Net;
using System.Net.Http.Json;
using LaresApi.Data;
using LaresApi.DTOs;

namespace LaresApi.Tests.Integration
{
    public class AuthEndpointsTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
    {
        [Fact]
        public async Task Login_GestorDoSeed_Retorna200ComToken()
        {
            var client = factory.CreateClient();

            var resposta = await client.PostAsJsonAsync("/auth/login",
                new LoginRequest(DbSeeder.GestorEmail, DbSeeder.GestorSenha));

            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
            var login = await resposta.Content.ReadFromJsonAsync<LoginResponse>();
            Assert.False(string.IsNullOrWhiteSpace(login!.Token));
            Assert.Equal("GESTOR", login.Papel);
        }

        [Fact]
        public async Task Login_SenhaErrada_Retorna401()
        {
            var client = factory.CreateClient();

            var resposta = await client.PostAsJsonAsync("/auth/login",
                new LoginRequest(DbSeeder.CidadaoEmail, "senha-errada"));

            Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
        }

        [Fact]
        public async Task Registro_Valido_Retorna201ComPapelCidadao()
        {
            var client = factory.CreateClient();

            var resposta = await client.PostAsJsonAsync("/auth/registro",
                new RegistroRequest("Ana Souza", $"ana-{Guid.NewGuid():N}@email.com", "Senha@123"));

            Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
            var usuario = await resposta.Content.ReadFromJsonAsync<UsuarioResponse>();
            Assert.Equal("CIDADAO", usuario!.Papel);
        }

        [Fact]
        public async Task Registro_EmailRepetido_Retorna409()
        {
            var client = factory.CreateClient();

            var resposta = await client.PostAsJsonAsync("/auth/registro",
                new RegistroRequest("Outro", DbSeeder.CidadaoEmail, "Senha@123"));

            Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
        }

        [Fact]
        public async Task Registro_DadosInvalidos_Retorna400ComCampos()
        {
            var client = factory.CreateClient();

            var resposta = await client.PostAsJsonAsync("/auth/registro",
                new RegistroRequest("", "email-sem-arroba", "123"));

            Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
            var erro = await resposta.Content.ReadFromJsonAsync<ErroResponse>();
            Assert.NotNull(erro!.Campos);
            Assert.Contains("nome", erro.Campos!.Keys);
            Assert.Contains("email", erro.Campos.Keys);
            Assert.Contains("senha", erro.Campos.Keys);
        }

        [Fact]
        public async Task UsuariosMe_SemToken_Retorna401()
        {
            var client = factory.CreateClient();

            var resposta = await client.GetAsync("/usuarios/me");

            Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
        }

        [Fact]
        public async Task Categorias_ComToken_RetornaAs7DoSeed()
        {
            var client = await ApiHelper.ClienteGestorAsync(factory);

            var categorias = await client.GetFromJsonAsync<List<CategoriaResponse>>("/categorias");

            Assert.Equal(7, categorias!.Count);
            Assert.Contains(categorias, c => c.Codigo == "BURACO" && c.PrazoPadraoDias == 15);
        }
    }
}
