using Microsoft.EntityFrameworkCore;

namespace CrawlData.Models
{
    public class CouponDbContext : DbContext
    {
        public CouponDbContext(DbContextOptions<CouponDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Coupon> Coupons { get; set; }
        public DbSet<UserCoupon> UserCoupons { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure User entity
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Username).IsUnique();
            });

            // Configure Coupon entity
            modelBuilder.Entity<Coupon>(entity =>
            {
                entity.HasKey(e => e.Id);
            });

            // Configure UserCoupon entity (junction table)
            modelBuilder.Entity<UserCoupon>(entity =>
            {
                // Composite primary key
                entity.HasKey(uc => new { uc.UserId, uc.CouponId });

                // Configure relationships
                entity.HasOne(uc => uc.User)
                    .WithMany(u => u.UserCoupons)
                    .HasForeignKey(uc => uc.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(uc => uc.Coupon)
                    .WithMany(c => c.UserCoupons)
                    .HasForeignKey(uc => uc.CouponId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}

