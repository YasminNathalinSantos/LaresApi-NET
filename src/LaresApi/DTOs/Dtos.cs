using System.Text.Json.Serialization;

namespace LaresApi.DTOs
{
    public record RegistroRequest(string? Nome, string? Email, string? Senha);

    public record LoginRequest(string? Email, string? Senha);

    public record LoginResponse(string Token, string Papel, string Nome, string UsuarioId);

    public record UsuarioResponse(string Id, string Nome, string Email, string Papel);

    public record UsuarioMeResponse(string Id, string Nome, string Email, string Papel, DateTime DataCadastro);

    public record UsuarioResumo(string Id, string Nome);

    public record CategoriaResponse(string Id, string Codigo, string Nome, string? Descricao, int PrazoPadraoDias);

    public record CategoriaResumo(string Id, string Codigo, string Nome);

    public record OcorrenciaRequest(string? Descricao, string? Bairro, decimal? Latitude, decimal? Longitude);

    public record AlterarStatusRequest(
        string? Status,
        string? Comentario,
        string? Categoria,
        string? Prioridade,
        int? PrazoEstimadoDias);

    public record HistoricoResponse(
        string? StatusAnterior,
        string StatusNovo,
        string? Comentario,
        string? UsuarioNome,
        DateTime DataAlteracao);

    public record OcorrenciaResponse(
        string Id,
        string Descricao,
        string Bairro,
        decimal? Latitude,
        decimal? Longitude,
        string Status,
        string? Prioridade,
        int? PrazoEstimadoDias,
        string? ResumoIa,
        string? MensagemCidadao,
        CategoriaResumo? Categoria,
        UsuarioResumo Usuario,
        DateTime DataAbertura,
        DateTime DataAtualizacao,
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        IReadOnlyList<HistoricoResponse>? Historico);

    public record PaginaResponse<T>(IReadOnlyList<T> Itens, int Pagina, int Tamanho, long TotalItens, int TotalPaginas);

    public record ErroResponse(
        int Status,
        string Erro,
        string Mensagem,
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        IDictionary<string, string>? Campos);
}
