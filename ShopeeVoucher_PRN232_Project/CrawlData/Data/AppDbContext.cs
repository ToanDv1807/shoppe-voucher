using CrawlData.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace CrawlData.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Coupon> Coupons { get; set; }
        public DbSet<Ecommerce> Ecommerces { get; set; }
        public DbSet<UserCoupon> UserCoupons { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure composite primary key for UserCoupon
            modelBuilder.Entity<UserCoupon>()
                .HasKey(uc => new { uc.UserId, uc.CouponId });

            // Configure User relationship
            modelBuilder.Entity<UserCoupon>()
                .HasOne(uc => uc.User)
                .WithMany(u => u.UserCoupons)
                .HasForeignKey(uc => uc.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure Coupon relationship
            modelBuilder.Entity<UserCoupon>()
                .HasOne(uc => uc.Coupon)
                .WithMany(c => c.UserCoupons)
                .HasForeignKey(uc => uc.CouponId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure Coupon-Ecommerce relationship
            modelBuilder.Entity<Coupon>()
                .HasOne(c => c.Ecommerce)
                .WithMany(e => e.Coupons)
                .HasForeignKey(c => c.Platform)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure unique constraint on username
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();

            // Seed initial e-commerce platforms
            modelBuilder.Entity<Ecommerce>().HasData(
                new Ecommerce { Id = 1, Name = "Shopee" },
                new Ecommerce { Id = 2, Name = "Lazada" },
                new Ecommerce { Id = 3, Name = "Tiki" },
                new Ecommerce { Id = 4, Name = "Sendo" }
            );
        }
    }
}

