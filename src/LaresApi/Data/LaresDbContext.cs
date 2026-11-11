using LaresApi.Models;
using Microsoft.EntityFrameworkCore;
using MongoDB.EntityFrameworkCore.Extensions;

namespace LaresApi.Data
{
    public class LaresDbContext : DbContext
    {
        public LaresDbContext(DbContextOptions<LaresDbContext> options) : base(options)
        {
            Database.AutoTransactionBehavior = AutoTransactionBehavior.Never;
        }

        public DbSet<Usuario> Usuarios => Set<Usuario>();
        public DbSet<Categoria> Categorias => Set<Categoria>();
        public DbSet<Ocorrencia> Ocorrencias => Set<Ocorrencia>();
        public DbSet<ChaveIdempotencia> ChavesIdempotencia => Set<ChaveIdempotencia>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Usuario>(e =>
            {
                e.ToCollection("usuarios");
                e.HasKey(u => u.Id);
                e.Property(u => u.Id).ValueGeneratedNever();
            });

            modelBuilder.Entity<Categoria>(e =>
            {
                e.ToCollection("categorias");
                e.HasKey(c => c.Id);
                e.Property(c => c.Id).ValueGeneratedNever();
            });

            modelBuilder.Entity<Ocorrencia>(e =>
            {
                e.ToCollection("ocorrencias");
                e.HasKey(o => o.Id);
                e.Property(o => o.Id).ValueGeneratedNever();
                e.OwnsMany(o => o.Historico);
            });

            modelBuilder.Entity<ChaveIdempotencia>(e =>
            {
                e.ToCollection("chaves_idempotencia");
                e.HasKey(c => c.Id);
                e.Property(c => c.Id).ValueGeneratedNever();
            });
        }
    }
}
