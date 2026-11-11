using LaresApi.Domain;

namespace LaresApi.Models
{
    public class Categoria
    {
        public string Id { get; set; } = NovoId.Gerar();
        public string Codigo { get; set; } = null!;
        public string Nome { get; set; } = null!;
        public string? Descricao { get; set; }
        public int PrazoPadraoDias { get; set; }
    }
}
