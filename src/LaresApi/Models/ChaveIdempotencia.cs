namespace LaresApi.Models
{
    public class ChaveIdempotencia
    {
        public string Id { get; set; } = null!;
        public string HashRequisicao { get; set; } = null!;
        public int? StatusCode { get; set; }
        public string? Resposta { get; set; }
        public string? Localizacao { get; set; }
        public DateTime DataCriacao { get; set; }
    }
}
