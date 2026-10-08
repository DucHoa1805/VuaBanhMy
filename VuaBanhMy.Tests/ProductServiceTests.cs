using VuaBanhMy.Models;
using VuaBanhMy.Services;

namespace VuaBanhMy.Tests
{
    public class ProductServiceTests
    {
        [Fact]
        public async Task GetMenuAsync_ExcludesUnavailableProducts()
        {
            // Arrange
            using var db = TestDbFactory.Create();
            var category = new Category { Name = "Bánh mì" };
            category.Products.Add(new Product { Name = "Bánh mì thịt", Price = 25000m });
            category.Products.Add(new Product { Name = "Bánh mì cá", Price = 20000m, IsAvailable = false });
            db.Categories.Add(category);
            await db.SaveChangesAsync();
            var service = new ProductService(db);

            // Act
            var menu = await service.GetMenuAsync();

            // Assert
            var product = Assert.Single(Assert.Single(menu).Products);
            Assert.Equal("Bánh mì thịt", product.Name);
        }

        [Fact]
        public async Task GetMenuAsync_GroupsProductsByCategory()
        {
            using var db = TestDbFactory.Create();
            var banhMi = new Category { Name = "Bánh mì" };
            banhMi.Products.Add(new Product { Name = "Bánh mì thịt", Price = 25000m });
            banhMi.Products.Add(new Product { Name = "Bánh mì pate", Price = 20000m });
            var doUong = new Category { Name = "Đồ uống" };
            doUong.Products.Add(new Product { Name = "Trà tắc", Price = 12000m });
            db.Categories.AddRange(banhMi, doUong);
            await db.SaveChangesAsync();
            var service = new ProductService(db);

            var menu = await service.GetMenuAsync();

            Assert.Equal(2, menu.Count);
            var banhMiGroup = menu.Single(m => m.CategoryId == banhMi.Id);
            Assert.Equal(["Bánh mì pate", "Bánh mì thịt"], banhMiGroup.Products.Select(p => p.Name).Order());
            var doUongGroup = menu.Single(m => m.CategoryId == doUong.Id);
            Assert.Equal("Trà tắc", Assert.Single(doUongGroup.Products).Name);
        }

        [Fact]
        public async Task GetMenuAsync_ExcludesEmptyCategories()
        {
            using var db = TestDbFactory.Create();
            var banhMi = new Category { Name = "Bánh mì" };
            banhMi.Products.Add(new Product { Name = "Bánh mì thịt", Price = 25000m });
            var empty = new Category { Name = "Món thêm" };              // không có món nào
            var allHidden = new Category { Name = "Đồ uống" };           // chỉ có món đã ẩn
            allHidden.Products.Add(new Product { Name = "Trà tắc", Price = 12000m, IsAvailable = false });
            db.Categories.AddRange(banhMi, empty, allHidden);
            await db.SaveChangesAsync();
            var service = new ProductService(db);

            var menu = await service.GetMenuAsync();

            Assert.Equal("Bánh mì", Assert.Single(menu).CategoryName);
        }
    }
}
