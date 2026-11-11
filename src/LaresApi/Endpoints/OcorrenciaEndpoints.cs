using System.Security.Claims;
using System.Text;
using LaresApi.Domain;
using LaresApi.DTOs;
using LaresApi.Extensions;
using LaresApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace LaresApi.Endpoints
{
    public static class OcorrenciaEndpoints
    {
        public static IEndpointRouteBuilder MapOcorrenciaEndpoints(this IEndpointRouteBuilder app)
        {
            var grupo = app.MapGroup("/ocorrencias")
                .WithTags("Ocorrências")
                .RequireAuthorization();

            grupo.MapPost("", CriarAsync)
                .RequireAuthorization(Politicas.Cidadao)
                .RequireRateLimiting(PoliticasRateLimit.CriarOcorrencia)
                .WithSummary("Registrar ocorrência (CIDADAO)")
                .WithDescription("Cria a ocorrência como RECEBIDA. Envie o header Idempotency-Key para não duplicar " +
                                 "o registro se o app repetir a requisição. Limite: 5 por minuto por usuário.")
                .Produces<OcorrenciaResponse>(StatusCodes.Status201Created)
                .Produces<ErroResponse>(StatusCodes.Status400BadRequest)
                .Produces<ErroResponse>(StatusCodes.Status403Forbidden)
                .Produces<ErroResponse>(StatusCodes.Status409Conflict)
                .Produces<ErroResponse>(StatusCodes.Status422UnprocessableEntity)
                .Produces<ErroResponse>(StatusCodes.Status429TooManyRequests);

            grupo.MapGet("/minhas", ListarMinhasAsync)
                .RequireAuthorization(Politicas.Cidadao)
                .WithSummary("Minhas ocorrências (CIDADAO)")
                .Produces<PaginaResponse<OcorrenciaResponse>>()
                .Produces<ErroResponse>(StatusCodes.Status400BadRequest)
                .Produces<ErroResponse>(StatusCodes.Status403Forbidden);

            grupo.MapGet("", BuscarAsync)
                .WithSummary("Buscar ocorrências")
                .WithDescription("Filtros opcionais: bairro (ignora maiúsculas), status, sort=prioridade " +
                                 "(ALTA → MEDIA → BAIXA e depois a mais antiga). Fila do gestor: status=RECEBIDA ou TRIADA com sort=prioridade.")
                .Produces<PaginaResponse<OcorrenciaResponse>>()
                .Produces<ErroResponse>(StatusCodes.Status400BadRequest);

            grupo.MapGet("/{id:length(24)}", ObterAsync)
                .WithSummary("Detalhe da ocorrência com a linha do tempo (histórico)")
                .Produces<OcorrenciaResponse>()
                .Produces<ErroResponse>(StatusCodes.Status404NotFound);

            grupo.MapPut("/{id:length(24)}", AtualizarAsync)
                .RequireAuthorization(Politicas.Cidadao)
                .WithSummary("Editar ocorrência (CIDADAO dono, só RECEBIDA ou TRIADA)")
                .Produces<OcorrenciaResponse>()
                .Produces<ErroResponse>(StatusCodes.Status400BadRequest)
                .Produces<ErroResponse>(StatusCodes.Status403Forbidden)
                .Produces<ErroResponse>(StatusCodes.Status404NotFound)
                .Produces<ErroResponse>(StatusCodes.Status409Conflict);

            grupo.MapDelete("/{id:length(24)}", ExcluirAsync)
                .RequireAuthorization(Politicas.Cidadao)
                .WithSummary("Excluir ocorrência (CIDADAO dono, só RECEBIDA ou TRIADA)")
                .Produces(StatusCodes.Status204NoContent)
                .Produces<ErroResponse>(StatusCodes.Status403Forbidden)
                .Produces<ErroResponse>(StatusCodes.Status404NotFound)
                .Produces<ErroResponse>(StatusCodes.Status409Conflict);

            grupo.MapPatch("/{id:length(24)}/status", AlterarStatusAsync)
                .RequireAuthorization(Politicas.Gestor)
                .WithSummary("Mudar status e definir prioridade e prazo (GESTOR)")
                .WithDescription("Na triagem (saindo de RECEBIDA) a prioridade e a categoria são obrigatórias. " +
                                 "Se o prazoEstimadoDias não for enviado, usa o prazo padrão da categoria.")
                .Produces<OcorrenciaResponse>()
                .Produces<ErroResponse>(StatusCodes.Status400BadRequest)
                .Produces<ErroResponse>(StatusCodes.Status403Forbidden)
                .Produces<ErroResponse>(StatusCodes.Status404NotFound)
                .Produces<ErroResponse>(StatusCodes.Status409Conflict);

            return app;
        }

        private static async Task<IResult> CriarAsync(
            OcorrenciaRequest request,
            [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
            ClaimsPrincipal user,
            HttpContext http,
            OcorrenciaService service,
            IdempotenciaService idempotencia,
            CancellationToken ct)
        {
            var usuarioId = user.ObterUsuarioId();

            if (string.IsNullOrWhiteSpace(idempotencyKey))
            {
                var criada = await service.CriarAsync(usuarioId, request, ct);
                return Results.Created($"/ocorrencias/{criada.Id}", criada);
            }

            var resultado = await idempotencia.ExecutarAsync(idempotencyKey, usuarioId, request, async () =>
            {
                var criada = await service.CriarAsync(usuarioId, request, ct);
                return new RespostaOperacao(StatusCodes.Status201Created, criada, $"/ocorrencias/{criada.Id}");
            }, ct);

            if (resultado.Localizacao is not null)
                http.Response.Headers.Location = resultado.Localizacao;
            http.Response.Headers["Idempotency-Replayed"] = resultado.Repetida ? "true" : "false";

            return Results.Content(resultado.CorpoJson, "application/json", Encoding.UTF8, resultado.StatusCode);
        }

        private static async Task<IResult> ListarMinhasAsync(int? page, int? size, ClaimsPrincipal user,
            OcorrenciaService service, CancellationToken ct) =>
            Results.Ok(await service.ListarMinhasAsync(user.ObterUsuarioId(), page, size, ct));

        private static async Task<IResult> BuscarAsync(string? bairro, string? status, string? sort, int? page, int? size,
            OcorrenciaService service, CancellationToken ct) =>
            Results.Ok(await service.BuscarAsync(bairro, status, sort, page, size, ct));

        private static async Task<IResult> ObterAsync(string id, OcorrenciaService service, CancellationToken ct) =>
            Results.Ok(await service.ObterAsync(id, ct));

        private static async Task<IResult> AtualizarAsync(string id, OcorrenciaRequest request, ClaimsPrincipal user,
            OcorrenciaService service, CancellationToken ct) =>
            Results.Ok(await service.AtualizarAsync(id, user.ObterUsuarioId(), request, ct));

        private static async Task<IResult> ExcluirAsync(string id, ClaimsPrincipal user, OcorrenciaService service,
            CancellationToken ct)
        {
            await service.ExcluirAsync(id, user.ObterUsuarioId(), ct);
            return Results.NoContent();
        }

        private static async Task<IResult> AlterarStatusAsync(string id, AlterarStatusRequest request, ClaimsPrincipal user,
            OcorrenciaService service, CancellationToken ct) =>
            Results.Ok(await service.AlterarStatusAsync(id, user.ObterUsuarioId(), request, ct));
    }
}
