using LaresApi.Domain;
using LaresApi.DTOs;
using LaresApi.Services;

namespace LaresApi.Endpoints
{
    public static class CategoriaEndpoints
    {
        public static IEndpointRouteBuilder MapCategoriaEndpoints(this IEndpointRouteBuilder app)
        {
            var categorias = app.MapGroup("/categorias")
                .WithTags("Categorias")
                .RequireAuthorization();

            categorias.MapGet("/{id:length(24)}", ObterAsync)
                .WithSummary("Detalha uma categoria")
                .Produces<CategoriaResponse>()
                .Produces<ErroResponse>(StatusCodes.Status401Unauthorized)
                .Produces<ErroResponse>(StatusCodes.Status404NotFound);

            categorias.MapPost("", CriarAsync)
                .RequireAuthorization(Politicas.Gestor)
                .WithSummary("Cria uma categoria (GESTOR)")
                .Produces<CategoriaResponse>(StatusCodes.Status201Created)
                .Produces<ErroResponse>(StatusCodes.Status400BadRequest)
                .Produces<ErroResponse>(StatusCodes.Status401Unauthorized)
                .Produces<ErroResponse>(StatusCodes.Status403Forbidden)
                .Produces<ErroResponse>(StatusCodes.Status409Conflict);

            categorias.MapPut("/{id:length(24)}", AtualizarAsync)
                .RequireAuthorization(Politicas.Gestor)
                .WithSummary("Atualiza uma categoria (GESTOR)")
                .Produces<CategoriaResponse>()
                .Produces<ErroResponse>(StatusCodes.Status400BadRequest)
                .Produces<ErroResponse>(StatusCodes.Status401Unauthorized)
                .Produces<ErroResponse>(StatusCodes.Status403Forbidden)
                .Produces<ErroResponse>(StatusCodes.Status404NotFound)
                .Produces<ErroResponse>(StatusCodes.Status409Conflict);

            categorias.MapDelete("/{id:length(24)}", ExcluirAsync)
                .RequireAuthorization(Politicas.Gestor)
                .WithSummary("Exclui uma categoria sem ocorrências associadas (GESTOR)")
                .Produces(StatusCodes.Status204NoContent)
                .Produces<ErroResponse>(StatusCodes.Status401Unauthorized)
                .Produces<ErroResponse>(StatusCodes.Status403Forbidden)
                .Produces<ErroResponse>(StatusCodes.Status404NotFound)
                .Produces<ErroResponse>(StatusCodes.Status409Conflict);

            return app;
        }

        private static async Task<IResult> ObterAsync(string id, CategoriaService service, CancellationToken ct) =>
            Results.Ok(await service.ObterAsync(id, ct));

        private static async Task<IResult> CriarAsync(CategoriaRequest request, CategoriaService service, CancellationToken ct)
        {
            var categoria = await service.CriarAsync(request, ct);
            return Results.Created($"/categorias/{categoria.Id}", categoria);
        }

        private static async Task<IResult> AtualizarAsync(
            string id, CategoriaRequest request, CategoriaService service, CancellationToken ct) =>
            Results.Ok(await service.AtualizarAsync(id, request, ct));

        private static async Task<IResult> ExcluirAsync(string id, CategoriaService service, CancellationToken ct)
        {
            await service.ExcluirAsync(id, ct);
            return Results.NoContent();
        }
    }
}
