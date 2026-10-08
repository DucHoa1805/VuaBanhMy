using Microsoft.EntityFrameworkCore;
using VuaBanhMy.Data;
using VuaBanhMy.Models;

namespace VuaBanhMy.Services
{
    public class CategoryService(ApplicationDbContext db) : ICategoryService
    {
        public Task<List<Category>> GetAllAsync() =>
            db.Categories.OrderBy(c => c.Name).ToListAsync();

        public Task<Category?> GetByIdAsync(int id) =>
            db.Categories.FirstOrDefaultAsync(c => c.Id == id);

        public async Task<ServiceResult> CreateAsync(string name)
        {
            var error = await ValidateNameAsync(name, excludeId: null);
            if (error != null)
                return ServiceResult.Fail(error);

            db.Categories.Add(new Category { Name = name.Trim() });
            await db.SaveChangesAsync();
            return ServiceResult.Ok();
        }

        public async Task<ServiceResult> UpdateAsync(int id, string name)
        {
            var category = await db.Categories.FindAsync(id);
            if (category == null)
                return ServiceResult.Fail("Không tìm thấy danh mục.");

            var error = await ValidateNameAsync(name, excludeId: id);
            if (error != null)
                return ServiceResult.Fail(error);

            category.Name = name.Trim();
            await db.SaveChangesAsync();
            return ServiceResult.Ok();
        }

        public async Task<ServiceResult> DeleteAsync(int id)
        {
            var category = await db.Categories.FindAsync(id);
            if (category == null)
                return ServiceResult.Fail("Không tìm thấy danh mục.");

            if (await db.Products.AnyAsync(p => p.CategoryId == id))
                return ServiceResult.Fail("Danh mục vẫn còn món, không thể xóa.");

            db.Categories.Remove(category);
            await db.SaveChangesAsync();
            return ServiceResult.Ok();
        }

        // Trả về thông báo lỗi, hoặc null nếu tên hợp lệ
        private async Task<string?> ValidateNameAsync(string name, int? excludeId)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "Vui lòng nhập tên danh mục.";

            var normalized = name.Trim().ToLower();
            var isDuplicate = await db.Categories.AnyAsync(c =>
                c.Id != excludeId && c.Name.ToLower() == normalized);

            return isDuplicate ? "Tên danh mục đã tồn tại." : null;
        }
    }
}
