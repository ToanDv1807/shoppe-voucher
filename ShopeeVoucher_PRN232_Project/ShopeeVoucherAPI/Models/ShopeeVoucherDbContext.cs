using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace ShopeeVoucherAPI.Models;

public partial class ShopeeVoucherDbContext : DbContext
{
    public ShopeeVoucherDbContext()
    {
    }

    public ShopeeVoucherDbContext(DbContextOptions<ShopeeVoucherDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Blog> Blogs { get; set; }

    public virtual DbSet<BlogCategory> BlogCategories { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<SavedVoucher> SavedVouchers { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<Voucher> Vouchers { get; set; }

    public virtual DbSet<VoucherCategory> VoucherCategories { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Blog>(entity =>
        {
            entity.HasKey(e => e.BlogId).HasName("PK__Blog__54379E30F95F4108");

            entity.ToTable("Blog");

            entity.HasIndex(e => e.Slug, "UQ__Blog__BC7B5FB6DA17FF6C").IsUnique();

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Slug).HasMaxLength(200);
            entity.Property(e => e.Thumbnail).HasMaxLength(500);
            entity.Property(e => e.Title).HasMaxLength(200);

            entity.HasOne(d => d.Author).WithMany(p => p.Blogs)
                .HasForeignKey(d => d.AuthorId)
                .HasConstraintName("FK__Blog__AuthorId__52593CB8");

            entity.HasOne(d => d.Category).WithMany(p => p.Blogs)
                .HasForeignKey(d => d.CategoryId)
                .HasConstraintName("FK__Blog__CategoryId__534D60F1");
        });

        modelBuilder.Entity<BlogCategory>(entity =>
        {
            entity.HasKey(e => e.BlogCategoryId).HasName("PK__BlogCate__6BD2DA0123245764");

            entity.ToTable("BlogCategory");

            entity.HasIndex(e => e.CategoryName, "UQ__BlogCate__8517B2E01B2AA3DC").IsUnique();

            entity.Property(e => e.CategoryName).HasMaxLength(150);
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("PK__Roles__8AFACE1AD84D2E1D");

            entity.HasIndex(e => e.RoleName, "UQ__Roles__8A2B6160C6B35EC3").IsUnique();

            entity.Property(e => e.RoleName).HasMaxLength(50);
        });

        modelBuilder.Entity<SavedVoucher>(entity =>
        {
            entity.HasKey(e => e.SavedId).HasName("PK__SavedVou__0B058FDC0EF26A7E");

            entity.ToTable("SavedVoucher");

            entity.Property(e => e.SavedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.User).WithMany(p => p.SavedVouchers)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__SavedVouc__UserI__49C3F6B7");

            entity.HasOne(d => d.Voucher).WithMany(p => p.SavedVouchers)
                .HasForeignKey(d => d.VoucherId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__SavedVouc__Vouch__4AB81AF0");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__Users__1788CC4C5B555957");

            entity.HasIndex(e => e.Email, "UQ__Users__A9D10534DD341670").IsUnique();

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Email).HasMaxLength(150);
            entity.Property(e => e.FullName).HasMaxLength(100);
            entity.Property(e => e.PasswordHash).HasMaxLength(255);

            entity.HasMany(d => d.Roles).WithMany(p => p.Users)
                .UsingEntity<Dictionary<string, object>>(
                    "UserRole",
                    r => r.HasOne<Role>().WithMany()
                        .HasForeignKey("RoleId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK__UserRoles__RoleI__3F466844"),
                    l => l.HasOne<User>().WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK__UserRoles__UserI__3E52440B"),
                    j =>
                    {
                        j.HasKey("UserId", "RoleId").HasName("PK__UserRole__AF2760AD24E2432D");
                        j.ToTable("UserRoles");
                    });
        });

        modelBuilder.Entity<Voucher>(entity =>
        {
            entity.HasKey(e => e.VoucherId).HasName("PK__Vouchers__3AEE7921802BCB86");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.DiscountValue).HasMaxLength(50);
            entity.Property(e => e.ExpiryDate).HasColumnType("datetime");
            entity.Property(e => e.Link).HasMaxLength(500);
            entity.Property(e => e.MinOrder).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Title).HasMaxLength(200);
            entity.Property(e => e.VoucherCode).HasMaxLength(100);

            entity.HasOne(d => d.Category).WithMany(p => p.Vouchers)
                .HasForeignKey(d => d.CategoryId)
                .HasConstraintName("FK__Vouchers__Catego__45F365D3");
        });

        modelBuilder.Entity<VoucherCategory>(entity =>
        {
            entity.HasKey(e => e.VoucherCategoryId).HasName("PK__VoucherC__9EED8AF5D1C9AD89");

            entity.ToTable("VoucherCategory");

            entity.HasIndex(e => e.CategoryName, "UQ__VoucherC__8517B2E0F448BDD8").IsUnique();

            entity.Property(e => e.CategoryName).HasMaxLength(100);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
