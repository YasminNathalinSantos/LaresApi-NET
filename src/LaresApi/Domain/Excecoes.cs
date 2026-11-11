namespace LaresApi.Domain
{
    public abstract class ApiException(int statusCode, string mensagem, IDictionary<string, string>? campos = null)
        : Exception(mensagem)
    {
        public int StatusCode { get; } = statusCode;
        public IDictionary<string, string>? Campos { get; } = campos;
    }

    public class ValidacaoException(IDictionary<string, string> campos, string mensagem = "Dados inválidos")
        : ApiException(StatusCodes.Status400BadRequest, mensagem, campos);

    public class NaoAutorizadoException(string mensagem)
        : ApiException(StatusCodes.Status401Unauthorized, mensagem);

    public class ProibidoException(string mensagem)
        : ApiException(StatusCodes.Status403Forbidden, mensagem);

    public class NaoEncontradoException(string mensagem)
        : ApiException(StatusCodes.Status404NotFound, mensagem);

    public class ConflitoException(string mensagem)
        : ApiException(StatusCodes.Status409Conflict, mensagem);

    public class IdempotenciaConflitoException(string mensagem)
        : ApiException(StatusCodes.Status422UnprocessableEntity, mensagem);
}
