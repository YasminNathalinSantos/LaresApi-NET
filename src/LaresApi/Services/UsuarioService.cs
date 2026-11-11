using LaresApi.Data;
using LaresApi.Domain;
using LaresApi.DTOs;
using Microsoft.EntityFrameworkCore;

namespace LaresApi.Services
{
    public class UsuarioService(LaresDbContext db)
    {
        public async Task<UsuarioMeResponse> ObterAsync(string id, CancellationToken ct)
        {
            var u = await db.Usuarios.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct)
                    ?? throw new NaoEncontradoException("Usuário não encontrado.");
            return new UsuarioMeResponse(u.Id, u.Nome, u.Email, u.Papel, DataHora.ParaBrasilia(u.DataCadastro));
        }
    }

    public class CategoriaService(LaresDbContext db)
    {
        public async Task<List<CategoriaResponse>> ListarAsync(CancellationToken ct)
        {
            var categorias = await db.Categorias.AsNoTracking().ToListAsync(ct);
            return categorias
                .OrderBy(c => c.Codigo == "OUTROS" ? 1 : 0)
                .ThenBy(c => c.Nome)
                .Select(c => OcorrenciaMapper.ParaResponse(c))
                .ToList();
        }
    }
}
