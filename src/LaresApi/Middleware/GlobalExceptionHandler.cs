using LaresApi.Domain;
using LaresApi.DTOs;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.WebUtilities;

namespace LaresApi.Middleware
{
    public static class RespostaErro
    {
        public static async Task EscreverAsync(HttpResponse response, int status, string mensagem,
            IDictionary<string, string>? campos = null, CancellationToken ct = default)
        {
            if (response.HasStarted) return;

            response.StatusCode = status;
            var erro = new ErroResponse(status, ReasonPhrases.GetReasonPhrase(status), mensagem, campos);
            await response.WriteAsJsonAsync(erro, ct);
        }
    }

    public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
    {
        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            int status;
            string mensagem;
            IDictionary<string, string>? campos = null;

            if (exception is ApiException api)
            {
                status = api.StatusCode;
                mensagem = api.Message;
                campos = api.Campos;
                logger.LogInformation("Requisição recusada com {Status}: {Mensagem}", status, mensagem);
            }
            else if (exception is BadHttpRequestException bad)
            {
                status = bad.StatusCode;
                mensagem = "Requisição inválida: verifique o JSON e os parâmetros enviados.";
                logger.LogInformation(bad, "Requisição malformada");
            }
            else
            {
                status = StatusCodes.Status500InternalServerError;
                mensagem = "Erro interno no servidor.";
                logger.LogError(exception, "Erro não tratado");
            }

            await RespostaErro.EscreverAsync(httpContext.Response, status, mensagem, campos, cancellationToken);
            return true;
        }
    }
}
