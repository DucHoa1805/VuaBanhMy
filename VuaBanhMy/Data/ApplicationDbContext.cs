using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using VuaBanhMy.Models;

namespace VuaBanhMy.Data
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
    {
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<Product> Products => Set<Product>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            // Bắt buộc gọi trước: cấu hình các bảng AspNet* của Identity
            base.OnModelCreating(builder);

            builder.Entity<Product>(entity =>
            {
                entity.Property(p => p.Price)
                      .HasPrecision(18, 2);

                entity.HasOne(p => p.Category)
                      .WithMany(c => c.Products)
                      .HasForeignKey(p => p.CategoryId)
                      .OnDelete(DeleteBehavior.Restrict); // không cho xóa danh mục còn món
            });

            builder.Entity<Category>()
                   .HasIndex(c => c.Name)
                   .IsUnique(); // không cho trùng tên danh mục

            SeedMenu(builder);
        }

        private static void SeedMenu(ModelBuilder builder)
        {
            builder.Entity<Category>().HasData(
                new Category { Id = 1, Name = "Bánh mì" },
                new Category { Id = 2, Name = "Đồ uống" },
                new Category { Id = 3, Name = "Món thêm" }
            );

            builder.Entity<Product>().HasData(
                new Product { Id = 1, CategoryId = 1, Name = "Bánh mì thịt nướng", Description = "Thịt heo nướng than, đồ chua, rau thơm", Price = 25000m, IsAvailable = true },
                new Product { Id = 2, CategoryId = 1, Name = "Bánh mì pate chả", Description = "Pate gan nhà làm, chả lụa, bơ trứng", Price = 20000m, IsAvailable = true },
                new Product { Id = 3, CategoryId = 1, Name = "Bánh mì gà xé", Description = "Gà xé phay, sốt mayonnaise, dưa leo", Price = 22000m, IsAvailable = true },
                new Product { Id = 4, CategoryId = 1, Name = "Bánh mì ốp la", Description = "Hai trứng ốp la, xì dầu, hành phi", Price = 18000m, IsAvailable = true },
                new Product { Id = 5, CategoryId = 2, Name = "Cà phê sữa đá", Description = "Cà phê phin truyền thống", Price = 18000m, IsAvailable = true },
                new Product { Id = 6, CategoryId = 2, Name = "Trà tắc", Description = "Trà xanh, tắc tươi, ít đường", Price = 12000m, IsAvailable = true },
                new Product { Id = 7, CategoryId = 3, Name = "Thêm trứng", Description = "Một trứng ốp la", Price = 5000m, IsAvailable = true },
                new Product { Id = 8, CategoryId = 3, Name = "Thêm pate", Description = null, Price = 5000m, IsAvailable = true }
            );
        }
    }
}
