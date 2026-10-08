namespace VuaBanhMy.Services
{
    public interface IImageService
    {
        // Trả về đường dẫn web, VD "/images/products/3f2a....jpg"
        Task<ServiceResult<string>> SaveProductImageAsync(IFormFile? file);
    }
}
