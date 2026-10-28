# Đo thời gian tải trang (NFR04)

Yêu cầu: các trang chính tải dưới 3 giây.

## Cách đo

- Ngày 29/10/2026, bản Release (`dotnet build -c Release`, .NET 10.0.401), PostgreSQL 17 trong Docker trên cùng máy (Windows 11).
- CSDL dev sau `--seed` đầy đủ: 2 kho, 50 biến thể, 182 phiếu xuất (3 tháng xuất bán), 450 dòng sổ biến động, 18 dòng tồn.
- Đăng nhập quản trị viên (thấy mọi trang, mọi kho). Mỗi trang nạp trong iframe và đo từ lúc gán `src` tới sự kiện `load` (gồm HTML, CSS, JS), hai lần liên tiếp: lần đầu ngay sau khi khởi động máy chủ và lần thứ hai.
- Hai thẻ của Tổng quan là nội dung nạp sau (`data-tai`), đo riêng.

## Kết quả (mili giây)

| Trang | Lần đầu | Lần sau |
| --- | ---: | ---: |
| `/` Tổng quan (khung trang) | 43 | 46 |
| `/TongQuan/The/DuoiToiThieu` | 21 | 19 |
| `/TongQuan/The/SapHetHan` | 18 | 18 |
| `/DonMua` | 198 | 49 |
| `/PhieuNhap` | 119 | 47 |
| `/XepHang` | 89 | 51 |
| `/XepHang/ChuyenViTri` | 65 | 38 |
| `/DeXuatNhap` (G3) | 138 | 48 |
| `/TonKhaDung` | 76 | 50 |
| `/DonDat` | 90 | 52 |
| `/LenhXuat` | 74 | 41 |
| `/PhieuXuat` | 73 | 44 |
| `/PhieuXuat/PX000003` | 100 | 43 |
| `/DieuChuyen` | 74 | 47 |
| `/KiemKe` | 75 | 43 |
| `/BaoCao/TongHop` | 122 | 51 |
| `/BaoCao/TheoLo` | 68 | 51 |
| `/BaoCao/TheoViTri` | 61 | 44 |
| `/BaoCao/NhapXuatTon` (4 tháng) | 82 | 47 |
| `/BaoCao/TheKho` (sữa K01, 91 dòng) | 88 | 67 |
| `/DanhMuc/SanPham` | 92 | 43 |
| `/ViTri` | 124 | 43 |
| `/Kho` | 75 | 37 |
| `/NguoiDung` | 69 | 52 |
| `/NhatKy` | 77 | 48 |
| `/NhapExcel` | 52 | 41 |
| `/NhapExcel/TonDauKy` | 50 | 36 |

Mọi trang dưới 0,2 giây, đạt yêu cầu; không cần thêm chỉ mục hay phân trang.

## Giới hạn

- Dữ liệu thử nhỏ. Báo cáo tồn và G3 gom số liệu trong bộ nhớ sau một vài truy vấn (theo kho được giao), nên thời gian tăng theo số dòng tồn và số dòng sổ biến động trong kỳ. Danh sách chứng từ phân trang ngay trong CSDL. Khi dữ liệu thật lớn (hàng chục nghìn dòng sổ biến động mỗi kỳ) cần đo lại `/BaoCao/NhapXuatTon` và `/DeXuatNhap` trước tiên.
- Đo trên máy phát triển, mạng nội bộ; trên máy tính bảng qua Wi-Fi cần cộng thêm độ trễ mạng.
