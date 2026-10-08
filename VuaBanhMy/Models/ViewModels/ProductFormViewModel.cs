using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace VuaBanhMy.Models.ViewModels
{
    // Dữ liệu form Thêm/Sửa món — chỉ chứa field Admin được phép sửa (chống over-posting)
    public class ProductFormViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên món")]
        [StringLength(150, ErrorMessage = "Tên món tối đa 150 ký tự")]
        [Display(Name = "Tên món")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Mô tả tối đa 500 ký tự")]
        [Display(Name = "Mô tả")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập giá")]
        [Range(1000, 10_000_000, ErrorMessage = "Giá từ 1.000đ đến 10.000.000đ")]
        [Display(Name = "Giá (đ)")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn danh mục")]
        [Display(Name = "Danh mục")]
        public int? CategoryId { get; set; }

        public string? ExistingImageUrl { get; set; }   // dùng ở Task 1.7
        public IFormFile? ImageFile { get; set; }        // dùng ở Task 1.7

        public List<SelectListItem> Categories { get; set; } = new();
    }
}
