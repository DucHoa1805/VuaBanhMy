# Giai đoạn 0 + 1: Nền móng & Thực đơn — Lộ trình học

> **Cách dùng lộ trình này:** Mỗi task có: *Mục tiêu → Kiến thức cần biết → Việc cần làm (kèm gợi ý) → Kiểm tra → Câu hỏi tự kiểm tra → Commit*.
> Làm xong một task, nhắn Claude "xong Task X" để được review trước khi sang task tiếp theo.
> Đánh dấu `- [x]` khi hoàn thành từng bước.

**Mục tiêu:** Có tài khoản 4 role chạy đúng, khách xem được menu theo danh mục, Admin quản lý được danh mục và món (kèm ảnh).

**Kiến trúc:** Controller → Service (interface, đăng ký DI) → `ApplicationDbContext` → SQL Server LocalDB. Trang Admin nằm trong `Areas/Admin`.

**Công nghệ:** ASP.NET Core MVC .NET 10, Identity, EF Core 10 (SQL Server), xUnit + EF Core InMemory.

**Spec:** `docs/superpowers/specs/2026-10-06-vuabanhmy-design.md` (mục 2, 3, 5, 6, 7, 9).

## Ràng buộc chung

- Tên class/cột/biến tiếng Anh; chữ hiển thị cho người dùng tiếng Việt.
- Mọi truy vấn DB dùng `async/await`.
- Mọi form POST có `[ValidateAntiForgeryToken]`.
- Không bind entity trực tiếp vào form — dùng ViewModel.
- Không xóa `Product`, chỉ đặt `IsAvailable = false`.
- `Price` kiểu `decimal(18,2)`.
- Commit sau mỗi task chạy được.

## Cấu trúc file sau giai đoạn 1

```
VuaBanhMy/                         (thư mục solution)
├── .gitignore
├── VuaBanhMy.slnx
├── docs/superpowers/...
├── VuaBanhMy/
│   ├── Program.cs                         (sửa)
│   ├── Data/ApplicationDbContext.cs       (sửa)
│   ├── Models/
│   │   ├── ApplicationUser.cs             (mới)
│   │   ├── Category.cs                    (mới)
│   │   ├── Product.cs                     (mới)
│   │   └── ViewModels/
│   │       ├── MenuCategoryViewModel.cs   (mới)
│   │       ├── CategoryFormViewModel.cs   (mới)
│   │       └── ProductFormViewModel.cs    (mới)
│   ├── Services/
│   │   ├── ServiceResult.cs               (mới)
│   │   ├── ICategoryService.cs / CategoryService.cs
│   │   ├── IProductService.cs  / ProductService.cs
│   │   └── IImageService.cs    / ImageService.cs
│   ├── Controllers/MenuController.cs      (mới)
│   ├── Views/Menu/Index.cshtml            (mới)
│   ├── Views/Shared/_LoginPartial.cshtml  (sửa)
│   ├── Areas/Identity/Pages/Account/Register.cshtml(.cs)  (scaffold rồi sửa)
│   └── Areas/Admin/
│       ├── Controllers/CategoriesController.cs, ProductsController.cs
│       └── Views/_ViewImports.cshtml, _ViewStart.cshtml, Categories/*, Products/*
└── VuaBanhMy.Tests/
    ├── TestDbFactory.cs
    ├── CategoryServiceTests.cs
    └── ProductServiceTests.cs
```

---

# GIAI ĐOẠN 0 — CHUẨN BỊ

## Task 0.1: Khởi tạo Git

**Mục tiêu:** Dự án có lịch sử commit ngay từ đầu (nhà tuyển dụng sẽ xem GitHub của bạn).

**Kiến thức cần biết:**
- `.gitignore` liệt kê file **không** đưa lên git: `bin/`, `obj/`, `.vs/`, file `.user`. Đây là file sinh ra khi build hoặc file cá nhân của máy bạn.
- Một commit = một thay đổi có ý nghĩa, message nói rõ "đã làm gì".

**Việc cần làm** (mở terminal ở `C:\Workspace\VuaBanhMy`, thư mục chứa `VuaBanhMy.slnx`):
- [ ] `git init`
- [ ] `dotnet new gitignore` — tạo sẵn `.gitignore` chuẩn cho .NET
- [ ] `git status` — kiểm tra **không** thấy `bin/`, `obj/`, `.vs/` trong danh sách
- [ ] `git add .` rồi `git commit -m "Initial ASP.NET Core MVC project with Identity"`

**Kiểm tra:** `git log --oneline` hiện 1 commit.

**Câu hỏi tự kiểm tra:** Vì sao không commit thư mục `bin/` và `obj/`?

---

## Task 0.2: Sửa cấu hình Identity & thêm role

**Mục tiêu:** Đăng ký xong đăng nhập được ngay; có đủ 4 role.

**Kiến thức cần biết:**
- `RequireConfirmedAccount = true` nghĩa là phải bấm link xác nhận trong email mới đăng nhập được. Dự án chưa có gửi email → khách bị kẹt.
- Đoạn seed trong `Program.cs` chạy **mỗi lần khởi động app**; `RoleExistsAsync` giúp không tạo trùng.
- **Migration** = file C# mô tả thay đổi cấu trúc DB. `Update-Database` áp dụng migration vào DB thật.

**Việc cần làm:**
- [ ] Trong `Program.cs`, đổi `RequireConfirmedAccount = true` thành `false`.
- [ ] Thêm `"Staff"` và `"Shipper"` vào mảng role đang seed.
- [ ] Mở **Package Manager Console** (Tools → NuGet Package Manager), chạy `Update-Database` (hoặc terminal trong thư mục project: `dotnet ef database update`).
- [ ] Chạy app (F5), đăng nhập `admin@banhmyking.vn` / `Admin@123`.

**Kiểm tra:**
- Đăng nhập admin thành công, góc phải hiện "Hello admin@banhmyking.vn!".
- Trong **SQL Server Object Explorer** (View → SQL Server Object Explorer → `(localdb)\MSSQLLocalDB`), bảng `AspNetRoles` có 4 dòng.

**Câu hỏi tự kiểm tra:** Nếu bỏ dòng `if (!await roleManager.RoleExistsAsync(role))` thì lần chạy thứ 2 chuyện gì xảy ra?

**Commit:** `Disable account confirmation and seed Staff, Shipper roles`

---

## Task 0.3: Mở rộng tài khoản thành `ApplicationUser`

**Mục tiêu:** Tài khoản có thêm `FullName`, `Address` (spec mục 3.2).

**Kiến thức cần biết:**
- `IdentityUser` là class có sẵn. Muốn thêm cột → tạo class **kế thừa** nó, rồi báo cho Identity và DbContext dùng class mới.
- Generic: `IdentityDbContext<ApplicationUser>` nghĩa là "DbContext của Identity, nhưng bảng user dùng class `ApplicationUser`".
- Phải đổi **mọi chỗ** đang dùng `IdentityUser` sang `ApplicationUser`, nếu sót sẽ lỗi lúc chạy: *"No service for type UserManager&lt;IdentityUser&gt;"*.

**Files:**
- Tạo: `Models/ApplicationUser.cs`
- Sửa: `Data/ApplicationDbContext.cs`, `Program.cs` (2 chỗ: `AddDefaultIdentity` và đoạn seed), `Views/Shared/_LoginPartial.cshtml`

**Việc cần làm:**
- [ ] Tạo `ApplicationUser`:
  ```csharp
  public class ApplicationUser : IdentityUser
  {
      [MaxLength(100)]
      public string FullName { get; set; } = string.Empty;
      // TODO của bạn: thêm Address (nullable, MaxLength 255)
  }
  ```
- [ ] `ApplicationDbContext` kế thừa `IdentityDbContext<ApplicationUser>` thay vì `IdentityDbContext`.
- [ ] `Program.cs`: `AddDefaultIdentity<ApplicationUser>`, `UserManager<ApplicationUser>`, `new ApplicationUser { ..., FullName = "Quản trị viên" }`.
- [ ] `_LoginPartial.cshtml`: đổi 2 dòng `@inject` sang `ApplicationUser` (nhớ `@using VuaBanhMy.Models`).
- [ ] Dùng Ctrl+Shift+F tìm `IdentityUser` trong toàn project — chỉ còn lại đúng ở `ApplicationUser : IdentityUser` và trong thư mục `Migrations`.
- [ ] `Add-Migration AddApplicationUserProfile` → **mở file migration ra đọc**: phải thấy `AddColumn` cho `FullName` và `Address`.
- [ ] `Update-Database`.

**Kiểm tra:** Bảng `AspNetUsers` có 2 cột mới; đăng nhập admin vẫn được.

**Câu hỏi tự kiểm tra:** Tại sao nên làm việc này ngay từ đầu thay vì sau khi đã có bảng `Orders` tham chiếu tới user?

**Commit:** `Add ApplicationUser with FullName and Address`

---

## Task 0.4: Trang đăng ký có Họ tên & tự gán role Customer

**Mục tiêu:** Khách đăng ký → nhập họ tên → tự động có role `Customer`.

**Kiến thức cần biết:**
- Các trang Identity (Login, Register...) nằm sẵn trong thư viện, bạn không thấy code. **Scaffold** = sinh ra bản copy vào project để bạn sửa.
- Trang Identity là **Razor Pages** (`.cshtml` + `.cshtml.cs`), không phải MVC — `InputModel` đóng vai trò ViewModel, `OnPostAsync` đóng vai trò action POST.

**Việc cần làm:**
- [ ] Chuột phải project → Add → New Scaffolded Item → **Identity** → chọn **Account\Register**, DbContext chọn `ApplicationDbContext`.
  (Visual Studio có thể tự cài package `Microsoft.VisualStudio.Web.CodeGeneration.Design` — bình thường.)
- [ ] Mở `Areas/Identity/Pages/Account/Register.cshtml.cs`:
  - Thêm vào `InputModel` thuộc tính `FullName` có `[Required]`, `[Display(Name = "Họ tên")]`, `[StringLength(100)]`.
  - Trong `OnPostAsync`, sau khi tạo `user`, gán `user.FullName = Input.FullName;` (trước `CreateAsync`).
  - Ngay sau `if (result.Succeeded)`, gọi `await _userManager.AddToRoleAsync(user, "Customer");`
- [ ] Mở `Register.cshtml`, thêm ô nhập `FullName` (copy khối `form-floating` của Email rồi sửa).
- [ ] Đọc lướt phần còn lại của `OnPostAsync` để hiểu luồng đăng ký.

**Kiểm tra:**
- Đăng ký tài khoản mới `khach1@test.com` → được đăng nhập luôn.
- Trong DB: `AspNetUsers` có `FullName` đúng; bảng `AspNetUserRoles` có dòng nối user này với role `Customer`.

**Câu hỏi tự kiểm tra:** Nếu kẻ xấu tự thêm field `Role=Admin` vào form đăng ký thì có thành Admin không? Vì sao?

**Commit:** `Scaffold Register page, add FullName and assign Customer role`

---

# GIAI ĐOẠN 1 — THỰC ĐƠN

## Task 1.1: Entity `Category`, `Product` và dữ liệu mẫu

**Mục tiêu:** Có 2 bảng `Categories`, `Products` với vài món mẫu.

**Kiến thức cần biết:**
- **Navigation property**: `Product.Category` (một) và `Category.Products` (nhiều) cho EF biết quan hệ 1-n. `CategoryId` là khóa ngoại.
- **Fluent API** trong `OnModelCreating` để cấu hình chi tiết (độ chính xác decimal...). Với Identity **bắt buộc gọi `base.OnModelCreating(builder)` đầu tiên**, nếu không các bảng Identity bị hỏng cấu hình.
- `HasData(...)` = seed dữ liệu bằng migration (phải tự đặt `Id`).

**Files:**
- Tạo: `Models/Category.cs`, `Models/Product.cs`
- Sửa: `Data/ApplicationDbContext.cs`

**Việc cần làm:**
- [ ] `Category`: `Id`, `Name` (`[Required]`, `[MaxLength(100)]`), `ICollection<Product> Products`.
- [ ] `Product` theo đúng bảng spec mục 3.2: `Id`, `Name` (150), `Description` (500, nullable), `Price`, `ImageUrl` (nullable), `IsAvailable` (mặc định `true`), `CategoryId`, `Category?`.
- [ ] Trong `ApplicationDbContext`: thêm `DbSet<Category> Categories` và `DbSet<Product> Products`.
- [ ] Override `OnModelCreating`:
  ```csharp
  protected override void OnModelCreating(ModelBuilder builder)
  {
      base.OnModelCreating(builder);

      builder.Entity<Product>()
          .Property(p => p.Price)
          .HasPrecision(18, 2);

      builder.Entity<Product>()
          .HasOne(p => p.Category)
          .WithMany(c => c.Products)
          .HasForeignKey(p => p.CategoryId)
          .OnDelete(DeleteBehavior.Restrict); // không cho xóa danh mục còn món

      // TODO của bạn: HasData cho 3 Category (Bánh mì, Đồ uống, Món thêm)
      //               và ít nhất 5 Product thuộc các danh mục đó
  }
  ```
- [ ] `Add-Migration AddCategoriesAndProducts` → đọc file migration (thấy `CreateTable` + `InsertData`) → `Update-Database`.

**Kiểm tra:** 2 bảng mới có dữ liệu mẫu; cột `Price` kiểu `decimal(18,2)`.

**Câu hỏi tự kiểm tra:** `DeleteBehavior.Restrict` khác `Cascade` thế nào? Nếu để `Cascade` thì xóa danh mục "Bánh mì" sẽ gây hậu quả gì?

**Commit:** `Add Category and Product entities with seed data`

---

## Task 1.2: Project test + `ServiceResult` + `CategoryService` (TDD)

**Mục tiêu:** Có project test chạy được; `CategoryService` có đủ CRUD, được viết theo kiểu **test trước, code sau**.

**Kiến thức cần biết:**
- **Dependency Injection:** service nhận `ApplicationDbContext` qua constructor; ASP.NET tự đưa vào. `AddScoped` = mỗi HTTP request một instance (cùng vòng đời với DbContext).
- **Interface** cho phép controller phụ thuộc vào "hợp đồng" chứ không phải class cụ thể → dễ thay thế, dễ test.
- **TDD:** viết test (đỏ) → viết code tối thiểu cho test xanh → dọn code. Test chạy với **EF Core InMemory** — DB giả trong RAM, mỗi test một DB riêng.
- Mẫu test **Arrange – Act – Assert**.

**Files:**
- Tạo: `VuaBanhMy.Tests/` (project), `VuaBanhMy.Tests/TestDbFactory.cs`, `VuaBanhMy.Tests/CategoryServiceTests.cs`
- Tạo: `Services/ServiceResult.cs`, `Services/ICategoryService.cs`, `Services/CategoryService.cs`
- Sửa: `Program.cs` (đăng ký DI)

**Interfaces (tên phải đúng để các task sau dùng):**
```csharp
public class ServiceResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public static ServiceResult Ok() => new() { Success = true };
    public static ServiceResult Fail(string message) => new() { Success = false, ErrorMessage = message };
}

public interface ICategoryService
{
    Task<List<Category>> GetAllAsync();
    Task<Category?> GetByIdAsync(int id);
    Task<ServiceResult> CreateAsync(string name);
    Task<ServiceResult> UpdateAsync(int id, string name);
    Task<ServiceResult> DeleteAsync(int id);
}
```

**Việc cần làm:**
- [ ] Tạo project test (terminal ở thư mục solution):
  ```
  dotnet new xunit -n VuaBanhMy.Tests -o VuaBanhMy.Tests
  dotnet sln VuaBanhMy.slnx add VuaBanhMy.Tests/VuaBanhMy.Tests.csproj
  dotnet add VuaBanhMy.Tests reference VuaBanhMy/VuaBanhMy.csproj
  dotnet add VuaBanhMy.Tests package Microsoft.EntityFrameworkCore.InMemory --version 10.0.11
  ```
- [ ] `TestDbFactory.cs`:
  ```csharp
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
  ```
- [ ] Tạo `ServiceResult`, `ICategoryService` như trên; tạo `CategoryService` với các method chỉ `throw new NotImplementedException();`.
- [ ] Viết test đầu tiên (mẫu):
  ```csharp
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
  ```
- [ ] Chạy `dotnet test` → **phải ĐỎ** (NotImplementedException). Đây là bước quan trọng: chứng minh test thật sự kiểm tra code.
- [ ] Viết code `CreateAsync` cho test xanh.
- [ ] Lặp lại vòng đỏ → xanh cho từng test sau (tự viết theo mẫu trên):
  - `CreateAsync_EmptyName_Fails` — tên rỗng/khoảng trắng → `Success == false`, DB không có dòng nào.
  - `CreateAsync_DuplicateName_Fails` — đã có "Bánh mì" thì tạo "bánh mì " (khác hoa thường, thừa khoảng trắng) → thất bại.
  - `UpdateAsync_NotFound_Fails` — id không tồn tại → thất bại.
  - `DeleteAsync_CategoryHasProducts_Fails` — danh mục còn món → thất bại, danh mục vẫn còn.
  - `DeleteAsync_EmptyCategory_Succeeds`.
- [ ] Đăng ký DI trong `Program.cs`: `builder.Services.AddScoped<ICategoryService, CategoryService>();`

**Kiểm tra:** `dotnet test` → 6 test PASS.

**Câu hỏi tự kiểm tra:**
1. `AddScoped`, `AddTransient`, `AddSingleton` khác nhau thế nào? Vì sao service dùng DbContext không nên là Singleton?
2. Vì sao mỗi test dùng một tên DB InMemory khác nhau?

**Commit:** `Add test project, ServiceResult and CategoryService with tests`

---

## Task 1.3: `ProductService` — phần đọc menu (TDD)

**Mục tiêu:** Lấy được menu nhóm theo danh mục, **chỉ món đang bán**.

**Kiến thức cần biết:**
- `Include(...)` để EF nạp kèm dữ liệu liên quan (eager loading).
- LINQ `Where`, `OrderBy`, `Select` — `Select` sang ViewModel giúp chỉ lấy cột cần thiết.
- `AsNoTracking()` cho truy vấn chỉ đọc → nhanh hơn.

**Files:**
- Tạo: `Models/ViewModels/MenuCategoryViewModel.cs`, `Services/IProductService.cs`, `Services/ProductService.cs`, `VuaBanhMy.Tests/ProductServiceTests.cs`
- Sửa: `Program.cs` (DI)

**Interfaces:**
```csharp
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

public interface IProductService
{
    // Khách
    Task<List<MenuCategoryViewModel>> GetMenuAsync();
    // Admin (dùng ở Task 1.6)
    Task<List<Product>> GetAllAsync();                 // gồm cả món đã ẩn, Include Category
    Task<Product?> GetByIdAsync(int id);
    Task<ServiceResult> CreateAsync(Product product);
    Task<ServiceResult> UpdateAsync(Product product);
    Task<ServiceResult> ToggleAvailabilityAsync(int id);
}
```
(Ở task này chỉ cài `GetMenuAsync`; các method Admin để `NotImplementedException`.)

**Việc cần làm — viết test trước:**
- [ ] `GetMenuAsync_ExcludesUnavailableProducts` — 1 món `IsAvailable=false` không xuất hiện.
- [ ] `GetMenuAsync_GroupsProductsByCategory` — 2 danh mục, mỗi danh mục đúng món của nó.
- [ ] `GetMenuAsync_ExcludesEmptyCategories` — danh mục không có món đang bán thì không hiện.
- [ ] Chạy đỏ → viết `GetMenuAsync` → xanh.
  Gợi ý hướng làm: truy vấn `Categories` → `Select` sang `MenuCategoryViewModel`, trong đó `Products = c.Products.Where(p => p.IsAvailable).Select(...)`, rồi lọc danh mục có `Products.Any()`.
- [ ] Đăng ký `AddScoped<IProductService, ProductService>()`.

**Kiểm tra:** `dotnet test` → toàn bộ PASS.

**Câu hỏi tự kiểm tra:** Nếu không dùng `Select` mà trả thẳng entity `Product` ra View, có rủi ro gì?

**Commit:** `Add ProductService.GetMenuAsync with tests`

---

## Task 1.4: Trang Menu cho khách

**Mục tiêu:** Truy cập `/Menu` thấy món theo từng danh mục, dạng thẻ (card) Bootstrap.

**Kiến thức cần biết:**
- Controller nhận service qua constructor (DI) — **không** `new ProductService(...)`.
- View strongly-typed: `@model List<MenuCategoryViewModel>`.
- Định dạng tiền Việt: `@product.Price.ToString("N0") đ`.

**Files:**
- Tạo: `Controllers/MenuController.cs`, `Views/Menu/Index.cshtml`
- Sửa: `Views/Shared/_Layout.cshtml` (thêm link "Thực đơn" lên navbar)

**Việc cần làm:**
- [ ] `MenuController` có constructor nhận `IProductService`; action `Index()` gọi `GetMenuAsync()` và trả `View(menu)`.
- [ ] View: vòng `foreach` danh mục → tiêu đề `<h3>`; bên trong `row` Bootstrap, mỗi món là một `card` (ảnh, tên, mô tả, giá). Nếu `ImageUrl` null thì dùng ảnh mặc định `/images/no-image.png` (tự thêm một ảnh vào `wwwroot/images/`).
- [ ] Nếu menu rỗng: hiện "Hiện chưa có món nào".
- [ ] Thêm link "Thực đơn" vào navbar (`asp-controller="Menu" asp-action="Index"`).
- [ ] Nút "Thêm vào giỏ" **chưa làm** (giai đoạn 2).

**Kiểm tra:**
- Mở `/Menu` khi **chưa đăng nhập** → vẫn xem được.
- Vào DB đặt một món `IsAvailable = 0` → F5 → món đó biến mất.
- Thu nhỏ trình duyệt cỡ điện thoại → card xếp thành 1 cột.

**Câu hỏi tự kiểm tra:** Khi request `/Menu` đến, ai tạo ra `MenuController`, `ProductService` và `ApplicationDbContext`? Theo thứ tự nào?

**Commit:** `Add customer menu page`

---

## Task 1.5: Area Admin + quản lý Danh mục

**Mục tiêu:** Admin vào `/Admin/Categories` để xem / thêm / sửa / xóa danh mục. Người không phải Admin bị chặn.

**Kiến thức cần biết:**
- **Area** = khu vực riêng có Controllers/Views của nó. Controller phải có `[Area("Admin")]` và cần route `areas` trong `Program.cs`.
- `[Authorize(Roles = "Admin")]` đặt trên class → áp dụng cho mọi action.
- Mẫu **Post/Redirect/Get**: POST thành công → `RedirectToAction` (tránh F5 gửi lại form). Thông báo qua `TempData["Success"]` / `TempData["Error"]`.
- `ModelState.IsValid` kiểm tra Data Annotations trên ViewModel.

**Files:**
- Tạo: `Areas/Admin/Controllers/CategoriesController.cs`
- Tạo: `Areas/Admin/Views/_ViewImports.cshtml`, `Areas/Admin/Views/_ViewStart.cshtml`
- Tạo: `Areas/Admin/Views/Categories/Index.cshtml`, `Create.cshtml`, `Edit.cshtml`
- Tạo: `Models/ViewModels/CategoryFormViewModel.cs`
- Sửa: `Program.cs` (route), `Views/Shared/_Layout.cshtml` (link "Quản trị" chỉ hiện với Admin + hiển thị TempData)

**Việc cần làm:**
- [ ] Thêm route **trước** route `default`:
  ```csharp
  app.MapControllerRoute(
      name: "areas",
      pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");
  ```
- [ ] `_ViewImports.cshtml` của Area: copy từ `Views/_ViewImports.cshtml`. `_ViewStart.cshtml`: `Layout = "_Layout";`
- [ ] `CategoryFormViewModel`: `Id`, `Name` với `[Required(ErrorMessage = "Vui lòng nhập tên danh mục")]`, `[StringLength(100)]`.
- [ ] `CategoriesController` (`[Area("Admin")]`, `[Authorize(Roles = "Admin")]`, nhận `ICategoryService`):
  - `Index` (GET) — danh sách
  - `Create` (GET + POST)
  - `Edit` (GET + POST) — GET trả `NotFound()` nếu id không tồn tại
  - `Delete` (POST only, có nút trong trang Index, xác nhận bằng `onclick="return confirm('...')"`)
  - Mọi POST có `[ValidateAntiForgeryToken]`; nếu `ServiceResult` thất bại → `ModelState.AddModelError("", result.ErrorMessage!)` (với Create/Edit) hoặc `TempData["Error"]` (với Delete).
- [ ] `_Layout.cshtml`: hiển thị alert Bootstrap khi có `TempData["Success"]` / `TempData["Error"]`; link "Quản trị" bọc trong `@if (User.IsInRole("Admin"))`.

**Kiểm tra:**
- Admin: thêm "Combo", sửa thành "Combo tiết kiệm", xóa được; xóa "Bánh mì" (còn món) → hiện lỗi, không xóa.
- Bỏ trống tên → hiện lỗi validation.
- Đăng nhập `khach1@test.com`, gõ tay `/Admin/Categories` → trang **Access denied**.
- Chưa đăng nhập gõ `/Admin/Categories` → bị chuyển sang trang Login.

**Câu hỏi tự kiểm tra:**
1. Vì sao Delete chỉ nhận POST, không dùng link GET `/Admin/Categories/Delete/5`?
2. `[ValidateAntiForgeryToken]` chống loại tấn công nào?

**Commit:** `Add Admin area with category management`

---

## Task 1.6: Admin quản lý Món ăn (TDD phần service)

**Mục tiêu:** Admin xem tất cả món (cả món đã ẩn), thêm, sửa, ẩn/hiện món. Chưa có upload ảnh (task sau).

**Kiến thức cần biết:**
- **Over-posting:** nếu bind thẳng entity `Product`, kẻ xấu có thể gửi thêm field không có trên form. ViewModel chỉ chứa field cho phép sửa.
- Dropdown danh mục: `SelectList` + tag helper `<select asp-for="CategoryId" asp-items="...">`.
- Soft delete: không có nút Xóa, chỉ có nút "Ẩn / Hiện".

**Files:**
- Sửa: `Services/ProductService.cs`, `VuaBanhMy.Tests/ProductServiceTests.cs`
- Tạo: `Models/ViewModels/ProductFormViewModel.cs`
- Tạo: `Areas/Admin/Controllers/ProductsController.cs`, `Areas/Admin/Views/Products/Index.cshtml`, `Create.cshtml`, `Edit.cshtml`

**Interfaces:**
```csharp
public class ProductFormViewModel
{
    public int Id { get; set; }
    [Required, StringLength(150)] public string Name { get; set; } = string.Empty;
    [StringLength(500)] public string? Description { get; set; }
    [Range(1000, 10_000_000, ErrorMessage = "Giá từ 1.000đ đến 10.000.000đ")]
    public decimal Price { get; set; }
    [Required] public int? CategoryId { get; set; }
    public string? ExistingImageUrl { get; set; }   // dùng ở Task 1.7
    public IFormFile? ImageFile { get; set; }        // dùng ở Task 1.7
    public List<SelectListItem> Categories { get; set; } = new();
}
```

**Việc cần làm — service (test trước):**
- [ ] `CreateAsync_CategoryNotFound_Fails`
- [ ] `CreateAsync_PriceNotPositive_Fails`
- [ ] `CreateAsync_Valid_SavesAsAvailable` — món mới luôn `IsAvailable == true`.
- [ ] `UpdateAsync_NotFound_Fails`
- [ ] `UpdateAsync_Valid_ChangesFieldsButKeepsAvailability` — sửa tên/giá không làm đổi `IsAvailable`.
- [ ] `ToggleAvailabilityAsync_FlipsFlag` — gọi 2 lần thì về giá trị ban đầu.
- [ ] `GetAllAsync_IncludesUnavailableProducts`
- [ ] Đỏ → cài đặt → xanh.
  Gợi ý `UpdateAsync`: tìm entity trong DB theo `product.Id`, **chép từng field được phép** (Name, Description, Price, CategoryId, ImageUrl) sang entity đó rồi `SaveChangesAsync()` — không dùng `_db.Update(product)`.

**Việc cần làm — controller & view:**
- [ ] `ProductsController` (`[Area("Admin")]`, `[Authorize(Roles = "Admin")]`, nhận `IProductService` + `ICategoryService`).
- [ ] Viết một hàm private `LoadCategoriesAsync(ProductFormViewModel vm)` để đổ dropdown — gọi ở GET **và** khi POST lỗi (quên gọi khi POST lỗi → dropdown trống, lỗi rất hay gặp).
- [ ] `Index`: bảng gồm ảnh nhỏ, tên, danh mục, giá, trạng thái (badge "Đang bán"/"Đã ẩn"), nút Sửa, nút Ẩn/Hiện (form POST).
- [ ] `Create`, `Edit` (GET + POST): map ViewModel ↔ entity trong controller.

**Kiểm tra:**
- `dotnet test` toàn bộ PASS.
- Thêm món mới → hiện ở `/Menu`. Ẩn món → biến mất khỏi `/Menu` nhưng vẫn ở trang Admin. Hiện lại → quay về menu.
- Nhập giá 0 hoặc bỏ trống danh mục → lỗi validation, dropdown vẫn còn đủ danh mục.

**Câu hỏi tự kiểm tra:** Vì sao `UpdateAsync` nên chép từng field thay vì `_db.Update(product)`?

**Commit:** `Add admin product management with soft delete`

---

## Task 1.7: Upload ảnh món

**Mục tiêu:** Khi thêm/sửa món, Admin chọn được ảnh; ảnh hiển thị ở menu.

**Kiến thức cần biết:**
- Form có file phải có `enctype="multipart/form-data"`. ASP.NET bind file vào `IFormFile`.
- **Không tin tên file người dùng gửi**: tự đặt tên mới bằng `Guid` để tránh trùng và tránh tên độc hại (`../../web.config`).
- Kiểm tra đuôi file (whitelist `.jpg .jpeg .png .webp`) và dung lượng (≤ 2 MB).
- `IWebHostEnvironment.WebRootPath` = đường dẫn thật tới `wwwroot`.

**Files:**
- Tạo: `Services/IImageService.cs`, `Services/ImageService.cs`
- Sửa: `Program.cs` (DI), `Areas/Admin/Controllers/ProductsController.cs`, `Areas/Admin/Views/Products/Create.cshtml`, `Edit.cshtml`, `.gitignore`

**Interfaces:**
```csharp
public class ServiceResult<T> : ServiceResult
{
    public T? Data { get; init; }
    public static ServiceResult<T> Ok(T data) => new() { Success = true, Data = data };
    public static new ServiceResult<T> Fail(string message) => new() { Success = false, ErrorMessage = message };
}

public interface IImageService
{
    // Trả về đường dẫn web, VD "/images/products/3f2a....jpg"
    Task<ServiceResult<string>> SaveProductImageAsync(IFormFile file);
}
```
(Thêm `ServiceResult<T>` vào cùng file `ServiceResult.cs`.)

**Việc cần làm:**
- [ ] `ImageService` nhận `IWebHostEnvironment`; kiểm tra file null/rỗng, đuôi file, dung lượng → `Fail` với thông báo tiếng Việt.
- [ ] Tạo thư mục `wwwroot/images/products` nếu chưa có (`Directory.CreateDirectory`), lưu bằng `FileStream` + `file.CopyToAsync(stream)`.
- [ ] Trong `ProductsController` POST Create/Edit: nếu `ImageFile != null` → gọi `SaveProductImageAsync`; lỗi → `ModelState.AddModelError(nameof(vm.ImageFile), ...)`. Khi Edit không chọn ảnh mới → giữ `ExistingImageUrl` (hidden input).
- [ ] View: thêm `enctype`, `<input asp-for="ImageFile" type="file" accept="image/*">`; trang Edit hiển thị ảnh hiện tại.
- [ ] Thêm `VuaBanhMy/wwwroot/images/products/` vào `.gitignore` (ảnh upload là dữ liệu, không phải mã nguồn).

**Kiểm tra:**
- Upload ảnh `.jpg` → thấy ở `/Menu`, file nằm trong `wwwroot/images/products` với tên Guid.
- Upload file `.txt` đổi tên thành `.exe` hoặc ảnh > 2 MB → báo lỗi, không lưu.
- Sửa món mà không chọn ảnh → ảnh cũ vẫn giữ nguyên.

**Câu hỏi tự kiểm tra:** Chỉ kiểm tra đuôi file đã đủ an toàn chưa? Kẻ xấu có thể vượt qua thế nào?

**Commit:** `Add product image upload`

---

## Task 1.8: Tổng kết giai đoạn 1

- [ ] `dotnet test` — toàn bộ PASS.
- [ ] Chạy lại toàn bộ kịch bản demo:
  1. Khách chưa đăng nhập xem `/Menu`.
  2. Đăng ký tài khoản mới có họ tên → là `Customer`, không vào được `/Admin`.
  3. Admin thêm danh mục, thêm món kèm ảnh, ẩn/hiện món → menu cập nhật đúng.
- [ ] Viết `README.md` ở thư mục solution: giới thiệu dự án, công nghệ, cách chạy (`Update-Database`, tài khoản admin mẫu), danh sách tính năng đã có.
- [ ] Commit: `Complete phase 1: menu and admin catalog management`
- [ ] (Tùy chọn) Tạo repo GitHub và `git push`.
- [ ] Nhắn Claude để review tổng giai đoạn 1 và lập lộ trình **Giai đoạn 2: Giỏ hàng & Đặt hàng**.
