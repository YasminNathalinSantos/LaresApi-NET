using LaresApi.Models;
using Microsoft.EntityFrameworkCore;

namespace LaresApi.Data
{
    public class LaresDbContext : DbContext
    {
        public LaresDbContext(DbContextOptions<LaresDbContext> options) : base(options)
        {
        }

        public DbSet<Usuario> Usuarios => Set<Usuario>();
        public DbSet<Categoria> Categorias => Set<Categoria>();
        public DbSet<Ocorrencia> Ocorrencias => Set<Ocorrencia>();
        public DbSet<ChaveIdempotencia> ChavesIdempotencia => Set<ChaveIdempotencia>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Usuario>(e =>
            {
                e.ToTable("LARES_USUARIO", t =>
                    t.HasCheckConstraint("CK_LARES_USUARIO_PAPEL", "[papel] IN ('CIDADAO', 'GESTOR')"));
                e.HasKey(u => u.Id);
                e.Property(u => u.Id).HasColumnName("id").HasMaxLength(24).IsUnicode(false).ValueGeneratedNever();
                e.Property(u => u.Nome).HasColumnName("nome").HasMaxLength(100);
                e.Property(u => u.Email).HasColumnName("email").HasMaxLength(150);
                e.Property(u => u.SenhaHash).HasColumnName("senha_hash").HasMaxLength(100).IsUnicode(false);
                e.Property(u => u.Papel).HasColumnName("papel").HasMaxLength(10).IsUnicode(false);
                e.Property(u => u.DataCadastro).HasColumnName("data_cadastro").HasColumnType("datetime2");
                e.HasIndex(u => u.Email).IsUnique();
            });

            modelBuilder.Entity<Categoria>(e =>
            {
                e.ToTable("LARES_CATEGORIA", t =>
                    t.HasCheckConstraint("CK_LARES_CATEGORIA_PRAZO", "[prazo_padrao_dias] > 0"));
                e.HasKey(c => c.Id);
                e.Property(c => c.Id).HasColumnName("id").HasMaxLength(24).IsUnicode(false).ValueGeneratedNever();
                e.Property(c => c.Codigo).HasColumnName("codigo").HasMaxLength(20).IsUnicode(false);
                e.Property(c => c.Nome).HasColumnName("nome").HasMaxLength(60);
                e.Property(c => c.Descricao).HasColumnName("descricao").HasMaxLength(255);
                e.Property(c => c.PrazoPadraoDias).HasColumnName("prazo_padrao_dias");
                e.HasIndex(c => c.Codigo).IsUnique();
            });

            modelBuilder.Entity<Ocorrencia>(e =>
            {
                e.ToTable("LARES_OCORRENCIA", t =>
                {
                    t.HasCheckConstraint("CK_LARES_OCORRENCIA_STATUS",
                        "[status] IN ('RECEBIDA', 'TRIADA', 'EM_ANDAMENTO', 'RESOLVIDA')");
                    t.HasCheckConstraint("CK_LARES_OCORRENCIA_PRIORIDADE",
                        "[prioridade] IS NULL OR [prioridade] IN ('BAIXA', 'MEDIA', 'ALTA')");
                });
                e.HasKey(o => o.Id);
                e.Property(o => o.Id).HasColumnName("id").HasMaxLength(24).IsUnicode(false).ValueGeneratedNever();
                e.Property(o => o.UsuarioId).HasColumnName("usuario_id").HasMaxLength(24).IsUnicode(false);
                e.Property(o => o.UsuarioNome).HasColumnName("usuario_nome").HasMaxLength(100);
                e.Property(o => o.CategoriaId).HasColumnName("categoria_id").HasMaxLength(24).IsUnicode(false);
                e.Property(o => o.CategoriaCodigo).HasColumnName("categoria_codigo").HasMaxLength(20).IsUnicode(false);
                e.Property(o => o.CategoriaNome).HasColumnName("categoria_nome").HasMaxLength(60);
                e.Property(o => o.Descricao).HasColumnName("descricao").HasMaxLength(500);
                e.Property(o => o.Bairro).HasColumnName("bairro").HasMaxLength(80);
                e.Property(o => o.BairroBusca).HasColumnName("bairro_busca").HasMaxLength(80);
                e.Property(o => o.Latitude).HasColumnName("latitude").HasPrecision(9, 6);
                e.Property(o => o.Longitude).HasColumnName("longitude").HasPrecision(9, 6);
                e.Property(o => o.Status).HasColumnName("status").HasMaxLength(15).IsUnicode(false);
                e.Property(o => o.Prioridade).HasColumnName("prioridade").HasMaxLength(5).IsUnicode(false);
                e.Property(o => o.PrioridadeOrdem).HasColumnName("prioridade_ordem");
                e.Property(o => o.PrazoEstimadoDias).HasColumnName("prazo_estimado_dias");
                e.Property(o => o.ResumoIa).HasColumnName("resumo_ia").HasMaxLength(1000);
                e.Property(o => o.MensagemCidadao).HasColumnName("mensagem_cidadao").HasMaxLength(1000);
                e.Property(o => o.DataAbertura).HasColumnName("data_abertura").HasColumnType("datetime2");
                e.Property(o => o.DataAtualizacao).HasColumnName("data_atualizacao").HasColumnType("datetime2");

                e.HasOne<Usuario>().WithMany().HasForeignKey(o => o.UsuarioId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne<Categoria>().WithMany().HasForeignKey(o => o.CategoriaId).OnDelete(DeleteBehavior.Restrict);

                e.OwnsMany(o => o.Historico, h =>
                {
                    h.ToTable("LARES_HISTORICO_STATUS");
                    h.WithOwner().HasForeignKey("ocorrencia_id");
                    h.Property<int>("Id").HasColumnName("id").ValueGeneratedOnAdd();
                    h.HasKey("Id");
                    h.Property<string>("ocorrencia_id").HasColumnName("ocorrencia_id")
                        .HasMaxLength(24).IsUnicode(false);
                    h.Property(p => p.StatusAnterior).HasColumnName("status_anterior")
                        .HasMaxLength(15).IsUnicode(false);
                    h.Property(p => p.StatusNovo).HasColumnName("status_novo").HasMaxLength(15).IsUnicode(false);
                    h.Property(p => p.Comentario).HasColumnName("comentario").HasMaxLength(255);
                    h.Property(p => p.UsuarioId).HasColumnName("usuario_id").HasMaxLength(24).IsUnicode(false);
                    h.Property(p => p.UsuarioNome).HasColumnName("usuario_nome").HasMaxLength(100);
                    h.Property(p => p.DataAlteracao).HasColumnName("data_alteracao").HasColumnType("datetime2");
                    h.HasOne<Usuario>().WithMany().HasForeignKey(p => p.UsuarioId).OnDelete(DeleteBehavior.Restrict);
                });
            });

            modelBuilder.Entity<ChaveIdempotencia>(e =>
            {
                e.ToTable("LARES_CHAVE_IDEMPOTENCIA");
                e.HasKey(c => c.Id);
                e.Property(c => c.Id).HasColumnName("id").HasMaxLength(64).IsUnicode(false).ValueGeneratedNever();
                e.Property(c => c.HashRequisicao).HasColumnName("hash_requisicao")
                    .HasMaxLength(64).IsUnicode(false);
                e.Property(c => c.StatusCode).HasColumnName("status_code");
                e.Property(c => c.Resposta).HasColumnName("resposta").HasColumnType("nvarchar(max)");
                e.Property(c => c.Localizacao).HasColumnName("localizacao").HasMaxLength(2048);
                e.Property(c => c.DataCriacao).HasColumnName("data_criacao").HasColumnType("datetime2");
            });
        }
    }
}
