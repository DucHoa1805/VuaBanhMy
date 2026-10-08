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

        // Phần Admin — cài ở Task 1.6
        public Task<List<Product>> GetAllAsync() => throw new NotImplementedException();
        public Task<Product?> GetByIdAsync(int id) => throw new NotImplementedException();
        public Task<ServiceResult> CreateAsync(Product product) => throw new NotImplementedException();
        public Task<ServiceResult> UpdateAsync(Product product) => throw new NotImplementedException();
        public Task<ServiceResult> ToggleAvailabilityAsync(int id) => throw new NotImplementedException();
    }
}
