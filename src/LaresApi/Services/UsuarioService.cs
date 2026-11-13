using LaresApi.Data;
using LaresApi.Domain;
using LaresApi.DTOs;
using LaresApi.Models;
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

        public async Task<CategoriaResponse> ObterAsync(string id, CancellationToken ct)
        {
            var categoria = await db.Categorias.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, ct)
                            ?? throw new NaoEncontradoException("Categoria não encontrada.");
            return OcorrenciaMapper.ParaResponse(categoria);
        }

        public async Task<CategoriaResponse> CriarAsync(CategoriaRequest request, CancellationToken ct)
        {
            Validacoes.ValidarCategoria(request);
            var codigo = Validacoes.Normalizar(request.Codigo)!;
            if (await db.Categorias.AnyAsync(c => c.Codigo == codigo, ct))
                throw new ConflitoException("Já existe uma categoria com este código.");

            var categoria = new Categoria
            {
                Codigo = codigo,
                Nome = request.Nome!.Trim(),
                Descricao = request.Descricao?.Trim(),
                PrazoPadraoDias = request.PrazoPadraoDias!.Value
            };

            db.Categorias.Add(categoria);
            await db.SaveChangesAsync(ct);
            return OcorrenciaMapper.ParaResponse(categoria);
        }

        public async Task<CategoriaResponse> AtualizarAsync(string id, CategoriaRequest request, CancellationToken ct)
        {
            Validacoes.ValidarCategoria(request);
            var categoria = await db.Categorias.FirstOrDefaultAsync(c => c.Id == id, ct)
                            ?? throw new NaoEncontradoException("Categoria não encontrada.");
            var codigo = Validacoes.Normalizar(request.Codigo)!;

            if (await db.Categorias.AnyAsync(c => c.Id != id && c.Codigo == codigo, ct))
                throw new ConflitoException("Já existe uma categoria com este código.");

            categoria.Codigo = codigo;
            categoria.Nome = request.Nome!.Trim();
            categoria.Descricao = request.Descricao?.Trim();
            categoria.PrazoPadraoDias = request.PrazoPadraoDias!.Value;

            await db.SaveChangesAsync(ct);
            return OcorrenciaMapper.ParaResponse(categoria);
        }

        public async Task ExcluirAsync(string id, CancellationToken ct)
        {
            var categoria = await db.Categorias.FirstOrDefaultAsync(c => c.Id == id, ct)
                            ?? throw new NaoEncontradoException("Categoria não encontrada.");

            if (await db.Ocorrencias.AnyAsync(o => o.CategoriaId == id, ct))
                throw new ConflitoException("Não é possível excluir uma categoria associada a ocorrências.");

            db.Categorias.Remove(categoria);
            await db.SaveChangesAsync(ct);
        }
    }
}
