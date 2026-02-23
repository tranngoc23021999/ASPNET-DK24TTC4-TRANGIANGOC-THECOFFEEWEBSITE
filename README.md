# THE COFFEE WEBSITE ☕

## THÔNG TIN CÁ NHÂN
- **Họ và tên**: TRẦN GIA NGỌC
- **Mã lớp**: DK24TTC4
- **MSSV**: 170124396
- **Email**: tranngoc23021999@gmail.com
- **SĐT**: 0773961454
## CẤU TRÚC SOURCE
- Progress-report: báo cáo tiến độ
- SCR: Chứa source code
- Thesis: Báo cáo đồ án
- README.md: Thông tin, hướng dẫn setup đồ án
## GIỚI THIỆU
### Công nghệ sử dụng

| Công nghệ                     | Mục đích                                                                     |
| ----------------------------- | ---------------------------------------------------------------------------- |
| ASP.NET Core MVC (.NET 9.0)   | Framework web                                                                |
| Entity Framework Core 9.0     | ORM — truy vấn SQL Server                                                    |
| BCrypt.Net-Next               | Hash mật khẩu                                                                |
| Cookie Authentication         | Xác thực người dùng                                                          |
| Session                       | Lưu trạng thái phiên                                                         |
| masax-drawpdf                 | In hoá đơn PDF — đã bundle sẵn tại `wwwroot/dist/drawpdf.standalone.umd.cjs` |
 
## HƯỚNG DẪN SETSUP
### 1. Yêu cầu hệ thống

| Thành phần             | Phiên bản / Chi tiết                                                          |
| ---------------------- | ----------------------------------------------------------------------------- |
| Hệ điều hành           | **>= 10**                                                                     |
| **.NET SDK**           | **9.0** (bắt buộc — project target `net9.0`)                                  |
| **SQL Server**         | LocalDB / SQL Express / SQL Server 2019 trở lên                               |
| **SSMS**               | SQL Server Management Studio — dùng quản trị CSDL                             |
| **Visual Studio 2022** |                                                                               |

### 2. Lấy mã nguồn

```bash
git clone https://github.com/tranngoc23021999/ASPNET-DK24TTC4-TRANGIANGOC-THECOFFEEWEBSITE.git
```
**Mở bằng Visual Studio:**

- File → **Open** → chọn `CoffeSolution/CoffeSolution.sln`

### 3. Cấu hình chuỗi kết nối

Mở `CoffeSolution/CoffeSolution/appsettings.json` → chỉnh mục `ConnectionStrings:DefaultConnection` cho phù hợp môi trường.

**LocalDB**

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=CoffeeShopDb;Trusted_Connection=True;MultipleActiveResultSets=true"
  }
}
```
**SQL Express**

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.\\SQLEXPRESS;Database=CoffeeShopDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
  }
}
```

**SQL Server auth**

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=SERVERNAME;Database=CoffeeShopDb;User Id=sa;Password=your_password;TrustServerCertificate=True;MultipleActiveResultSets=true"
  }
}
```
### 5. Khởi tạo cơ sở dữ liệu

Project đã có sẵn thư mục `Migrations/` nên **không cần** tạo migration mới. Chỉ cần chạy `Update-Database`.

1. Mở **Tools → NuGet Package Manager → Package Manager Console (PMC)**
2. Chọn **Default Project** = `CoffeSolution` (project chứa `.csproj`)
3. Chạy:

```powershell
Update-Database -Context ApplicationDbContext
```
Hoặc có thể dùng CMD với:
```powershell
dotnet ef database update
```

> [!TIP]
> Tạo migration mới (ví dụ sau khi sửa model), dùng:
> ```powershell
> dotnet ef migrations add TenMigration --context ApplicationDbContext
> ```

### 6. Chạy ứng dụng
```bash
cd CoffeSolution/CoffeSolution
dotnet restore
dotnet build
dotnet run
```

> [!NOTE]
>  **Tài khoản admin mặc định:**
> | Trường   | Giá trị                      |
> | -------- | ---------------------------- |
> | Username | `trangiangoc`                |
> | Password | `trangiangoc@123`            |
> | Email    | `TranGiaNgoc@coffeeshop.com` |


Mở URL `https://localhost:7207` (Lưu ý link debug bằng https)
