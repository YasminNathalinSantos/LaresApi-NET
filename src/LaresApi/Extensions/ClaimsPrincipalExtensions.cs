using System.Security.Claims;
using LaresApi.Domain;

namespace LaresApi.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        public static string ObterUsuarioId(this ClaimsPrincipal usuario)
        {
            var sub = usuario.FindFirst("sub")?.Value;
            return string.IsNullOrWhiteSpace(sub)
                ? throw new NaoAutorizadoException("Token sem identificação do usuário.")
                : sub;
        }
    }
}
