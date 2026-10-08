using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using VuaBanhMy.Models;
using VuaBanhMy.Models.ViewModels;
using VuaBanhMy.Services;

namespace VuaBanhMy.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class ProductsController(
        IProductService productService,
        ICategoryService categoryService,
        IImageService imageService) : Controller
    {
        public async Task<IActionResult> Index()
        {
            var products = await productService.GetAllAsync();
            return View(products);
        }

        public async Task<IActionResult> Create()
        {
            var vm = new ProductFormViewModel();
            await LoadCategoriesAsync(vm);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductFormViewModel vm)
        {
            var imageUrl = await SaveUploadedImageAsync(vm);
            if (!ModelState.IsValid)
            {
                await LoadCategoriesAsync(vm);
                return View(vm);
            }

            var result = await productService.CreateAsync(new Product
            {
                Name = vm.Name,
                Description = vm.Description,
                Price = vm.Price,
                CategoryId = vm.CategoryId!.Value,
                ImageUrl = imageUrl
            });
            if (!result.Success)
            {
                ModelState.AddModelError("", result.ErrorMessage!);
                await LoadCategoriesAsync(vm);
                return View(vm);
            }

            TempData["Success"] = $"Đã thêm món \"{vm.Name.Trim()}\".";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var product = await productService.GetByIdAsync(id);
            if (product == null)
                return NotFound();

            var vm = new ProductFormViewModel
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                CategoryId = product.CategoryId,
                ExistingImageUrl = product.ImageUrl
            };
            await LoadCategoriesAsync(vm);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ProductFormViewModel vm)
        {
            if (id != vm.Id)
                return BadRequest();

            var newImageUrl = await SaveUploadedImageAsync(vm);
            if (!ModelState.IsValid)
            {
                await LoadCategoriesAsync(vm);
                return View(vm);
            }

            var result = await productService.UpdateAsync(new Product
            {
                Id = vm.Id,
                Name = vm.Name,
                Description = vm.Description,
                Price = vm.Price,
                CategoryId = vm.CategoryId!.Value,
                ImageUrl = newImageUrl ?? vm.ExistingImageUrl // không chọn ảnh mới → giữ ảnh cũ
            });
            if (!result.Success)
            {
                ModelState.AddModelError("", result.ErrorMessage!);
                await LoadCategoriesAsync(vm);
                return View(vm);
            }

            TempData["Success"] = "Đã cập nhật món.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleAvailability(int id)
        {
            var result = await productService.ToggleAvailabilityAsync(id);
            if (result.Success)
                TempData["Success"] = "Đã đổi trạng thái món.";
            else
                TempData["Error"] = result.ErrorMessage;

            return RedirectToAction(nameof(Index));
        }

        // Lưu ảnh nếu Admin có chọn file. Trả về đường dẫn ảnh mới, hoặc null nếu
        // không chọn ảnh / form đang có lỗi / ảnh không hợp lệ (lỗi được thêm vào ModelState)
        private async Task<string?> SaveUploadedImageAsync(ProductFormViewModel vm)
        {
            if (vm.ImageFile == null || !ModelState.IsValid)
                return null;

            var result = await imageService.SaveProductImageAsync(vm.ImageFile);
            if (!result.Success)
            {
                ModelState.AddModelError(nameof(vm.ImageFile), result.ErrorMessage!);
                return null;
            }

            return result.Data;
        }

        // Đổ dữ liệu dropdown danh mục — gọi ở GET và cả khi POST lỗi
        private async Task LoadCategoriesAsync(ProductFormViewModel vm)
        {
            var categories = await categoryService.GetAllAsync();
            vm.Categories = categories
                .Select(c => new SelectListItem(c.Name, c.Id.ToString()))
                .ToList();
        }
    }
}
