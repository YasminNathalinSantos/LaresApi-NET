using LaresApi.Data;
using LaresApi.Settings;
using Microsoft.Extensions.Options;

namespace LaresApi.Extensions
{
    public static class WebApplicationExtensions
    {
        public static void ValidarConfiguracao(this WebApplication app)
        {
            var jwt = app.Services.GetRequiredService<IOptions<JwtSettings>>().Value;
            if (string.IsNullOrWhiteSpace(jwt.Chave) || jwt.Chave.Length < 32)
                throw new InvalidOperationException(
                    "Defina a variável de ambiente Jwt__Chave com pelo menos 32 caracteres.");
        }

        public static async Task InicializarBancoAsync(this WebApplication app)
        {
            var banco = app.Services.GetRequiredService<IOptions<LaresDatabaseSettings>>().Value;
            if (!banco.InicializarNaPartida) return;

            await using var scope = app.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<LaresDbContext>();
            var tempo = scope.ServiceProvider.GetRequiredService<TimeProvider>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<LaresDbContext>>();

            if (!banco.UsarEmMemoria && string.IsNullOrWhiteSpace(banco.ConnectionString))
                logger.LogWarning("LaresDatabase:ConnectionString não configurada: usando banco em memória. Configure-a para usar o MongoDB Atlas.");

            try
            {
                await DbSeeder.SemearAsync(db, tempo);
                logger.LogInformation("Banco pronto ({Provider}, {Banco}).", db.Database.ProviderName, banco.DatabaseName);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Não foi possível inicializar o banco de dados.");
            }
        }
    }
}
