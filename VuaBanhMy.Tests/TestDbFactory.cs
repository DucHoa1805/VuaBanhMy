using Microsoft.EntityFrameworkCore;
using VuaBanhMy.Data;

namespace VuaBanhMy.Tests
{
    public static class TestDbFactory
    {
        public static ApplicationDbContext Create()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()) // mỗi test 1 DB riêng
                .Options;
            return new ApplicationDbContext(options);
        }
    }
}
