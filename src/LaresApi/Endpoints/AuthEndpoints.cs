using System.Security.Claims;
using LaresApi.Domain;
using LaresApi.DTOs;
using LaresApi.Extensions;
using LaresApi.Services;

namespace LaresApi.Endpoints
{
    public static class AuthEndpoints
    {
        public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
        {
            var auth = app.MapGroup("/auth")
                .WithTags("Autenticação")
                .AllowAnonymous()
                .RequireRateLimiting(PoliticasRateLimit.Autenticacao);

            auth.MapPost("/registro", RegistrarAsync)
                .WithSummary("Cadastro de cidadão")
                .WithDescription("Cria um usuário com o papel CIDADAO. O gestor vem do seed.")
                .Produces<UsuarioResponse>(StatusCodes.Status201Created)
                .Produces<ErroResponse>(StatusCodes.Status400BadRequest)
                .Produces<ErroResponse>(StatusCodes.Status409Conflict)
                .Produces<ErroResponse>(StatusCodes.Status429TooManyRequests);

            auth.MapPost("/login", LoginAsync)
                .WithSummary("Login")
                .WithDescription("Devolve o JWT (vale 24 h), o papel, o nome e o id do usuário.")
                .Produces<LoginResponse>()
                .Produces<ErroResponse>(StatusCodes.Status400BadRequest)
                .Produces<ErroResponse>(StatusCodes.Status401Unauthorized)
                .Produces<ErroResponse>(StatusCodes.Status429TooManyRequests);

            var usuarios = app.MapGroup("/usuarios")
                .WithTags("Usuários")
                .RequireAuthorization();

            usuarios.MapGet("/me", ObterMeAsync)
                .WithSummary("Dados do usuário logado")
                .Produces<UsuarioMeResponse>()
                .Produces<ErroResponse>(StatusCodes.Status401Unauthorized);

            var categorias = app.MapGroup("/categorias")
                .WithTags("Categorias")
                .RequireAuthorization();

            categorias.MapGet("", ListarCategoriasAsync)
                .WithSummary("Lista de categorias com o prazo padrão de cada uma")
                .Produces<List<CategoriaResponse>>()
                .Produces<ErroResponse>(StatusCodes.Status401Unauthorized);

            return app;
        }

        private static async Task<IResult> RegistrarAsync(RegistroRequest request, AuthService service, CancellationToken ct)
        {
            var usuario = await service.RegistrarAsync(request, ct);
            return Results.Created($"/usuarios/{usuario.Id}", usuario);
        }

        private static async Task<IResult> LoginAsync(LoginRequest request, AuthService service, CancellationToken ct) =>
            Results.Ok(await service.LoginAsync(request, ct));

        private static async Task<IResult> ObterMeAsync(ClaimsPrincipal user, UsuarioService service, CancellationToken ct) =>
            Results.Ok(await service.ObterAsync(user.ObterUsuarioId(), ct));

        private static async Task<IResult> ListarCategoriasAsync(CategoriaService service, CancellationToken ct) =>
            Results.Ok(await service.ListarAsync(ct));
    }
}
