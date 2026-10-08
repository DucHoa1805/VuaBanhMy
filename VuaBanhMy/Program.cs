using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using VuaBanhMy.Data;
using VuaBanhMy.Models;
using VuaBanhMy.Services;
namespace VuaBanhMy
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(connectionString));
            builder.Services.AddDatabaseDeveloperPageExceptionFilter();

            builder.Services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = false)
                .AddRoles<IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>();
            builder.Services.AddControllersWithViews();

            builder.Services.AddScoped<ICategoryService, CategoryService>();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseMigrationsEndPoint();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapStaticAssets();
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}")
                .WithStaticAssets();
            app.MapRazorPages()
               .WithStaticAssets();

            // Seed role + tài khoản admin
            using (var scope = app.Services.CreateScope())
            {
                var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

                foreach (var role in new[] { "Admin", "Staff", "Customer", "Shipper" })
                    if (!await roleManager.RoleExistsAsync(role))
                        await roleManager.CreateAsync(new IdentityRole(role));

                var adminEmail = "admin@banhmyking.vn";
                if (await userManager.FindByEmailAsync(adminEmail) is null)
                {
                    var admin = new ApplicationUser
                    {
                        UserName = adminEmail,
                        Email = adminEmail,
                        EmailConfirmed = true,
                        FullName = "Quản trị viên"
                    };

                    var result = await userManager.CreateAsync(admin, "Admin@123");
                    if (!result.Succeeded)
                        throw new InvalidOperationException(
                            "Không tạo được tài khoản admin: " +
                            string.Join("; ", result.Errors.Select(e => e.Description)));

                    await userManager.AddToRoleAsync(admin, "Admin");
                }
            }

            await app.RunAsync();
        }
    }
}