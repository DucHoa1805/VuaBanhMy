using VuaBanhMy.Models;
using VuaBanhMy.Models.ViewModels;

namespace VuaBanhMy.Services
{
    public interface IProductService
    {
        // Khách
        Task<List<MenuCategoryViewModel>> GetMenuAsync();

        // Admin (dùng ở Task 1.6)
        Task<List<Product>> GetAllAsync();                 // gồm cả món đã ẩn, Include Category
        Task<Product?> GetByIdAsync(int id);
        Task<ServiceResult> CreateAsync(Product product);
        Task<ServiceResult> UpdateAsync(Product product);
        Task<ServiceResult> ToggleAvailabilityAsync(int id);
    }
}
