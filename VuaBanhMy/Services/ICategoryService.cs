using VuaBanhMy.Models;

namespace VuaBanhMy.Services
{
    public interface ICategoryService
    {
        Task<List<Category>> GetAllAsync();
        Task<Category?> GetByIdAsync(int id);
        Task<ServiceResult> CreateAsync(string name);
        Task<ServiceResult> UpdateAsync(int id, string name);
        Task<ServiceResult> DeleteAsync(int id);
    }
}
