using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VuaBanhMy.Models.ViewModels;
using VuaBanhMy.Services;

namespace VuaBanhMy.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class CategoriesController(ICategoryService categoryService) : Controller
    {
        public async Task<IActionResult> Index()
        {
            var categories = await categoryService.GetAllAsync();
            return View(categories);
        }

        public IActionResult Create()
        {
            return View(new CategoryFormViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CategoryFormViewModel vm)
        {
            if (!ModelState.IsValid)
                return View(vm);

            var result = await categoryService.CreateAsync(vm.Name);
            if (!result.Success)
            {
                ModelState.AddModelError("", result.ErrorMessage!);
                return View(vm);
            }

            TempData["Success"] = $"Đã thêm danh mục \"{vm.Name.Trim()}\".";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var category = await categoryService.GetByIdAsync(id);
            if (category == null)
                return NotFound();

            return View(new CategoryFormViewModel { Id = category.Id, Name = category.Name });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CategoryFormViewModel vm)
        {
            if (id != vm.Id)
                return BadRequest();

            if (!ModelState.IsValid)
                return View(vm);

            var result = await categoryService.UpdateAsync(id, vm.Name);
            if (!result.Success)
            {
                ModelState.AddModelError("", result.ErrorMessage!);
                return View(vm);
            }

            TempData["Success"] = "Đã cập nhật danh mục.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await categoryService.DeleteAsync(id);
            if (result.Success)
                TempData["Success"] = "Đã xóa danh mục.";
            else
                TempData["Error"] = result.ErrorMessage;

            return RedirectToAction(nameof(Index));
        }
    }
}
