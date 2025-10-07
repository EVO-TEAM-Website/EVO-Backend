using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace EVO_Backend.Data
{
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            var cfg = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .Build();

            var cs = cfg.GetConnectionString("DefaultConnection");
            var opt = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlServer(cs)
                .Options;

            return new ApplicationDbContext(opt);
        }
    }
}
