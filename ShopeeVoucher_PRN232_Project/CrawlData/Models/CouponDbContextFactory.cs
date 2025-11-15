using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace CrawlData.Models
{
    public class CouponDbContextFactory : IDesignTimeDbContextFactory<CouponDbContext>
    {
        public CouponDbContext CreateDbContext(string[] args)
        {
            // Build configuration
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();

            // Get connection string
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            // Configure DbContext options
            var optionsBuilder = new DbContextOptionsBuilder<CouponDbContext>();
            optionsBuilder.UseSqlServer(connectionString);

            return new CouponDbContext(optionsBuilder.Options);
        }
    }
}

