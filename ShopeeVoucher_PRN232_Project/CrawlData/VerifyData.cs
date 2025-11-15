using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using CrawlData.Models;

namespace CrawlData
{
    public class VerifyData
    {
        // Renamed from Main to avoid conflict - run with: dotnet run --project VerifyData
        static async Task VerifyDatabase(string[] args)
        {
            Console.WriteLine("=== Verifying Crawled Data ===");
            Console.WriteLine();

            try
            {
                // Load configuration
                var configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json")
                    .Build();

                var connectionString = configuration.GetConnectionString("DefaultConnection");

                // Setup database context
                var optionsBuilder = new DbContextOptionsBuilder<CouponDbContext>();
                optionsBuilder.UseSqlServer(connectionString);

                using var dbContext = new CouponDbContext(optionsBuilder.Options);

                // Get all coupons
                var coupons = await dbContext.Coupons
                    .Where(c => c.Platform == "shopee")
                    .OrderByDescending(c => c.StartDate)
                    .ToListAsync();

                Console.WriteLine($"Total Shopee Coupons in Database: {coupons.Count}");
                Console.WriteLine();
                Console.WriteLine("=" + new string('=', 100));

                int count = 1;
                foreach (var coupon in coupons.Take(10)) // Show first 10
                {
                    Console.WriteLine($"\n[{count}] {coupon.Supplier ?? "N/A"}");
                    Console.WriteLine($"    Type: {(coupon.Type ? "Percentage" : "Fixed Amount")}");
                    Console.WriteLine($"    Discount: {coupon.Discount}{(coupon.Type ? "%" : "đ")}");
                    Console.WriteLine($"    Min Order: {coupon.MinValueApply?.ToString("N0") ?? "N/A"}đ");
                    Console.WriteLine($"    Available: {coupon.Available?.ToString() ?? "N/A"}%");
                    Console.WriteLine($"    Description: {(coupon.Description?.Length > 80 ? coupon.Description.Substring(0, 80) + "..." : coupon.Description ?? "N/A")}");
                    Console.WriteLine($"    Expired: {coupon.ExpiredDate:dd/MM/yyyy}");
                    Console.WriteLine($"    Platform: {coupon.Platform}");
                    count++;
                }

                if (coupons.Count > 10)
                {
                    Console.WriteLine($"\n... and {coupons.Count - 10} more coupons");
                }

                Console.WriteLine("\n" + new string('=', 100));
                
                // Statistics
                Console.WriteLine("\n=== Statistics ===");
                Console.WriteLine($"Total Coupons: {coupons.Count}");
                Console.WriteLine($"Percentage Coupons: {coupons.Count(c => c.Type)}");
                Console.WriteLine($"Fixed Amount Coupons: {coupons.Count(c => !c.Type)}");
                Console.WriteLine($"Average Discount (Percentage): {(coupons.Where(c => c.Type).Any() ? coupons.Where(c => c.Type).Average(c => c.Discount).ToString("F2") : "N/A")}%");
                Console.WriteLine($"Average Discount (Fixed): {(coupons.Where(c => !c.Type).Any() ? coupons.Where(c => !c.Type).Average(c => c.Discount).ToString("N0") : "N/A")}đ");

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            }

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }
    }
}

