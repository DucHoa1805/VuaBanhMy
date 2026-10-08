using Microsoft.EntityFrameworkCore;
using VuaBanhMy.Data;
using VuaBanhMy.Models;
using VuaBanhMy.Models.ViewModels;

namespace VuaBanhMy.Services
{
    public class ProductService(ApplicationDbContext db) : IProductService
    {
        public Task<List<MenuCategoryViewModel>> GetMenuAsync() =>
            db.Categories
                .AsNoTracking()
                .Where(c => c.Products.Any(p => p.IsAvailable))   // bỏ danh mục không có món đang bán
                .OrderBy(c => c.Id)
                .Select(c => new MenuCategoryViewModel
                {
                    CategoryId = c.Id,
                    CategoryName = c.Name,
                    Products = c.Products
                        .Where(p => p.IsAvailable)
                        .OrderBy(p => p.Name)
                        .Select(p => new MenuProductViewModel
                        {
                            Id = p.Id,
                            Name = p.Name,
                            Description = p.Description,
                            Price = p.Price,
                            ImageUrl = p.ImageUrl
                        })
                        .ToList()
                })
                .ToListAsync();

        // ===== Phần Admin =====

        public Task<List<Product>> GetAllAsync() =>
            db.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .OrderBy(p => p.Category!.Name)
                .ThenBy(p => p.Name)
                .ToListAsync();

        public Task<Product?> GetByIdAsync(int id) =>
            db.Products.FirstOrDefaultAsync(p => p.Id == id);

        public async Task<ServiceResult> CreateAsync(Product product)
        {
            var error = await ValidateAsync(product);
            if (error != null)
                return ServiceResult.Fail(error);

            product.Name = product.Name.Trim();
            product.IsAvailable = true; // món mới luôn đang bán
            db.Products.Add(product);
            await db.SaveChangesAsync();
            return ServiceResult.Ok();
        }

        public async Task<ServiceResult> UpdateAsync(Product product)
        {
            var existing = await db.Products.FindAsync(product.Id);
            if (existing == null)
                return ServiceResult.Fail("Không tìm thấy món.");

            var error = await ValidateAsync(product);
            if (error != null)
                return ServiceResult.Fail(error);

            // Chỉ chép các field được phép sửa — IsAvailable giữ nguyên
            existing.Name = product.Name.Trim();
            existing.Description = product.Description;
            existing.Price = product.Price;
            existing.CategoryId = product.CategoryId;
            existing.ImageUrl = product.ImageUrl;
            await db.SaveChangesAsync();
            return ServiceResult.Ok();
        }

        public async Task<ServiceResult> ToggleAvailabilityAsync(int id)
        {
            var product = await db.Products.FindAsync(id);
            if (product == null)
                return ServiceResult.Fail("Không tìm thấy món.");

            product.IsAvailable = !product.IsAvailable;
            await db.SaveChangesAsync();
            return ServiceResult.Ok();
        }

        // Trả về thông báo lỗi, hoặc null nếu hợp lệ
        private async Task<string?> ValidateAsync(Product product)
        {
            if (string.IsNullOrWhiteSpace(product.Name))
                return "Vui lòng nhập tên món.";
            if (product.Price <= 0)
                return "Giá phải lớn hơn 0.";
            if (!await db.Categories.AnyAsync(c => c.Id == product.CategoryId))
                return "Danh mục không tồn tại.";
            return null;
        }
    }
}
