# CLAUDE.md

Hướng dẫn cho Claude Code khi làm việc trong repo này.

## Dự án

Phần mềm quản lý kho tổng: một hệ thống quản lý tập trung nhiều kho (K ≥ 2, dữ liệu thử dùng 2 kho) của cùng một doanh nghiệp. Chỉ làm quản lý kho: nhập kho, xuất kho, kiểm kê, chuyển kho. Không làm bán hàng, thanh toán, website.

- Stack: ASP.NET Core MVC (.NET 10), Entity Framework Core, PostgreSQL (chạy bằng docker compose).
- Một người làm toàn bộ dự án. Phần phân vai nhóm, giờ làm và người phụ trách trong báo cáo chỉ để tham khảo, không áp dụng.
- Tiến độ và quy trình làm từng nhiệm vụ: [docs/NHIEM_VU.md](docs/NHIEM_VU.md). Làm xong nhiệm vụ nào thì tích ở đó, kèm mã commit gộp.
- Kế hoạch chi tiết đã duyệt cho mọi nhiệm vụ và các quyết định kỹ thuật chung (A1 đến A15): [docs/KE_HOACH.md](docs/KE_HOACH.md). Làm đúng theo đó; cần lệch thì sửa file đó và hỏi trước.
- Đặc tả đầy đủ: [docs/BaoCao_DoAn_QuanLyKho.md](docs/BaoCao_DoAn_QuanLyKho.md). Hình (use case, sơ đồ hoạt động, ERD) ở `docs/images/`, `hinh-NN.png` ứng với Hình 2.NN. Khi chi tiết trong file này chưa đủ, đọc mục tương ứng của báo cáo.

## Lệnh

```bash
dotnet build quanlykho.slnx
dotnet run --project quanlykho
```

```bash
docker compose up -d          # bật PostgreSQL (container quanlykho-db)
docker compose down           # tắt, giữ dữ liệu; thêm -v là xóa dữ liệu
dotnet tool restore           # lần đầu: cài dotnet-ef
dotnet run --project quanlykho -- --seed   # migrate rồi nạp dữ liệu thử (chạy lại không nhân đôi), xong thì thoát
```

Dữ liệu thử ở `Data/Seed/DuLieuThu.cs`: kho `K01`, `K02` (19 ô mỗi kho, có ô mặc định khu tiếp nhận và cách ly), 4 nhóm hàng, 50 biến thể, 3 nhà cung cấp, 2 nơi nhận, 7 tài khoản `ND001`–`ND007` email `@kho.test` (mật khẩu ở `DuLieuThu:MatKhau` trong `appsettings.Development.json`). `--seed` còn thêm mỗi kho một hóa đơn nhập mẫu đã xác nhận và đã xếp vào ô lưu trữ (`Data/Seed/PhieuNhapMau.cs`, đi qua service, không có trong DB test). Muốn DB sạch: `docker compose down -v`, `docker compose up -d`, rồi seed lại.

```bash
dotnet test                   # chạy toàn bộ test (quanlykho.Tests); test tích hợp cần Docker đang chạy
dotnet ef migrations add <Ten> --project quanlykho --output-dir Data/Migrations   # thêm migration
dotnet ef database update --project quanlykho                                       # áp migration vào DB dev
```

Khi app đang chạy bằng `dotnet run`, thư mục `bin/Debug` bị khóa: thêm `--configuration Release` cho lệnh `dotnet ef`. Ở Development app tự chạy migration lúc khởi động (`Csdl:TuDongMigrate` trong `appsettings.Development.json`).

Kiểm tra kết nối CSDL: `GET /health/db` (200 khi kết nối được, 503 khi không).

Test nằm ở `quanlykho.Tests/` (xUnit). Test dựng app dùng `HaTang/UngDungFactory` (tắt tự migrate): mặc định đăng nhập giả theo vai qua `factory.TaoClient(Vai.QuanTri)` (không cần CSDL); test cần CSDL và cookie thật tạo `new UngDungFactory { ChuoiKetNoi = ..., DangNhapGia = false, Gio = new FakeTimeProvider(...) }` (mẫu: `Integration/PhanQuyenTests`). Test cần CSDL đặt trong `Integration/`, gắn `[Collection(CsdlCollection.Ten)]` và gọi `CsdlFixture.TaoCsdlAsync()` để có database riêng đã migrate trên container PostgreSQL của Testcontainers. CI ở `.github/workflows/ci.yml` chạy build và test khi push `develop`, `main` và khi có pull request.

Trên máy này PostgreSQL chạy ở cổng 5434 (5432 và 5433 bị dự án khác dùng) qua `docker-compose.override.yml` (copy từ `docker-compose.override.example.yml`, đã nằm trong `.gitignore`). Chuỗi kết nối cổng 5434 đặt bằng `dotnet user-secrets` (khóa `ConnectionStrings:QuanLyKho`); `appsettings.Development.json` giữ cổng 5432 mặc định.

## Kiến trúc

- Thư mục (KE_HOACH.md A1): `Domain/` entity và enum; `Data/` DbContext, cấu hình bảng, migration, dữ liệu thử; `Common/` phần dùng chung (lỗi, cổng ghi tồn, phân quyền, nhật ký); `Features/<Mang>/` controller trang, controller API, service, ViewModel của từng mảng; view ở `Views/<Mang>/`.
- Controller, Service theo mảng; không có tầng Repository riêng, `AppDbContext` đóng vai trò đó. Luật nghiệp vụ chỉ nằm trong Service.
- Lỗi: Service ném `NghiepVuException` (422), `XungDotException` (409), `KhongCoQuyenException` (403) trong `Common/Loi`. `XuLyLoiFilter` đổi thành JSON `{ loi, chiTiet }` cho controller `[ApiController]`, hoặc hiện lại view của action với lỗi trong ModelState cho trang. Không bắt các lỗi này trong controller.
- Đăng nhập, phân quyền (`Common/PhanQuyen`): mặc định mọi trang cần đăng nhập, trang công khai đánh `[AllowAnonymous]`. Giới hạn vai bằng `[Authorize(Roles = Vai.QuanTri)]` (hằng trong `Vai`: `QuanTri`, `QuanLyKho`, `ThuKho`, `BanHang`, `QuanLy`, `TrongKho`). Service nhận mã kho phải gọi `NguoiDungHienTai.DamBaoQuyenKhoAsync(maKho)`; danh sách lọc theo `KhoDuocGiaoAsync()`. Đổi vai, kho, mật khẩu hoặc khóa tài khoản phải đổi `SecurityStamp` (phiên cũ hết hiệu lực ở request kế tiếp).
- Nhật ký: gọi `GhiNhatKy.Ghi(...)` trước `SaveChangesAsync` của thao tác để lưu cùng transaction.
- Ghi tồn: Service mở `db.Database.BeginTransactionAsync()`, đổi trạng thái chứng từ, gọi `CongGhiTon.GhiAsync(khoaChongLap, loaiChungTu, maChungTu, dong)` (dòng `DongGhiTon(MaLo, MaViTri, SoLuongThayDoi)`, chỉ ô cấp 7), rồi commit. Trả `KetQuaGhi.DaGhiTruocDo` khi khóa đã ghi thì không làm gì thêm. Giữ hàng: `GiuAsync`, `BoGiuAsync`. Không tự sửa `TonKho` ở chỗ khác.
- Văn hóa request cố định là InvariantCulture (số `1.5`, ngày `yyyy-MM-dd` khi gửi lên); hiển thị kiểu Việt Nam do helper định dạng làm. Giờ hiện tại lấy qua `TimeProvider` (đã đăng ký), không gọi `DateTime.Now`.
- API REST trả JSON dưới `/api/...`, đường dẫn tiếng Việt không dấu dạng kebab: `/api/don-mua`, `/api/phieu-nhap/{ma}/xac-nhan`, `/api/vi-tri/o-phu-hop`, `/api/xep-hang`.
- Bảng và cột đặt tên tiếng Việt không dấu PascalCase như trong báo cáo: `ViTri`, `BienThe`, `Lo`, `TonKho`, `SoBienDong`, `DonMua`, `PhieuNhap`, `DonViQuyDoi`, `MaViTri`, `SoLuongDaGiu`...
- Thay đổi CSDL bằng migration của EF Core. Cấu hình bảng bằng Fluent API trong `Data/Configurations/` (một lớp `IEntityTypeConfiguration` mỗi bảng, gom theo mảng); kiểu cột dùng helper trong `CauHinhChung` (`KichThuoc`, `KhoiLuong`, `SoLuong`, `Tien`, `ChiNhan<TEnum>`, `Check`). Enum mới phải thêm vào `ConfigureConventions` của `AppDbContext`. Mọi khóa ngoại là RESTRICT.
- Mã chứng từ sinh bằng `MaChungTu.TaoAsync("PN")` (sequence PostgreSQL theo tiền tố).

## Quy tắc nghiệp vụ bắt buộc (Bảng 1.5)

Không được vi phạm các quy tắc này, kể cả để code ngắn hơn.

1. **Cổng ghi tồn duy nhất.** Mọi thay đổi tồn chỉ đi qua một service ghi tồn. Cổng này cập nhật `TonKho` và ghi `SoBienDong` trong cùng một transaction. Không nơi nào khác được sửa `TonKho`.
2. **Chống ghi lặp.** Mỗi lần ghi tồn có một `MaKhoaChongLap` duy nhất, API nhận ở header `Idempotency-Key`. Bấm lặp không ghi hai lần.
3. **Không tồn âm.** `SoLuongDaGiu` không lớn hơn `SoLuong`. Ghi đồng thời phải khóa dòng, không dựa vào kiểm tra trước rồi mới ghi.
4. **Chứng từ đã Hoàn tất không sửa.** Hủy bằng bút toán đảo trong sổ biến động, không xóa.
5. **Không xóa cứng** dữ liệu đã có biến động tồn hoặc chứng từ (sản phẩm, đơn vị, đối tác, vị trí, tài khoản). Chỉ ngừng sử dụng hoặc khóa.
6. **Tồn theo kho, lô, vị trí.** Tổng biến động trên thẻ kho luôn bằng tồn của từng kho. Chuyển kho hai bước (kho đi trừ tồn, trạng thái Chờ nhập kho; kho đến nhận rồi xếp), tổng tồn toàn hệ thống trước và sau bằng nhau.
7. **Tồn khả dụng** loại trừ: hàng ở khu tiếp nhận, khu cách ly, lô hết hạn, hàng đang vận chuyển, hàng đã giữ.
8. **Quyền theo vai và theo kho, kiểm tra ở máy chủ**, không chỉ ẩn nút. Bốn vai: quản trị viên, quản lý kho, thủ kho, nhân viên bán hàng. Quản lý kho và thủ kho được gán một hoặc nhiều kho; người dùng chỉ thấy và thao tác trên kho được giao. Nhân viên bán hàng chỉ xem tồn khả dụng, tạo đơn đặt hàng, xem trạng thái đơn. Tài khoản bị khóa mất quyền ngay; luôn còn ít nhất một quản trị viên đang hoạt động.
9. **Hạn dùng:** hàng có theo dõi hạn dùng thì bắt buộc nhập hạn dùng, xuất theo hết hạn trước (FEFO); lô đã hết hạn không xuất được.
10. **Kiểm kê:** vị trí đang kiểm bị khóa (không nhập, xuất, chuyển, xếp vào). Mọi chênh lệch phải được phân loại (thiếu, thừa, hư hỏng, hết hạn, sai vị trí, sai biến thể), có lý do và người duyệt, rồi sinh chứng từ điều chỉnh. Không sửa thẳng số tồn.
11. **Người kiểm tra hàng đã lấy khác người lấy.**
12. **Danh mục và thuộc tính cấu hình trên giao diện.** Thêm ngành hàng, thuộc tính, kho mới không phải sửa mã nguồn. Biến thể sinh từ tổ hợp thuộc tính.
13. **AI chỉ gợi ý** (G1, G3): không tự ghi tồn, tự tạo hay tự duyệt phiếu. Gợi ý lỗi thì người dùng vẫn thao tác tay được. Lưu lịch sử gợi ý để thống kê tỷ lệ chấp nhận.
14. **Nhật ký thao tác chỉ thêm**, không sửa, không xóa. Ghi duyệt, hủy, hoàn thành, điều chỉnh tồn, phân quyền, khóa tài khoản.
15. **Đơn vị quy đổi một tầng**, hệ số nguyên > 1 (ví dụ 1 thùng = 24 chai). `TonKho`, `SoBienDong`, giữ hàng, G1, G3 chỉ tính theo đơn vị tính chính. Dòng chứng từ lưu `TenDonVi`, `HeSo`, `SoLuongGoc` tại thời điểm lập; quy về đơn vị chính trước khi gọi cổng ghi tồn. Hiển thị kèm cách quy ra đơn vị lớn nhất (235 chai = 9 thùng 19 chai).

## Dữ liệu (Bảng 2.32, 2.34; ERD Hình 2.11)

- Mã danh mục, chứng từ: `varchar(20)`. Mã vị trí: `varchar(40)`.
- Kích thước `numeric(8,3)` (mét), khối lượng `numeric(10,3)` (kg), số lượng `numeric(14,3)`, tiền `numeric(18,2)`. Trong C# dùng `decimal`, không dùng `double`.
- Ngày giờ `timestamptz`, hạn dùng `date`.
- `ThanhTien = SoLuongGoc × DonGia` là cột tính lưu sẵn; `TongTien` tính khi truy vấn. Không sửa tay.
- `TonKho` là bảng tổng hợp có chủ đích, khóa `(MaLo, MaViTri)`.
- Vị trí dạng cây 7 cấp, mã ví dụ `K01-TA02-TG03-P04-H05-KE06-O07` (kho, tòa, tầng, phòng – dãy, hàng, kệ, ô). Hàng chỉ nằm ở cấp ô. Loại khu: tiếp nhận, lưu trữ, cách ly, xuất. Ô có kích thước trong và tải trọng tối đa.
- Kiểm tra sức chứa khi xếp hàng theo Bảng 2.33 (vừa kích thước có xoay, xếp lưới 6 cách xoay cho ô một biến thể, hệ số lấp đầy 0,85 cho ô nhiều biến thể, tải trọng, khóa dòng ô trong transaction).

## Trạng thái chứng từ (Bảng 2.14)

| Chứng từ | Trạng thái |
| --- | --- |
| Đơn mua (đóng vai trò lệnh nhập) | Nháp → Chưa thực hiện → Đang thực hiện (Chờ nhận hàng, Chờ kiểm đếm) → Hoàn thành |
| Đơn đặt hàng | Nháp → Chờ duyệt → Đã duyệt (đã giữ hàng) → Đang xuất → Hoàn thành; Hủy trước khi xuất |
| Lệnh xuất kho | Chưa thực hiện → Đang thực hiện (Chờ lấy hàng, Chờ kiểm tra, Chờ xác nhận) → Hoàn thành |
| Phiếu nhập, phiếu xuất (ghi sổ) | Lưu tạm → Hoàn tất → Đã hủy; chỉ Hoàn tất mới ghi tồn |
| Yêu cầu kiểm kê, yêu cầu điều chuyển | Chưa thực hiện → Đang thực hiện → Hoàn thành |
| Phiếu kiểm kê | Chưa thực hiện → Đang kiểm kê → Hoàn thành |
| Phiếu điều chuyển | Lưu tạm → Chờ nhập kho → Nhận một phần → Hoàn thành; Hủy khi chưa nhận |

Trạng thái do hệ thống tính, không đánh dấu tay.

## Quy ước API (mục 2.16.4)

- Mọi API kiểm tra quyền theo vai và theo kho ở máy chủ.
- Mã HTTP: 401 chưa đăng nhập hoặc tài khoản bị khóa; 403 không có quyền theo vai hoặc kho; 422 lỗi nghiệp vụ kèm lý do (lỗi xếp hàng kèm số tối đa còn xếp được; thiếu hàng dạng "Cần X, còn Y"); 409 xung đột ghi đồng thời, cần thử lại.
- API không trả đơn giá, thành tiền cho thủ kho; chỉ quản lý kho và quản trị viên xem giá. Service đặt giá là null khi người xem không được thấy; API mới trả `JsonResult` với `WhenWritingNull` để bỏ hẳn trường giá (xem `PhieuNhapApiController`).
- Enum trong JSON là chuỗi tên giá trị (`"Dat"`, `"MuaHang"`), cả chiều gửi lên (`JsonStringEnumConverter` trong `Program.cs`).

## Gợi ý AI (mục 1.2.3)

- **G1, lấy hàng:** tổng lấy bằng q, không vượt tồn khả dụng từng (lô, vị trí); chỉ lô đạt chất lượng, chưa hết hạn, ở khu lưu trữ, không bị khóa kiểm kê. Ưu tiên FEFO (có hạn dùng) hoặc FIFO, rồi ít kệ phải ghé, sắp thứ tự ghé theo mã vị trí. Trả (lô, vị trí, số lượng, thứ tự) kèm lý do.
- **G3, nhập bổ sung (theo từng kho):** chỉ đề xuất khi d > 0 và có ít nhất 14 ngày lịch sử xuất. Đề xuất khi `A ≤ d × L + m`, số lượng `ceil(d × (L + 30) − A − số đang chờ nhập)`, số nguyên dương. A: tồn khả dụng, d: nhu cầu trung bình/ngày từ phiếu xuất hoàn tất, L: thời gian chờ hàng, m: tồn tối thiểu. Hằng số 30 cấu hình được (`G3:SoNgayDuHang`, mặc định 30). Màn hình `/DeXuatNhap`, API `/api/de-xuat-nhap`.

## Giao diện

- Tiếng Việt, ít bước, dùng được trên máy tính bảng; trang tải dưới 3 giây.
- Dựa trên Bootstrap có sẵn. Một file CSS dùng chung (biến màu, cỡ chữ, khoảng cách), một `_Layout`; không CSS riêng từng trang, không style inline.
- Thành phần dùng lại bằng partial view hoặc View Component: bảng dữ liệu có lọc và phân trang, ô quét mã (tự nhận con trỏ), huy hiệu trạng thái (mỗi trạng thái một màu cố định), hộp xác nhận, thông báo, ô chọn kho, ô chọn đơn vị. Có trang `/Styleguide` hiện mọi thành phần.
- Thành phần đã có (xem `/Styleguide`, chỉ mở ở Development):
  - Bảng: `<bang-du-lieu trang="Model" thong-bao-rong="...">` với model `TrangDuLieu<T>` (`TrangDuLieu<T>.TaoAsync(query, trang)`); thanh lọc là `<form class="thanh-loc" method="get">`.
  - Partial: `_HuyHieuTrangThai` (model enum; tên, màu khai ở `Common/GiaoDien/TrangThaiHienThi`, enum trạng thái mới phải thêm vào đó), `_OQuetMa` (model `OQuetMa`; JS nghe sự kiện `quet-ma`, báo bằng `QuanLyKho.baoQuet`), `_TrangRong`, `_DangTai`, `_ThongBao` (TempData `ThongBao`, `Loi`), `_HopXacNhan` (thuộc tính `data-xac-nhan` trên form, nút, liên kết).
  - View Component `OChonKho`: `@await Component.InvokeAsync("OChonKho", new { ten = "MaKho", chon = ... })`.
  - Phiếu có dòng hàng: `<tbody data-dong-hang>` + `<template>` với tên `Dong[__i__].X`, nút `data-them-dong`, `data-xoa-dong` (site.js đánh số lại).
  - Gọi API từ JS: `QuanLyKho.goiApi(url, { method, body, khoaChongLap })` tự gửi `Idempotency-Key` và token chống CSRF, trả `{ ok, status, duLieu, loi }`.
  - Menu bên trái khai ở `Common/GiaoDien/Menu.cs`; làm xong màn hình nào thì điền `Url` cho mục đó.
- Form cần dữ liệu phụ (danh sách để chọn) đặt dữ liệu đó trong model (ví dụ `DanhSachNhom`, `DanhSachKho`) và nạp trước khi gọi Service, để `XuLyLoiFilter` hiện lại view khi lỗi nghiệp vụ. View phải trùng tên action (`Them`, `Sua`), phần chung đặt ở partial `_Form`.
- Mọi POST, PUT, DELETE (cả API) kiểm token chống CSRF (`AutoValidateAntiforgeryToken`); form Razor có sẵn token, fetch dùng `goiApi`.
- Ngày `dd/MM/yyyy`; số và tiền theo kiểu Việt Nam. Thuật ngữ thống nhất: Hóa đơn nhập, Lô, Vị trí, Ô, Khu tiếp nhận.
- Màn hình nào cũng có trạng thái rỗng, lỗi và đang tải.

## Phạm vi (mục 1.2.5, 2.15)

Thứ tự làm: hóa đơn nhập (đơn mua → phiếu nhập → khu tiếp nhận) → xếp hàng vào vị trí → đơn đặt hàng, giữ hàng, xuất kho + G1 → chuyển kho, kiểm kê → G3, cảnh báo, báo cáo tồn, nhật ký, nhập Excel danh mục, chuyển vị trí trong kho → quy đổi đơn vị trên các chứng từ, nhập tồn đầu kỳ từ Excel.

- **Must:** FR01–FR12 (nhập kho, giữ hàng, quét mã khi lấy, kiểm tra chéo, tồn khả dụng, đơn đặt hàng, chuyển kho, kiểm kê, tài khoản, danh mục và biến thể, vị trí 7 cấp, kiểm tra sức chứa).
- **Should:** FR13 cảnh báo, FR14 báo cáo tồn, FR16 chuyển vị trí trong kho, FR17 nhập Excel, FR18 nhật ký, FR20 G1, FR21 G3, FR28 quy đổi đơn vị.
- **Could:** FR19 bàn làm việc; xuất Excel báo cáo tồn; sao lưu hằng ngày.
- **Không làm:** gợi ý xếp vị trí (G2), luân chuyển giữa kho (G4), nhập hàng trả lại, sơ đồ mặt bằng kho, phân loại ABC, lắp ráp, tháo dỡ, combo, quét camera, serial, quy đổi nhiều tầng, kết nối kế toán hoặc CRM, dự báo bằng học máy.

## Git

- `main`: bản ổn định, nhánh mặc định. `develop`: tích hợp. Làm mỗi việc trên một nhánh `feature/...` tách từ `develop`, gộp lại vào `develop`.
- Không push thẳng lên `main`, không force push.
- Commit message tiếng Việt, ngắn, dạng "Mảng: việc đã làm" (ví dụ "Thiết lập: docs/ và .gitignore").
- Repo: `github.com/duyxl56785/Project_quanlykho` (Private).
