# VuaBanhMy — Tài liệu thiết kế (Spec)

> Ngày lập: 2026-10-06 · Trạng thái: Đã duyệt
> Mục đích: Website đặt bánh mì online — dự án cá nhân cho CV thực tập .NET.
> Cách dùng tài liệu: mở lại mỗi khi bắt đầu một giai đoạn mới hoặc quên "tại sao lại làm thế này".

---

## 1. Tổng quan

Khách hàng vào website → xem menu → đăng nhập → thêm món vào giỏ → thanh toán →
nhà hàng nhận thông báo đơn mới → xác nhận và làm món → giao cho shipper của nhà hàng → shipper giao đến khách.

### Công nghệ

| Thành phần | Công nghệ |
|---|---|
| Web framework | ASP.NET Core MVC (.NET 10) |
| Đăng nhập / phân quyền | ASP.NET Core Identity + Roles |
| CSDL | SQL Server + Entity Framework Core (Code First, Migrations) |
| Realtime (giai đoạn 3) | SignalR |
| Thanh toán online (giai đoạn 5) | VNPay Sandbox |
| Kiểm thử | xUnit + EF Core InMemory |

### Kiến trúc: Controller → Service → DbContext

```
Trình duyệt ──► Controller ──► Service (interface) ──► ApplicationDbContext ──► SQL Server
                  │                 │
                  │                 └─ chứa toàn bộ logic nghiệp vụ (tính tiền, kiểm tra trạng thái...)
                  └─ chỉ nhận request, gọi service, trả View
```

- Một project web duy nhất (`VuaBanhMy`) + một project test (`VuaBanhMy.Tests`).
- Service được đăng ký qua Dependency Injection: `builder.Services.AddScoped<ICartService, CartService>();`
- **Lý do:** logic nằm một chỗ, dễ test, dễ giải thích khi phỏng vấn. Clean Architecture nhiều project bị loại vì quá nặng cho giai đoạn học.

---

## 2. Vai trò người dùng (Roles)

| Role | Ai | Được làm gì | Tạo tài khoản bằng cách |
|---|---|---|---|
| `Admin` | Chủ quán | Quản lý danh mục, món, tài khoản Staff/Shipper | Seed sẵn trong `Program.cs` |
| `Staff` | Nhân viên quầy/bếp | Xem đơn, xác nhận, cập nhật trạng thái làm món, gán shipper, hủy đơn (có lý do) | Admin tạo |
| `Shipper` | Shipper của nhà hàng | Xem đơn được gán cho mình, cập nhật đang giao / đã giao | Admin tạo |
| `Customer` | Khách hàng | Giỏ hàng, đặt đơn, xem lịch sử, hủy đơn khi còn "Chờ xác nhận" | Tự đăng ký (tự gán role) |

Khách **không cần đăng nhập để xem menu**; bấm "Thêm vào giỏ" mới yêu cầu đăng nhập.

---

## 3. Cơ sở dữ liệu

### 3.1 Sơ đồ quan hệ

```
Category  1 ── n  Product
User      1 ── n  CartItem  n ── 1  Product
User      1 ── n  Order          (vai trò khách: CustomerId)
User      1 ── n  Order          (vai trò shipper: ShipperId, nullable)
Order     1 ── n  OrderItem n ── 1  Product
```

### 3.2 Các bảng

**ApplicationUser** (kế thừa `IdentityUser` — bảng `AspNetUsers`)

| Cột | Kiểu | Ghi chú |
|---|---|---|
| (kế thừa) | | `Id`, `UserName`, `Email`, `PhoneNumber`, `PasswordHash`... |
| FullName | string (100) | |
| Address | string (255), null | Địa chỉ mặc định, dùng để điền sẵn form thanh toán |

**Category**

| Cột | Kiểu | Ghi chú |
|---|---|---|
| Id | int, PK | |
| Name | string (100), bắt buộc | VD: Bánh mì, Đồ uống, Món thêm |

**Product**

| Cột | Kiểu | Ghi chú |
|---|---|---|
| Id | int, PK | |
| Name | string (150), bắt buộc | |
| Description | string (500), null | |
| Price | decimal(18,2) | Phải > 0 |
| ImageUrl | string, null | Đường dẫn ảnh trong `wwwroot/images/products` |
| IsAvailable | bool | **Soft delete**: ẩn món thay vì xóa |
| CategoryId | int, FK → Category | |

**CartItem**

| Cột | Kiểu | Ghi chú |
|---|---|---|
| Id | int, PK | |
| UserId | string, FK → AspNetUsers | |
| ProductId | int, FK → Product | |
| Quantity | int | ≥ 1 |
| Note | string (200), null | VD: "không hành, thêm ớt" |

Ràng buộc: **unique (UserId, ProductId)** — thêm lại cùng món thì tăng số lượng.

**Order**

| Cột | Kiểu | Ghi chú |
|---|---|---|
| Id | int, PK | |
| CustomerId | string, FK → AspNetUsers | |
| ReceiverName | string (100) | **Snapshot** lúc đặt |
| ReceiverPhone | string (20) | **Snapshot** |
| DeliveryAddress | string (255) | **Snapshot** |
| Note | string (500), null | Ghi chú cho cả đơn |
| TotalAmount | decimal(18,2) | Tính ở server |
| Status | `OrderStatus` (lưu int) | |
| PaymentMethod | `PaymentMethod` (lưu int) | `Cod`, `VnPay` |
| PaymentStatus | `PaymentStatus` (lưu int) | `Unpaid`, `Paid`, `Failed` |
| ShipperId | string, null, FK → AspNetUsers | Staff gán khi đơn `ReadyForDelivery` |
| CancelReason | string (255), null | Bắt buộc khi Staff hủy |
| CreatedAt | DateTime (UTC) | |
| UpdatedAt | DateTime (UTC) | |

**OrderItem**

| Cột | Kiểu | Ghi chú |
|---|---|---|
| Id | int, PK | |
| OrderId | int, FK → Order | |
| ProductId | int, FK → Product | |
| ProductName | string (150) | **Snapshot** tên món |
| UnitPrice | decimal(18,2) | **Snapshot** giá lúc đặt |
| Quantity | int | |
| Note | string (200), null | Copy từ CartItem |

### 3.3 Hai nguyên tắc quan trọng

1. **Snapshot dữ liệu** — Đơn hàng lưu bản sao tên món, giá, thông tin người nhận tại thời điểm đặt.
   Admin đổi giá hoặc khách đổi địa chỉ sau đó thì đơn cũ vẫn đúng.
2. **Soft delete món ăn** — Không xóa `Product` (vì `OrderItem` đang tham chiếu tới), chỉ đặt `IsAvailable = false`.

---

## 4. Vòng đời đơn hàng

### 4.1 Trạng thái (`enum OrderStatus`)

| Giá trị | Ý nghĩa |
|---|---|
| `Pending` | Chờ nhà hàng xác nhận |
| `Confirmed` | Nhà hàng đã nhận đơn |
| `Preparing` | Đang làm món |
| `ReadyForDelivery` | Làm xong, chờ giao |
| `Delivering` | Shipper đang giao |
| `Delivered` | Đã giao (kết thúc) |
| `Cancelled` | Đã hủy (kết thúc) |

### 4.2 Bảng chuyển trạng thái hợp lệ

| Từ | Sang | Ai thực hiện | Điều kiện |
|---|---|---|---|
| `Pending` | `Confirmed` | Staff | |
| `Pending` | `Cancelled` | Customer (chủ đơn) | Không cần lý do |
| `Pending` | `Cancelled` | Staff | Bắt buộc lý do |
| `Confirmed` | `Preparing` | Staff | |
| `Confirmed` | `Cancelled` | Staff | Bắt buộc lý do |
| `Preparing` | `ReadyForDelivery` | Staff | |
| `Preparing` | `Cancelled` | Staff | Bắt buộc lý do |
| `ReadyForDelivery` | *(gán shipper, giữ nguyên trạng thái)* | Staff | User được chọn phải có role `Shipper` |
| `ReadyForDelivery` | `Delivering` | Shipper | Phải là shipper được gán |
| `ReadyForDelivery` | `Cancelled` | Staff | Bắt buộc lý do |
| `Delivering` | `Delivered` | Shipper | Phải là shipper được gán |

Mọi chuyển đổi **khác** đều bị từ chối. Toàn bộ quy tắc nằm ở **một chỗ duy nhất** trong `OrderService`.
Ngoài phạm vi: giao thất bại / hoàn đơn khi đang giao.

```
Pending ──► Confirmed ──► Preparing ──► ReadyForDelivery ──► Delivering ──► Delivered
   │            │             │                 │
   └────────────┴─────────────┴─────────────────┴──► Cancelled
```

---

## 5. Tầng Service

Mọi service trả về `ServiceResult` (hoặc `ServiceResult<T>`) cho các thao tác có thể thất bại vì nghiệp vụ:

```csharp
public class ServiceResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
}
```

### ICategoryService (giai đoạn 1)
- Admin: thêm / sửa / xóa danh mục (không cho xóa danh mục đang có món).

### IProductService (giai đoạn 1)
- Lấy menu (chỉ món `IsAvailable`, nhóm theo danh mục), lấy chi tiết món.
- Admin: thêm / sửa / ẩn-hiện món.

### IImageService (giai đoạn 1)
- Lưu ảnh món upload vào `wwwroot/images/products` (kiểm tra đuôi file, dung lượng), trả về đường dẫn.

### ICartService (giai đoạn 2)
- `GetCartAsync(userId)`, `AddItemAsync(userId, productId, quantity, note)`,
  `UpdateQuantityAsync`, `RemoveItemAsync`, `ClearAsync`.
- Từ chối món không tồn tại / đã ẩn, số lượng ≤ 0.

### IOrderService (giai đoạn 2–4)
- `PlaceOrderAsync(userId, checkoutInfo)`:
  1. Đọc giỏ; từ chối nếu rỗng hoặc có món đã ẩn.
  2. Tạo `Order` + `OrderItem` (snapshot tên, giá, người nhận); **giá lấy từ DB, không lấy từ form**.
  3. Tính `TotalAmount` ở server; `Status = Pending`; `PaymentStatus = Unpaid`.
  4. Xóa giỏ.
  5. **Một lần `SaveChangesAsync()`** → thành công hết hoặc không lưu gì.
- Chuyển trạng thái: `CancelByCustomerAsync`, `ConfirmAsync`, `StartPreparingAsync`, `MarkReadyAsync`,
  `AssignShipperAsync`, `StartDeliveryAsync`, `MarkDeliveredAsync`, `CancelByStaffAsync(reason)` — tất cả đi qua bảng quy tắc mục 4.2.
- Kiểm tra quyền sở hữu: khách chỉ thao tác đơn của mình; shipper chỉ đơn được gán.

### IOrderNotifier (giai đoạn 3)
- `OrderPlacedAsync(order)`, `OrderStatusChangedAsync(order)`.
- Cài đặt bằng SignalR: Staff nhận đơn mới realtime; khách thấy trạng thái đơn đổi realtime.
- `OrderService` chỉ biết interface, không phụ thuộc SignalR.

### IPaymentService (giai đoạn 5)
- Tạo URL thanh toán VNPay, xác thực chữ ký callback/IPN, cập nhật `PaymentStatus`.
- Đơn VNPay chỉ được báo cho nhà hàng sau khi `PaymentStatus = Paid`.

---

## 6. Controller & Area

| Khu vực | Controller | Quyền |
|---|---|---|
| (chung) | `MenuController` | Ai cũng xem |
| (chung) | `CartController` | `Customer` |
| (chung) | `OrdersController` (checkout, lịch sử, chi tiết, hủy) | `Customer` |
| `Areas/Admin` | `CategoriesController`, `ProductsController`, `UsersController` | `Admin` |
| `Areas/Staff` | `OrdersController` | `Staff` |
| `Areas/Shipper` | `DeliveriesController` | `Shipper` |

Phân quyền bằng `[Authorize(Roles = "...")]` đặt ở cấp controller.

---

## 7. Xử lý lỗi & bảo mật

- Lỗi nghiệp vụ → `ServiceResult` thất bại → controller đưa thông báo vào `TempData` → View hiển thị.
- Lỗi validation form → Data Annotations trên ViewModel + `ModelState.IsValid`.
- Giá tiền, tổng tiền **luôn tính ở server**.
- Mọi form POST có `[ValidateAntiForgeryToken]`.
- Không dùng entity trực tiếp làm model cho form (dùng ViewModel) để tránh over-posting.
- Sửa `RequireConfirmedAccount = false` (chưa có gửi email, nếu để `true` khách đăng ký xong không đăng nhập được).

---

## 8. Kiểm thử

Project `VuaBanhMy.Tests` (xUnit + EF Core InMemory). Ưu tiên test `OrderService` và `CartService`:

- Đặt đơn với giỏ rỗng → thất bại.
- Đặt đơn thành công → tổng tiền đúng, giỏ bị xóa, OrderItem có snapshot giá.
- Đổi giá món sau khi đặt → đơn cũ không đổi.
- Chuyển trạng thái không hợp lệ (VD `Pending` → `Delivered`) → bị chặn.
- Khách hủy đơn của người khác → bị chặn; khách hủy đơn `Confirmed` → bị chặn.
- Shipper cập nhật đơn không được gán → bị chặn.

---

## 9. Các giai đoạn (Phases)

| # | Giai đoạn | Nội dung chính | Kiến thức học được |
|---|---|---|---|
| 0 | Chuẩn bị | Git, `ApplicationUser`, sửa cấu hình Identity, thêm role Staff/Shipper, tự gán role Customer khi đăng ký | Git, mở rộng Identity, Migrations |
| 1 | Thực đơn | Category, Product, trang menu, Admin CRUD, upload ảnh | EF Core, quan hệ 1-n, Area, Authorize, ViewModel, DI service |
| 2 | Giỏ hàng & Đặt hàng (COD) | CartService, checkout, lịch sử đơn, khách hủy đơn | Nghiệp vụ, snapshot, transaction, unit test |
| 3 | Phía nhà hàng | Area Staff, chuyển trạng thái, thông báo realtime | State machine, SignalR |
| 4 | Giao hàng | Role Shipper, gán shipper, theo dõi đơn | Phân quyền theo dữ liệu (ownership) |
| 5 | Thanh toán online | VNPay sandbox | Tích hợp API bên thứ ba, chữ ký HMAC, IPN |

Mở rộng sau (không thuộc phạm vi hiện tại): topping tính tiền, lịch sử trạng thái đơn, đánh giá món, giao thất bại / hoàn tiền.

---

## 10. Quy ước code

- Tên class / cột / biến: tiếng Anh. Text hiển thị cho người dùng: tiếng Việt.
- Mỗi entity một file trong `Models/`; ViewModel trong `Models/ViewModels/`; service trong `Services/` (interface + class cạnh nhau).
- Dùng `async/await` cho mọi truy vấn DB.
- Commit git sau mỗi bước nhỏ chạy được, message rõ ràng (VD: `Add Product entity and migration`).
