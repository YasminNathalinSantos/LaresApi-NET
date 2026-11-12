using LaresApi.Data;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace LaresApi.HealthChecks
{
    public class MemoriaHealthCheck : IHealthCheck
    {
        private const long LimiteBytes = 512L * 1024 * 1024;

        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            var usada = GC.GetTotalMemory(forceFullCollection: false);
            var dados = new Dictionary<string, object> { ["memoriaMb"] = usada / 1024 / 1024 };

            var resultado = usada < LimiteBytes
                ? HealthCheckResult.Healthy("Uso de memória normal", dados)
                : HealthCheckResult.Degraded("Uso de memória acima de 512 MB", data: dados);

            return Task.FromResult(resultado);
        }
    }

    public class SqlServerHealthCheck(LaresDbContext db) : IHealthCheck
    {
        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            if (db.Database.IsInMemory())
                return HealthCheckResult.Healthy("Banco em memória");

            try
            {
                return await db.Database.CanConnectAsync(cancellationToken)
                    ? HealthCheckResult.Healthy("SQL Server acessível")
                    : HealthCheckResult.Unhealthy("SQL Server inacessível");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("SQL Server inacessível", ex);
            }
        }
    }

    public static class HealthChecksSetup
    {
        public static IServiceCollection AddLaresHealthChecks(this IServiceCollection services)
        {
            services.AddHealthChecks()
                .AddCheck("api", () => HealthCheckResult.Healthy("API respondendo"), tags: ["live"])
                .AddCheck<MemoriaHealthCheck>("memoria", tags: ["live"])
                .AddCheck<SqlServerHealthCheck>("sqlserver", HealthStatus.Unhealthy, tags: ["ready"], timeout: TimeSpan.FromSeconds(10));

            return services;
        }

        public static WebApplication MapLaresHealthChecks(this WebApplication app)
        {
            app.MapHealthChecks("/health", new HealthCheckOptions { ResponseWriter = EscreverJsonAsync });

            app.MapHealthChecks("/health/live", new HealthCheckOptions
            {
                Predicate = c => c.Tags.Contains("live"),
                ResponseWriter = EscreverJsonAsync
            });

            app.MapHealthChecks("/health/ready", new HealthCheckOptions
            {
                Predicate = c => c.Tags.Contains("ready"),
                ResponseWriter = EscreverJsonAsync
            });

            return app;
        }

        private static Task EscreverJsonAsync(HttpContext context, HealthReport report)
        {
            var resposta = new
            {
                status = report.Status.ToString(),
                duracaoMs = Math.Round(report.TotalDuration.TotalMilliseconds, 1),
                verificacoes = report.Entries.Select(e => new
                {
                    nome = e.Key,
                    status = e.Value.Status.ToString(),
                    descricao = e.Value.Description,
                    duracaoMs = Math.Round(e.Value.Duration.TotalMilliseconds, 1),
                    erro = e.Value.Exception?.Message,
                    dados = e.Value.Data.Count > 0 ? e.Value.Data : null
                })
            };

            return context.Response.WriteAsJsonAsync(resposta);
        }
    }
}
