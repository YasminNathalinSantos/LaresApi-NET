using LaresApi.Domain;
using LaresApi.Models;

namespace LaresApi.DTOs
{
    public static class OcorrenciaMapper
    {
        public static OcorrenciaResponse ParaResponse(Ocorrencia o, bool incluirHistorico) => new(
            o.Id,
            o.Descricao,
            o.Bairro,
            o.Latitude,
            o.Longitude,
            o.Status,
            o.Prioridade,
            o.PrazoEstimadoDias,
            o.ResumoIa,
            o.MensagemCidadao,
            o.CategoriaId is null ? null : new CategoriaResumo(o.CategoriaId, o.CategoriaCodigo!, o.CategoriaNome!),
            new UsuarioResumo(o.UsuarioId, o.UsuarioNome),
            DataHora.ParaBrasilia(o.DataAbertura),
            DataHora.ParaBrasilia(o.DataAtualizacao),
            incluirHistorico
                ? o.Historico
                    .Select(h => new HistoricoResponse(h.StatusAnterior, h.StatusNovo, h.Comentario, h.UsuarioNome,
                        DataHora.ParaBrasilia(h.DataAlteracao)))
                    .ToList()
                : null);

        public static CategoriaResponse ParaResponse(Categoria c) =>
            new(c.Id, c.Codigo, c.Nome, c.Descricao, c.PrazoPadraoDias);
    }
}
