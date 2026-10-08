namespace VuaBanhMy.Models.ViewModels
{
    // Một nhóm trên trang Menu: tên danh mục + các món đang bán
    public class MenuCategoryViewModel
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public List<MenuProductViewModel> Products { get; set; } = new();
    }

    public class MenuProductViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public string? ImageUrl { get; set; }
    }
}
