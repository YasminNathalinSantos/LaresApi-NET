namespace LaresApi.Models
{
    public class HistoricoStatus
    {
        public string? StatusAnterior { get; set; }
        public string StatusNovo { get; set; } = null!;
        public string? Comentario { get; set; }
        public string? UsuarioId { get; set; }
        public string? UsuarioNome { get; set; }
        public DateTime DataAlteracao { get; set; }
    }
}
