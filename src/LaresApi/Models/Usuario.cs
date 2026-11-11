using LaresApi.Domain;

namespace LaresApi.Models
{
    public class Usuario
    {
        public string Id { get; set; } = NovoId.Gerar();
        public string Nome { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string SenhaHash { get; set; } = null!;
        public string Papel { get; set; } = null!;
        public DateTime DataCadastro { get; set; }
    }
}
