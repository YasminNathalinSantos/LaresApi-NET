using LaresApi.Domain;

namespace LaresApi.Models
{
    public class Ocorrencia
    {
        public string Id { get; set; } = NovoId.Gerar();
        public string UsuarioId { get; set; } = null!;
        public string UsuarioNome { get; set; } = null!;
        public string? CategoriaId { get; set; }
        public string? CategoriaCodigo { get; set; }
        public string? CategoriaNome { get; set; }
        public string Descricao { get; set; } = null!;
        public string Bairro { get; set; } = null!;
        public string BairroBusca { get; set; } = null!;
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public string Status { get; set; } = StatusOcorrencia.Recebida;
        public string? Prioridade { get; set; }
        public int PrioridadeOrdem { get; set; } = Prioridades.Ordem(null);
        public int? PrazoEstimadoDias { get; set; }
        public string? ResumoIa { get; set; }
        public string? MensagemCidadao { get; set; }
        public DateTime DataAbertura { get; set; }
        public DateTime DataAtualizacao { get; set; }
        public List<HistoricoStatus> Historico { get; set; } = [];

        public static Ocorrencia Abrir(Usuario cidadao, string descricao, string bairro,
            decimal? latitude, decimal? longitude, DateTime agora)
        {
            var ocorrencia = new Ocorrencia
            {
                UsuarioId = cidadao.Id,
                UsuarioNome = cidadao.Nome,
                Latitude = latitude,
                Longitude = longitude,
                Status = StatusOcorrencia.Recebida,
                DataAbertura = agora,
                DataAtualizacao = agora
            };
            ocorrencia.DefinirTexto(descricao, bairro);

            ocorrencia.Historico.Add(new HistoricoStatus
            {
                StatusAnterior = null,
                StatusNovo = StatusOcorrencia.Recebida,
                UsuarioId = cidadao.Id,
                UsuarioNome = cidadao.Nome,
                DataAlteracao = agora
            });

            return ocorrencia;
        }

        public void AtualizarDados(string descricao, string bairro, decimal? latitude, decimal? longitude, DateTime agora)
        {
            GarantirQueCidadaoPodeAlterar();
            DefinirTexto(descricao, bairro);
            Latitude = latitude;
            Longitude = longitude;
            DataAtualizacao = agora;
        }

        public void GarantirQueCidadaoPodeAlterar()
        {
            if (!RegrasStatus.CidadaoPodeAlterar(Status))
                throw new ConflitoException(
                    $"A ocorrência está {Status} e não pode mais ser alterada pelo cidadão.");
        }

        public void AlterarStatus(string novoStatus, Usuario gestor, string? comentario,
            string? prioridade, int? prazoEstimadoDias, Categoria? novaCategoria, DateTime agora)
        {
            if (!RegrasStatus.TransicaoPermitida(Status, novoStatus))
                throw new ConflitoException($"Não é permitido mudar o status de {Status} para {novoStatus}.");

            var prioridadeFinal = prioridade ?? Prioridade;
            var prazoFinal = prazoEstimadoDias ?? PrazoEstimadoDias;
            var temCategoria = novaCategoria is not null || CategoriaId is not null;

            if (Status == StatusOcorrencia.Recebida)
            {
                var campos = new Dictionary<string, string>();
                if (prioridadeFinal is null)
                    campos["prioridade"] = "obrigatória para triar a ocorrência (BAIXA, MEDIA ou ALTA)";
                if (!temCategoria)
                    campos["categoria"] = "obrigatória para triar a ocorrência";
                if (campos.Count > 0)
                    throw new ValidacaoException(campos, "Informe a prioridade e a categoria para triar a ocorrência");

                if (prazoFinal is null && novaCategoria is not null)
                    prazoFinal = novaCategoria.PrazoPadraoDias;
            }

            Historico.Add(new HistoricoStatus
            {
                StatusAnterior = Status,
                StatusNovo = novoStatus,
                Comentario = comentario,
                UsuarioId = gestor.Id,
                UsuarioNome = gestor.Nome,
                DataAlteracao = agora
            });

            if (novaCategoria is not null)
            {
                CategoriaId = novaCategoria.Id;
                CategoriaCodigo = novaCategoria.Codigo;
                CategoriaNome = novaCategoria.Nome;
            }

            Prioridade = prioridadeFinal;
            PrioridadeOrdem = Prioridades.Ordem(prioridadeFinal);
            PrazoEstimadoDias = prazoFinal;
            Status = novoStatus;
            DataAtualizacao = agora;
        }

        private void DefinirTexto(string descricao, string bairro)
        {
            Descricao = descricao;
            Bairro = bairro;
            BairroBusca = bairro.Trim().ToUpperInvariant();
        }
    }
}
