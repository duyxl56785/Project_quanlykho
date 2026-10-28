# Quản lý kho tổng

Phần mềm quản lý tập trung nhiều kho (nhập kho, xuất kho, kiểm kê, chuyển kho) bằng ASP.NET Core MVC (.NET 10), Entity Framework Core và PostgreSQL.

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
