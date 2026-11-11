using LaresApi.Domain;
using LaresApi.Models;
using Microsoft.EntityFrameworkCore;

namespace LaresApi.Data
{
    public static class DbSeeder
    {
        public const string GestorEmail = "gestor@lares.com";
        public const string GestorSenha = "Gestor@123";
        public const string CidadaoEmail = "cidadao@lares.com";
        public const string CidadaoSenha = "Cidadao@123";

        private const string GestorHash = "$2a$10$a8zIKv1mZCPSGj2TB7HYIey7PU0ZJCFvXQfBjUbxSlBMmYTLtH9Fq";
        private const string CidadaoHash = "$2a$10$fJaKzbxQHQmIUfiGobANsOeN479BBlXrAb.rsC/J5oYupqtPZ3BT.";

        private static readonly (string Codigo, string Nome, string Descricao, int Prazo)[] Categorias =
        [
            ("BURACO", "Buraco na via", "Buracos e afundamentos no asfalto", 15),
            ("ILUMINACAO", "Iluminação pública", "Postes apagados ou piscando", 7),
            ("LIXO", "Lixo e entulho", "Descarte irregular e coleta atrasada", 5),
            ("CALCADA", "Calçada danificada", "Calçadas quebradas ou sem acessibilidade", 30),
            ("ARVORE", "Árvore e poda", "Poda, queda ou risco de queda de árvores", 20),
            ("SINALIZACAO", "Sinalização de trânsito", "Placas, faixas e semáforos", 10),
            ("OUTROS", "Outros", "Demais problemas urbanos", 30)
        ];

        public static async Task SemearAsync(LaresDbContext db, TimeProvider tempo, CancellationToken ct = default)
        {
            var agora = DataHora.AgoraUtc(tempo);

            if (!await db.Usuarios.AnyAsync(u => u.Email == CidadaoEmail, ct))
                db.Usuarios.Add(new Usuario
                {
                    Nome = "Cidadão Teste",
                    Email = CidadaoEmail,
                    SenhaHash = CidadaoHash,
                    Papel = Papeis.Cidadao,
                    DataCadastro = agora
                });

            if (!await db.Usuarios.AnyAsync(u => u.Email == GestorEmail, ct))
                db.Usuarios.Add(new Usuario
                {
                    Nome = "Gestor Prefeitura",
                    Email = GestorEmail,
                    SenhaHash = GestorHash,
                    Papel = Papeis.Gestor,
                    DataCadastro = agora
                });

            foreach (var (codigo, nome, descricao, prazo) in Categorias)
            {
                if (!await db.Categorias.AnyAsync(c => c.Codigo == codigo, ct))
                    db.Categorias.Add(new Categoria
                    {
                        Codigo = codigo,
                        Nome = nome,
                        Descricao = descricao,
                        PrazoPadraoDias = prazo
                    });
            }

            await db.SaveChangesAsync(ct);
        }
    }
}
