using System.Net.Http.Headers;
using System.Net.Http.Json;
using LaresApi.Data;
using LaresApi.DTOs;

namespace LaresApi.Tests.Integration
{
    public static class ApiHelper
    {
        public static async Task<string> LoginAsync(HttpClient client, string email, string senha)
        {
            var resposta = await client.PostAsJsonAsync("/auth/login", new LoginRequest(email, senha));
            resposta.EnsureSuccessStatusCode();
            var login = await resposta.Content.ReadFromJsonAsync<LoginResponse>();
            return login!.Token;
        }

        public static async Task<HttpClient> ClienteGestorAsync(CustomWebApplicationFactory factory)
        {
            var client = factory.CreateClient();
            var token = await LoginAsync(client, DbSeeder.GestorEmail, DbSeeder.GestorSenha);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        public static async Task<HttpClient> ClienteCidadaoNovoAsync(CustomWebApplicationFactory factory)
        {
            var client = factory.CreateClient();
            var email = $"cidadao-{Guid.NewGuid():N}@teste.com";

            var registro = await client.PostAsJsonAsync("/auth/registro",
                new RegistroRequest("Cidadão de Teste", email, "Senha@123"));
            registro.EnsureSuccessStatusCode();

            var token = await LoginAsync(client, email, "Senha@123");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        public static OcorrenciaRequest OcorrenciaValida(string descricao = "Buraco enorme na frente da escola") =>
            new(descricao, "Vila Mariana", -23.588700m, -46.632300m);

        public static async Task<OcorrenciaResponse> CriarOcorrenciaAsync(HttpClient cidadao, OcorrenciaRequest? req = null)
        {
            var resposta = await cidadao.PostAsJsonAsync("/ocorrencias", req ?? OcorrenciaValida());
            resposta.EnsureSuccessStatusCode();
            return (await resposta.Content.ReadFromJsonAsync<OcorrenciaResponse>())!;
        }
    }
}
