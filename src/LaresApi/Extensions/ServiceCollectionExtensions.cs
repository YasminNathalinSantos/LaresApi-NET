using System.Text;
using System.Threading.RateLimiting;
using LaresApi.Data;
using LaresApi.Domain;
using LaresApi.Middleware;
using LaresApi.OpenApi;
using LaresApi.Services;
using LaresApi.Settings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using MongoDB.EntityFrameworkCore.Extensions;

namespace LaresApi.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddLaresBancoDeDados(this IServiceCollection services)
        {
            services.AddOptions<LaresDatabaseSettings>().BindConfiguration("LaresDatabase");

            services.AddSingleton<IMongoClient>(sp =>
            {
                var banco = sp.GetRequiredService<IOptions<LaresDatabaseSettings>>().Value;
                if (string.IsNullOrWhiteSpace(banco.ConnectionString))
                    throw new InvalidOperationException(
                        "Connection string do MongoDB não configurada. Defina a variável de ambiente LaresDatabase__ConnectionString.");

                var settings = MongoClientSettings.FromConnectionString(banco.ConnectionString);
                settings.ServerApi = new ServerApi(ServerApiVersion.V1);
                settings.ServerSelectionTimeout = TimeSpan.FromSeconds(10);
                return new MongoClient(settings);
            });

            services.AddDbContext<LaresDbContext>((sp, options) =>
            {
                var banco = sp.GetRequiredService<IOptions<LaresDatabaseSettings>>().Value;
                if (banco.UsarEmMemoria || string.IsNullOrWhiteSpace(banco.ConnectionString))
                {
                    options.UseInMemoryDatabase(banco.DatabaseName);
                    return;
                }

                options.UseMongoDB(sp.GetRequiredService<IMongoClient>(), banco.DatabaseName);
            });

            return services;
        }

        public static IServiceCollection AddLaresServicos(this IServiceCollection services)
        {
            services.AddSingleton(TimeProvider.System);
            services.AddScoped<TokenService>();
            services.AddScoped<AuthService>();
            services.AddScoped<UsuarioService>();
            services.AddScoped<CategoriaService>();
            services.AddScoped<OcorrenciaService>();
            services.AddScoped<IIdempotenciaStore, EfIdempotenciaStore>();
            services.AddScoped<IdempotenciaService>();
            return services;
        }

        public static IServiceCollection AddLaresAutenticacao(this IServiceCollection services)
        {
            services.AddOptions<JwtSettings>().BindConfiguration("Jwt");

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();

            services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
                .Configure<IOptions<JwtSettings>>((options, jwtOptions) =>
                {
                    var jwt = jwtOptions.Value;
                    options.MapInboundClaims = false;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = jwt.Emissor,
                        ValidateAudience = true,
                        ValidAudience = jwt.Audiencia,
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Chave)),
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.FromMinutes(1),
                        NameClaimType = "name",
                        RoleClaimType = "role"
                    };

                    options.Events = new JwtBearerEvents
                    {
                        OnChallenge = async context =>
                        {
                            context.HandleResponse();
                            await RespostaErro.EscreverAsync(context.Response, StatusCodes.Status401Unauthorized,
                                "Token ausente, inválido ou expirado. Envie Authorization: Bearer <token>.");
                        },
                        OnForbidden = context => RespostaErro.EscreverAsync(context.Response,
                            StatusCodes.Status403Forbidden, "Seu papel não tem permissão para esta operação.")
                    };
                });

            services.AddAuthorizationBuilder()
                .AddPolicy(Politicas.Cidadao, p => p.RequireRole(Papeis.Cidadao))
                .AddPolicy(Politicas.Gestor, p => p.RequireRole(Papeis.Gestor));

            return services;
        }

        public static IServiceCollection AddLaresRateLimiting(this IServiceCollection services)
        {
            services.AddOptions<RateLimitingSettings>().BindConfiguration("RateLimiting");

            services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                options.AddPolicy(PoliticasRateLimit.CriarOcorrencia, httpContext =>
                {
                    var cfg = httpContext.RequestServices.GetRequiredService<IOptions<RateLimitingSettings>>().Value;
                    var chave = httpContext.User.FindFirst("sub")?.Value
                                ?? httpContext.Connection.RemoteIpAddress?.ToString()
                                ?? "anonimo";

                    return RateLimitPartition.GetFixedWindowLimiter($"ocorrencia:{chave}", _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = cfg.OcorrenciasPorJanela,
                        Window = TimeSpan.FromSeconds(cfg.JanelaSegundos),
                        QueueLimit = 0,
                        AutoReplenishment = true
                    });
                });

                options.AddPolicy(PoliticasRateLimit.Autenticacao, httpContext =>
                {
                    var cfg = httpContext.RequestServices.GetRequiredService<IOptions<RateLimitingSettings>>().Value;
                    var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonimo";

                    return RateLimitPartition.GetFixedWindowLimiter($"auth:{ip}", _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = cfg.AutenticacaoPorJanela,
                        Window = TimeSpan.FromSeconds(cfg.JanelaSegundos),
                        QueueLimit = 0,
                        AutoReplenishment = true
                    });
                });

                options.OnRejected = async (context, cancellationToken) =>
                {
                    var segundos = 60;
                    if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                        segundos = Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds));

                    context.HttpContext.Response.Headers.RetryAfter = segundos.ToString();
                    await RespostaErro.EscreverAsync(context.HttpContext.Response, StatusCodes.Status429TooManyRequests,
                        $"Limite de requisições atingido. Tente novamente em {segundos} segundos.", ct: cancellationToken);
                };
            });

            return services;
        }

        public static IServiceCollection AddLaresDocumentacao(this IServiceCollection services)
        {
            services.AddOpenApi(options =>
            {
                options.AddDocumentTransformer<InfoEJwtTransformer>();
            });
            return services;
        }
    }
}
