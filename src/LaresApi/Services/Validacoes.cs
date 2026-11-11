using System.Text.RegularExpressions;
using LaresApi.Domain;
using LaresApi.DTOs;

namespace LaresApi.Services
{
    public static partial class Validacoes
    {
        public const int TamanhoMaximoPagina = 50;

        [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
        private static partial Regex EmailRegex();

        public static void ValidarRegistro(RegistroRequest req)
        {
            var campos = new Dictionary<string, string>();
            Texto(campos, "nome", req.Nome, 100);
            Texto(campos, "email", req.Email, 150);
            if (!campos.ContainsKey("email") && !EmailRegex().IsMatch(req.Email!.Trim()))
                campos["email"] = "formato de e-mail inválido";

            if (string.IsNullOrWhiteSpace(req.Senha))
                campos["senha"] = "não pode ser vazia";
            else if (req.Senha.Length < 6 || req.Senha.Length > 72)
                campos["senha"] = "deve ter entre 6 e 72 caracteres";

            Lancar(campos);
        }

        public static void ValidarLogin(LoginRequest req)
        {
            var campos = new Dictionary<string, string>();
            if (string.IsNullOrWhiteSpace(req.Email)) campos["email"] = "não pode ser vazio";
            if (string.IsNullOrWhiteSpace(req.Senha)) campos["senha"] = "não pode ser vazia";
            Lancar(campos);
        }

        public static void ValidarOcorrencia(OcorrenciaRequest req)
        {
            var campos = new Dictionary<string, string>();
            Texto(campos, "descricao", req.Descricao, 500);
            Texto(campos, "bairro", req.Bairro, 80);

            if (req.Latitude is < -90 or > 90)
                campos["latitude"] = "deve estar entre -90 e 90";
            if (req.Longitude is < -180 or > 180)
                campos["longitude"] = "deve estar entre -180 e 180";
            if ((req.Latitude is null) != (req.Longitude is null))
                campos["latitude"] = "latitude e longitude devem ser enviadas juntas";

            Lancar(campos);
        }

        public static void ValidarAlteracaoStatus(AlterarStatusRequest req)
        {
            var campos = new Dictionary<string, string>();

            if (string.IsNullOrWhiteSpace(req.Status))
                campos["status"] = "não pode ser vazio";
            else if (!StatusOcorrencia.EhValido(Normalizar(req.Status)))
                campos["status"] = "deve ser RECEBIDA, TRIADA, EM_ANDAMENTO ou RESOLVIDA";

            if (req.Prioridade is not null && !Prioridades.EhValida(Normalizar(req.Prioridade)))
                campos["prioridade"] = "deve ser BAIXA, MEDIA ou ALTA";

            if (req.PrazoEstimadoDias is not null and (< 1 or > 365))
                campos["prazoEstimadoDias"] = "deve estar entre 1 e 365 dias";

            if (req.Categoria is not null && (string.IsNullOrWhiteSpace(req.Categoria) || req.Categoria.Trim().Length > 20))
                campos["categoria"] = "informe o código da categoria (ex.: BURACO)";

            if (req.Comentario is { Length: > 255 })
                campos["comentario"] = "deve ter no máximo 255 caracteres";

            Lancar(campos);
        }

        public static void ValidarPaginacao(int pagina, int tamanho)
        {
            var campos = new Dictionary<string, string>();
            if (pagina < 0) campos["page"] = "deve ser maior ou igual a 0";
            if (tamanho < 1 || tamanho > TamanhoMaximoPagina) campos["size"] = $"deve estar entre 1 e {TamanhoMaximoPagina}";
            Lancar(campos);
        }

        public static string? Normalizar(string? valor) => valor?.Trim().ToUpperInvariant();

        private static void Texto(Dictionary<string, string> campos, string nome, string? valor, int max)
        {
            if (string.IsNullOrWhiteSpace(valor))
                campos[nome] = "não pode ser vazio";
            else if (valor.Trim().Length > max)
                campos[nome] = $"deve ter no máximo {max} caracteres";
        }

        private static void Lancar(Dictionary<string, string> campos)
        {
            if (campos.Count > 0) throw new ValidacaoException(campos);
        }
    }
}
