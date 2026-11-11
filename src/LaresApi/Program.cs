using System.Security.Cryptography;
using LaresApi.Endpoints;
using LaresApi.Extensions;
using LaresApi.HealthChecks;
using LaresApi.Middleware;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment() && string.IsNullOrWhiteSpace(builder.Configuration["Jwt:Chave"]))
    builder.Configuration["Jwt:Chave"] = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));

builder.Services.AddLaresBancoDeDados();
builder.Services.AddLaresServicos();
builder.Services.AddLaresAutenticacao();
builder.Services.AddLaresRateLimiting();
builder.Services.AddLaresHealthChecks();
builder.Services.AddLaresDocumentacao();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.Configure<RouteHandlerOptions>(o => o.ThrowOnBadRequest = true);

var app = builder.Build();

app.ValidarConfiguracao();
await app.InicializarBancoAsync();

app.UseExceptionHandler();

app.MapOpenApi();
app.MapScalarApiReference();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/openapi/v1.json", "Lares API v1");
    options.RoutePrefix = "swagger";
    options.DocumentTitle = "Lares API (.NET)";
});

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();
app.MapAuthEndpoints();
app.MapOcorrenciaEndpoints();
app.MapLaresHealthChecks();

app.Run();

public partial class Program { }
