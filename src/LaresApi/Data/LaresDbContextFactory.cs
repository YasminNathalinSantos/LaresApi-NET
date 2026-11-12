using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace LaresApi.Data
{
    public class LaresDbContextFactory : IDesignTimeDbContextFactory<LaresDbContext>
    {
        public LaresDbContext CreateDbContext(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .AddUserSecrets<LaresDbContextFactory>(optional: true)
                .AddEnvironmentVariables()
                .Build();

            var connectionString = configuration.GetConnectionString("LaresSql")
                ?? "Server=(localdb)\\MSSQLLocalDB;Database=LaresDb;Trusted_Connection=True;TrustServerCertificate=True";

            var options = new DbContextOptionsBuilder<LaresDbContext>()
                .UseSqlServer(connectionString)
                .Options;

            return new LaresDbContext(options);
        }
    }
}
