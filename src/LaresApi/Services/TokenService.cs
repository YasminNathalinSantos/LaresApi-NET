using System.Text;
using LaresApi.Models;
using LaresApi.Settings;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace LaresApi.Services
{
    public class TokenService(IOptions<JwtSettings> jwtOptions, TimeProvider tempo)
    {
        public string GerarToken(Usuario usuario)
        {
            var jwt = jwtOptions.Value;
            var chave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Chave));
            var agora = tempo.GetUtcNow().UtcDateTime;

            var descritor = new SecurityTokenDescriptor
            {
                Issuer = jwt.Emissor,
                Audience = jwt.Audiencia,
                IssuedAt = agora,
                NotBefore = agora,
                Expires = agora.AddHours(jwt.ExpiracaoHoras),
                SigningCredentials = new SigningCredentials(chave, SecurityAlgorithms.HmacSha256),
                Claims = new Dictionary<string, object>
                {
                    ["sub"] = usuario.Id,
                    ["name"] = usuario.Nome,
                    ["email"] = usuario.Email,
                    ["role"] = usuario.Papel
                }
            };

            return new JsonWebTokenHandler().CreateToken(descritor);
        }
    }
}
