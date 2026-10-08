using VuaBanhMy.Models;
using VuaBanhMy.Services;

namespace VuaBanhMy.Tests
{
    public class CategoryServiceTests
    {
        [Fact]
        public async Task CreateAsync_ValidName_SavesCategory()
        {
            // Arrange
            using var db = TestDbFactory.Create();
            var service = new CategoryService(db);

            // Act
            var result = await service.CreateAsync("Bánh mì");

            // Assert
            Assert.True(result.Success);
            Assert.Single(db.Categories);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task CreateAsync_EmptyName_Fails(string name)
        {
            using var db = TestDbFactory.Create();
            var service = new CategoryService(db);

            var result = await service.CreateAsync(name);

            Assert.False(result.Success);
            Assert.Empty(db.Categories);
        }

        [Fact]
        public async Task CreateAsync_DuplicateName_Fails()
        {
            using var db = TestDbFactory.Create();
            db.Categories.Add(new Category { Name = "Bánh mì" });
            await db.SaveChangesAsync();
            var service = new CategoryService(db);

            var result = await service.CreateAsync("bánh mì ");

            Assert.False(result.Success);
            Assert.Single(db.Categories);
        }

        [Fact]
        public async Task UpdateAsync_NotFound_Fails()
        {
            using var db = TestDbFactory.Create();
            var service = new CategoryService(db);

            var result = await service.UpdateAsync(999, "Đồ uống");

            Assert.False(result.Success);
        }

        [Fact]
        public async Task DeleteAsync_CategoryHasProducts_Fails()
        {
            using var db = TestDbFactory.Create();
            var category = new Category { Name = "Bánh mì" };
            category.Products.Add(new Product { Name = "Bánh mì thịt", Price = 25000m });
            db.Categories.Add(category);
            await db.SaveChangesAsync();
            var service = new CategoryService(db);

            var result = await service.DeleteAsync(category.Id);

            Assert.False(result.Success);
            Assert.Single(db.Categories);
        }

        [Fact]
        public async Task DeleteAsync_EmptyCategory_Succeeds()
        {
            using var db = TestDbFactory.Create();
            var category = new Category { Name = "Món thêm" };
            db.Categories.Add(category);
            await db.SaveChangesAsync();
            var service = new CategoryService(db);

            var result = await service.DeleteAsync(category.Id);

            Assert.True(result.Success);
            Assert.Empty(db.Categories);
        }
    }
}
