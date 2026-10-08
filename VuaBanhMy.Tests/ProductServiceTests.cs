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

        // ===== Phần Admin =====

        // Tạo DB có sẵn 1 danh mục, trả về (db, categoryId)
        private static async Task<(Data.ApplicationDbContext db, int categoryId)> CreateDbWithCategoryAsync()
        {
            var db = TestDbFactory.Create();
            var category = new Category { Name = "Bánh mì" };
            db.Categories.Add(category);
            await db.SaveChangesAsync();
            return (db, category.Id);
        }

        [Fact]
        public async Task CreateAsync_CategoryNotFound_Fails()
        {
            using var db = TestDbFactory.Create();
            var service = new ProductService(db);

            var result = await service.CreateAsync(new Product { Name = "Bánh mì thịt", Price = 25000m, CategoryId = 999 });

            Assert.False(result.Success);
            Assert.Empty(db.Products);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-5000)]
        public async Task CreateAsync_PriceNotPositive_Fails(decimal price)
        {
            var (db, categoryId) = await CreateDbWithCategoryAsync();
            using var _ = db;
            var service = new ProductService(db);

            var result = await service.CreateAsync(new Product { Name = "Bánh mì thịt", Price = price, CategoryId = categoryId });

            Assert.False(result.Success);
            Assert.Empty(db.Products);
        }

        [Fact]
        public async Task CreateAsync_Valid_SavesAsAvailable()
        {
            var (db, categoryId) = await CreateDbWithCategoryAsync();
            using var _ = db;
            var service = new ProductService(db);

            // Cố tình gửi IsAvailable = false: món mới vẫn phải đang bán
            var result = await service.CreateAsync(new Product { Name = "Bánh mì thịt", Price = 25000m, CategoryId = categoryId, IsAvailable = false });

            Assert.True(result.Success);
            var saved = Assert.Single(db.Products);
            Assert.Equal("Bánh mì thịt", saved.Name);
            Assert.True(saved.IsAvailable);
        }

        [Fact]
        public async Task UpdateAsync_NotFound_Fails()
        {
            var (db, categoryId) = await CreateDbWithCategoryAsync();
            using var _ = db;
            var service = new ProductService(db);

            var result = await service.UpdateAsync(new Product { Id = 999, Name = "Bánh mì thịt", Price = 25000m, CategoryId = categoryId });

            Assert.False(result.Success);
        }

        [Fact]
        public async Task UpdateAsync_Valid_ChangesFieldsButKeepsAvailability()
        {
            var (db, categoryId) = await CreateDbWithCategoryAsync();
            using var _ = db;
            var product = new Product { Name = "Bánh mì thịt", Price = 25000m, CategoryId = categoryId, IsAvailable = false };
            db.Products.Add(product);
            await db.SaveChangesAsync();
            var service = new ProductService(db);

            // Dữ liệu từ form: không mang IsAvailable (mặc định true)
            var result = await service.UpdateAsync(new Product
            {
                Id = product.Id,
                Name = "Bánh mì thịt nướng",
                Description = "Thịt nướng than",
                Price = 30000m,
                CategoryId = categoryId
            });

            Assert.True(result.Success);
            var saved = Assert.Single(db.Products);
            Assert.Equal("Bánh mì thịt nướng", saved.Name);
            Assert.Equal("Thịt nướng than", saved.Description);
            Assert.Equal(30000m, saved.Price);
            Assert.False(saved.IsAvailable);
        }

        [Fact]
        public async Task ToggleAvailabilityAsync_FlipsFlag()
        {
            var (db, categoryId) = await CreateDbWithCategoryAsync();
            using var _ = db;
            var product = new Product { Name = "Bánh mì thịt", Price = 25000m, CategoryId = categoryId };
            db.Products.Add(product);
            await db.SaveChangesAsync();
            var service = new ProductService(db);

            await service.ToggleAvailabilityAsync(product.Id);
            Assert.False(product.IsAvailable);

            await service.ToggleAvailabilityAsync(product.Id);
            Assert.True(product.IsAvailable);
        }

        [Fact]
        public async Task GetAllAsync_IncludesUnavailableProducts()
        {
            var (db, categoryId) = await CreateDbWithCategoryAsync();
            using var _ = db;
            db.Products.AddRange(
                new Product { Name = "Bánh mì thịt", Price = 25000m, CategoryId = categoryId },
                new Product { Name = "Bánh mì cá", Price = 20000m, CategoryId = categoryId, IsAvailable = false });
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear(); // quên Include thì Category sẽ là null
            var service = new ProductService(db);

            var products = await service.GetAllAsync();

            Assert.Equal(2, products.Count);
            Assert.All(products, p => Assert.Equal("Bánh mì", p.Category!.Name)); // đã Include Category
        }
    }
}
