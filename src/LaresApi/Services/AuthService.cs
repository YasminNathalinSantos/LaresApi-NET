using LaresApi.Data;
using LaresApi.Domain;
using LaresApi.DTOs;
using LaresApi.Models;
using Microsoft.EntityFrameworkCore;

namespace LaresApi.Services
{
    public class AuthService(LaresDbContext db, TokenService tokenService, TimeProvider tempo)
    {
        private const int CustoBCrypt = 10;

        public async Task<UsuarioResponse> RegistrarAsync(RegistroRequest req, CancellationToken ct)
        {
            Validacoes.ValidarRegistro(req);
            var email = req.Email!.Trim().ToLowerInvariant();

            if (await db.Usuarios.AnyAsync(u => u.Email == email, ct))
                throw new ConflitoException("Já existe um usuário com este e-mail.");

            var usuario = new Usuario
            {
                Nome = req.Nome!.Trim(),
                Email = email,
                SenhaHash = BCrypt.Net.BCrypt.HashPassword(req.Senha, CustoBCrypt),
                Papel = Papeis.Cidadao,
                DataCadastro = DataHora.AgoraUtc(tempo)
            };

            db.Usuarios.Add(usuario);
            await db.SaveChangesAsync(ct);

            return new UsuarioResponse(usuario.Id, usuario.Nome, usuario.Email, usuario.Papel);
        }

        public async Task<LoginResponse> LoginAsync(LoginRequest req, CancellationToken ct)
        {
            Validacoes.ValidarLogin(req);
            var email = req.Email!.Trim().ToLowerInvariant();

            var usuario = await db.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email, ct);
            if (usuario is null || !BCrypt.Net.BCrypt.Verify(req.Senha, usuario.SenhaHash))
                throw new NaoAutorizadoException("E-mail ou senha inválidos.");

            var token = tokenService.GerarToken(usuario);
            return new LoginResponse(token, usuario.Papel, usuario.Nome, usuario.Id);
        }
    }
}
