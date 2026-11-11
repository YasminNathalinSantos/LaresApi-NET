using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace LaresApi.Tests.Integration
{
    public class CustomWebApplicationFactory : WebApplicationFactory<Program>
    {
        private readonly string _nomeBanco = $"lares-testes-{Guid.NewGuid()}";

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["LaresDatabase:UsarEmMemoria"] = "true",
                    ["LaresDatabase:DatabaseName"] = _nomeBanco,
                    ["LaresDatabase:InicializarNaPartida"] = "true",
                    ["Jwt:Chave"] = "chave-somente-para-testes-com-mais-de-32-caracteres",
                    ["RateLimiting:OcorrenciasPorJanela"] = "5",
                    ["RateLimiting:AutenticacaoPorJanela"] = "1000",
                    ["RateLimiting:JanelaSegundos"] = "60"
                });
            });
        }
    }
}
