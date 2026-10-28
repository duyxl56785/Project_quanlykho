# Quản lý kho tổng

Phần mềm quản lý tập trung nhiều kho (nhập kho, xuất kho, kiểm kê, chuyển kho) bằng ASP.NET Core MVC (.NET 10), Entity Framework Core và PostgreSQL.

Chức năng chính: đơn mua, hóa đơn nhập, khu tiếp nhận và xếp hàng vào ô (vị trí 7 cấp, kiểm sức chứa); đơn đặt hàng, giữ hàng, lệnh xuất kho có quét mã và kiểm tra chéo; chuyển kho hai bước; kiểm kê và điều chỉnh tồn có duyệt; gợi ý lấy hàng (G1) và đề xuất nhập bổ sung (G3); cảnh báo, báo cáo tồn, thẻ kho, nhật ký thao tác; nhập Excel danh mục và tồn đầu kỳ; quy đổi đơn vị một tầng.

## Cài đặt

Cần: .NET SDK 10, Docker Desktop (chạy PostgreSQL 17), Git.

```bash
git clone https://github.com/duyxl56785/Project_quanlykho.git
cd Project_quanlykho
dotnet tool restore
docker compose up -d
```

`docker compose up -d` bật container `quanlykho-db` ở cổng 5432 (CSDL `quanlykho`, người dùng `quanlykho`). Nếu cổng 5432 đã bị dùng: copy `docker-compose.override.example.yml` thành `docker-compose.override.yml` (mở cổng 5434), rồi đặt chuỗi kết nối riêng cho máy bằng user-secrets:

```bash
dotnet user-secrets set "ConnectionStrings:QuanLyKho" "Host=localhost;Port=5434;Database=quanlykho;Username=quanlykho;Password=quanlykho_dev" --project quanlykho
```

## Chạy

Nạp dữ liệu thử (tự migrate, chạy lại không nhân đôi, xong thì thoát):

```bash
dotnet run --project quanlykho -- --seed
```

Chạy ứng dụng, mở http://localhost:5166 (ở Development app tự migrate khi khởi động):

```bash
dotnet run --project quanlykho
```

Kiểm tra kết nối CSDL: `GET /health/db` trả 200 khi kết nối được, 503 khi không.

Dữ liệu thử gồm 2 kho `K01` (Hà Nội), `K02` (Bắc Ninh), 50 biến thể hàng, 3 nhà cung cấp, mỗi kho một hóa đơn nhập mẫu đã xếp vào ô và 3 tháng lịch sử xuất bán (để G3 có đề xuất). Tài khoản thử (mật khẩu chung ở khóa `DuLieuThu:MatKhau` trong `quanlykho/appsettings.Development.json`):

| Email | Vai | Kho |
| --- | --- | --- |
| `admin@kho.test` | Quản trị viên | Tất cả |
| `ql.k01@kho.test` | Quản lý kho | K01 |
| `ql.k02@kho.test` | Quản lý kho | K02 |
| `tk1.k01@kho.test`, `tk2.k01@kho.test` | Thủ kho | K01 |
| `tk.k02@kho.test` | Thủ kho | K02 |
| `banhang@kho.test` | Nhân viên bán hàng | Tất cả (chỉ xem tồn khả dụng, đặt hàng) |

Muốn CSDL sạch (xóa hết dữ liệu): `docker compose down -v`, `docker compose up -d`, rồi seed lại.

## Test

```bash
dotnet test
```

Test tích hợp chạy trên PostgreSQL thật qua Testcontainers nên Docker phải đang chạy. CI (`.github/workflows/ci.yml`) build và test mỗi lần push `develop`, `main` và khi có pull request. Kết quả nghiệm thu theo Bảng 1.6 ở [docs/NGHIEM_THU.md](docs/NGHIEM_THU.md), đo thời gian tải trang ở [docs/DO_TAI_TRANG.md](docs/DO_TAI_TRANG.md).

## Tài liệu

- [docs/BaoCao_DoAn_QuanLyKho.md](docs/BaoCao_DoAn_QuanLyKho.md): đặc tả đầy đủ.
- [docs/KE_HOACH.md](docs/KE_HOACH.md): kế hoạch và quyết định kỹ thuật.
- [docs/NHIEM_VU.md](docs/NHIEM_VU.md): tiến độ từng nhiệm vụ.
- `/Styleguide` (chỉ ở Development): các thành phần giao diện dùng chung.

## Sao lưu và khôi phục

Script PowerShell trong `scripts/`, chạy trên máy có Docker đang chạy container `quanlykho-db`.

Sao lưu (tệp `backups/quanlykho-<yyyyMMdd-HHmm>.dump`, chỉ giữ 14 bản mới nhất):

```powershell
powershell -ExecutionPolicy Bypass -File scripts\sao-luu.ps1
```

Khôi phục từ một bản sao lưu. Tắt ứng dụng trước; script hỏi xác nhận (gõ `CO`) vì dữ liệu hiện tại bị ghi đè; lỗi giữa chừng thì CSDL giữ nguyên:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\khoi-phuc.ps1 backups\quanlykho-20261029-0200.dump
```

### Đặt lịch sao lưu hằng ngày (Windows Task Scheduler)

Chạy một lần trong PowerShell (đổi đường dẫn repo cho đúng máy), tạo tác vụ sao lưu lúc 02:00 mỗi ngày:

```powershell
$repo = "D:\monhoc\Do_an\quanlykho"
$lenh = New-ScheduledTaskAction -Execute "powershell.exe" -Argument "-NoProfile -ExecutionPolicy Bypass -File `"$repo\scripts\sao-luu.ps1`"" -WorkingDirectory $repo
Register-ScheduledTask -TaskName "quanlykho-sao-luu" -Action $lenh -Trigger (New-ScheduledTaskTrigger -Daily -At 2am)
```

Hoặc mở Task Scheduler → Create Basic Task → Daily, 02:00 → Start a program: `powershell.exe`, đối số `-NoProfile -ExecutionPolicy Bypass -File "<repo>\scripts\sao-luu.ps1"`. Máy phải bật và Docker phải chạy vào giờ đó; xem kết quả lần chạy gần nhất ở cột Last Run Result.
