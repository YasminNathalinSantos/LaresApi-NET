using LaresApi.Data;
using LaresApi.Domain;
using LaresApi.DTOs;
using LaresApi.Models;
using Microsoft.EntityFrameworkCore;

namespace LaresApi.Services
{
    public class OcorrenciaService(LaresDbContext db, TimeProvider tempo)
    {
        public async Task<OcorrenciaResponse> CriarAsync(string usuarioId, OcorrenciaRequest req, CancellationToken ct)
        {
            Validacoes.ValidarOcorrencia(req);

            var cidadao = await db.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.Id == usuarioId, ct)
                          ?? throw new NaoAutorizadoException("Usuário do token não existe mais.");

            var ocorrencia = Ocorrencia.Abrir(cidadao, req.Descricao!.Trim(), req.Bairro!.Trim(),
                req.Latitude, req.Longitude, DataHora.AgoraUtc(tempo));

            db.Ocorrencias.Add(ocorrencia);
            await db.SaveChangesAsync(ct);

            return OcorrenciaMapper.ParaResponse(ocorrencia, incluirHistorico: false);
        }

        public async Task<PaginaResponse<OcorrenciaResponse>> ListarMinhasAsync(string usuarioId, int? page, int? size, CancellationToken ct)
        {
            var (pagina, tamanho) = (page ?? 0, size ?? 10);
            Validacoes.ValidarPaginacao(pagina, tamanho);

            var query = db.Ocorrencias.AsNoTracking()
                .Where(o => o.UsuarioId == usuarioId)
                .OrderByDescending(o => o.DataAbertura)
                .ThenByDescending(o => o.Id);

            return await PaginarAsync(query, pagina, tamanho, ct);
        }

        public async Task<PaginaResponse<OcorrenciaResponse>> BuscarAsync(string? bairro, string? status, string? sort,
            int? page, int? size, CancellationToken ct)
        {
            var (pagina, tamanho) = (page ?? 0, size ?? 10);
            Validacoes.ValidarPaginacao(pagina, tamanho);

            var statusNormalizado = Validacoes.Normalizar(status);
            if (!string.IsNullOrWhiteSpace(status) && !StatusOcorrencia.EhValido(statusNormalizado))
                throw new ValidacaoException(new Dictionary<string, string>
                {
                    ["status"] = "deve ser RECEBIDA, TRIADA, EM_ANDAMENTO ou RESOLVIDA"
                });

            var ordenacao = sort?.Trim().ToLowerInvariant();
            if (ordenacao is not (null or "" or "prioridade" or "dataabertura"))
                throw new ValidacaoException(new Dictionary<string, string>
                {
                    ["sort"] = "use 'prioridade' ou 'dataAbertura'"
                });

            IQueryable<Ocorrencia> query = db.Ocorrencias.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(bairro))
            {
                var bairroBusca = bairro.Trim().ToUpperInvariant();
                query = query.Where(o => o.BairroBusca == bairroBusca);
            }

            if (!string.IsNullOrWhiteSpace(statusNormalizado))
                query = query.Where(o => o.Status == statusNormalizado);

            IOrderedQueryable<Ocorrencia> ordenada = ordenacao == "prioridade"
                ? query.OrderBy(o => o.PrioridadeOrdem).ThenBy(o => o.DataAbertura).ThenBy(o => o.Id)
                : query.OrderByDescending(o => o.DataAbertura).ThenByDescending(o => o.Id);

            return await PaginarAsync(ordenada, pagina, tamanho, ct);
        }

        public async Task<OcorrenciaResponse> ObterAsync(string id, CancellationToken ct)
        {
            var ocorrencia = await db.Ocorrencias.AsNoTracking().FirstOrDefaultAsync(o => o.Id == id, ct)
                             ?? throw new NaoEncontradoException($"Ocorrência {id} não encontrada.");
            return OcorrenciaMapper.ParaResponse(ocorrencia, incluirHistorico: true);
        }

        public async Task<OcorrenciaResponse> AtualizarAsync(string id, string usuarioId, OcorrenciaRequest req, CancellationToken ct)
        {
            Validacoes.ValidarOcorrencia(req);

            var ocorrencia = await CarregarAsync(id, ct);
            GarantirDono(ocorrencia, usuarioId);

            ocorrencia.AtualizarDados(req.Descricao!.Trim(), req.Bairro!.Trim(), req.Latitude, req.Longitude,
                DataHora.AgoraUtc(tempo));

            await db.SaveChangesAsync(ct);
            return OcorrenciaMapper.ParaResponse(ocorrencia, incluirHistorico: false);
        }

        public async Task ExcluirAsync(string id, string usuarioId, CancellationToken ct)
        {
            var ocorrencia = await CarregarAsync(id, ct);
            GarantirDono(ocorrencia, usuarioId);
            ocorrencia.GarantirQueCidadaoPodeAlterar();

            db.Ocorrencias.Remove(ocorrencia);
            await db.SaveChangesAsync(ct);
        }

        public async Task<OcorrenciaResponse> AlterarStatusAsync(string id, string gestorId, AlterarStatusRequest req, CancellationToken ct)
        {
            Validacoes.ValidarAlteracaoStatus(req);

            var ocorrencia = await CarregarAsync(id, ct);
            var gestor = await db.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.Id == gestorId, ct)
                         ?? throw new NaoAutorizadoException("Usuário do token não existe mais.");

            Categoria? novaCategoria = null;
            if (req.Categoria is not null)
            {
                var codigo = Validacoes.Normalizar(req.Categoria)!;
                novaCategoria = await db.Categorias.AsNoTracking().FirstOrDefaultAsync(c => c.Codigo == codigo, ct)
                                ?? throw new ValidacaoException(new Dictionary<string, string>
                                {
                                    ["categoria"] = "categoria não encontrada (veja GET /categorias)"
                                });
            }

            ocorrencia.AlterarStatus(
                Validacoes.Normalizar(req.Status)!,
                gestor,
                string.IsNullOrWhiteSpace(req.Comentario) ? null : req.Comentario.Trim(),
                Validacoes.Normalizar(req.Prioridade),
                req.PrazoEstimadoDias,
                novaCategoria,
                DataHora.AgoraUtc(tempo));

            await db.SaveChangesAsync(ct);
            return OcorrenciaMapper.ParaResponse(ocorrencia, incluirHistorico: true);
        }

        private async Task<Ocorrencia> CarregarAsync(string id, CancellationToken ct) =>
            await db.Ocorrencias.FirstOrDefaultAsync(o => o.Id == id, ct)
            ?? throw new NaoEncontradoException($"Ocorrência {id} não encontrada.");

        private static void GarantirDono(Ocorrencia ocorrencia, string usuarioId)
        {
            if (ocorrencia.UsuarioId != usuarioId)
                throw new ProibidoException("Só quem abriu a ocorrência pode alterá-la ou excluí-la.");
        }

        private static async Task<PaginaResponse<OcorrenciaResponse>> PaginarAsync(
            IQueryable<Ocorrencia> query, int pagina, int tamanho, CancellationToken ct)
        {
            var total = await query.CountAsync(ct);
            var itens = await query.Skip(pagina * tamanho).Take(tamanho).ToListAsync(ct);
            var totalPaginas = (int)Math.Ceiling(total / (double)tamanho);

            return new PaginaResponse<OcorrenciaResponse>(
                itens.Select(o => OcorrenciaMapper.ParaResponse(o, incluirHistorico: false)).ToList(),
                pagina, tamanho, total, totalPaginas);
        }
    }
}
