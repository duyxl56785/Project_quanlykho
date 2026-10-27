# Kế hoạch triển khai

Kế hoạch chi tiết cho mọi nhiệm vụ trong [NHIEM_VU.md](NHIEM_VU.md). Điều kiện "Xong khi" nằm ở NHIEM_VU.md; file này ghi **làm thế nào**: bảng dữ liệu, service, màn hình, API, test.

Cách dùng: làm nhiệm vụ theo thứ tự trong NHIEM_VU.md, đọc mục cùng tên ở phần C, làm theo quy trình chung. Chỉ dừng lại hỏi khi phải đi lệch khỏi kế hoạch này; khi đó sửa file này trước rồi mới code.

---

## A. Quyết định kỹ thuật chung

**A1. Cấu trúc thư mục** (một dự án web `quanlykho`, một dự án test `quanlykho.Tests`)

```
quanlykho/
  Domain/              entity và enum (SanPham, BienThe, TrangThaiPhieuNhap...)
  Data/                AppDbContext, Configurations/ (cấu hình từng bảng), Migrations/, Seed/
  Common/              dùng chung: CongGhiTon, PhanQuyen, NhatKy, MaChungTu, lỗi nghiệp vụ, định dạng
  Features/<Mang>/     <Mang>Controller (trang), <Mang>ApiController (JSON), <Mang>Service, ViewModels
  Views/<Mang>/        view Razor theo chuẩn MVC
quanlykho.Tests/
  Unit/                logic thuần (sức chứa, G1, G3, quy đổi)
  Integration/         chạy trên PostgreSQL thật bằng Testcontainers
```

Mảng: `TaiKhoan`, `DanhMuc`, `Kho` (kho và vị trí), `NhapKho`, `XepHang`, `XuatKho`, `DieuChuyen`, `KiemKe`, `TongQuan`, `BaoCao`, `NhatKy`, `NhapExcel`, `Styleguide`.

**A2. Trang và API.** Màn hình danh sách và biểu mẫu dùng Razor + form POST tới controller trang. Màn hình quét mã trên máy tính bảng và các thao tác tương tác (ô phù hợp, xếp hàng, lấy hàng, gợi ý) dùng `fetch` tới API JSON `/api/...` theo Bảng 2.35. Cả hai gọi cùng một Service; mọi luật nghiệp vụ nằm trong Service, không nằm trong controller.

**A3. Không có tầng Repository riêng.** `AppDbContext` của EF Core đóng vai trò repository và unit of work; Service dùng thẳng DbContext. (Báo cáo ghi Controller, Service, Repository; khi viết báo cáo, mô tả DbContext là tầng Repository.)

**A4. Đăng nhập.** Cookie authentication của ASP.NET Core, bảng `NguoiDung` theo ERD, mật khẩu băm bằng `PasswordHasher<NguoiDung>` (gói `Microsoft.Extensions.Identity.Core`). Không dùng ASP.NET Identity đầy đủ để giữ bảng đúng ERD. Sự kiện `OnValidatePrincipal` đọc lại `TrangThai` và `SecurityStamp` mỗi request, nên khóa tài khoản hoặc đổi quyền có hiệu lực ngay. Đường dẫn `/api/...` trả 401, 403 dạng JSON thay vì chuyển hướng tới trang đăng nhập.

**A5. Phân quyền theo vai và kho.** 4 vai là hằng số `VaiTro.QuanTriVien | QuanLyKho | ThuKho | NhanVienBanHang`, kiểm bằng policy trên controller. Service `NguoiDungHienTai` cho biết mã người dùng, vai, danh sách kho được giao; mọi Service nhận `maKho` phải gọi `DamBaoQuyenKho(maKho)` (ném `KhongCoQuyenException`, thành 403) và mọi truy vấn danh sách phải lọc theo kho được giao. Quản trị viên thấy mọi kho; nhân viên bán hàng xem tồn khả dụng mọi kho.

**A6. Lỗi.** `NghiepVuException(thongDiep, duLieu)` thành 422 (API) hoặc lỗi ModelState (trang); `XungDotException` thành 409; `KhongCoQuyenException` thành 403. Một filter chung đổi exception ra JSON `{ "loi": "...", "chiTiet": {...} }`.

**A7. Cổng ghi tồn** (`Common/CongGhiTon`), nơi duy nhất sửa `TonKho`:

- `GhiAsync(maKhoaChongLap, loaiChungTu, maChungTu, dong[])` với mỗi dòng `(MaLo, MaViTri, SoLuongThayDoi)`; `GiuAsync` và `BoGiuAsync` đổi `SoLuongDaGiu`.
- Bắt buộc chạy trong transaction do Service gọi mở (để đổi trạng thái chứng từ và ghi tồn cùng một transaction); không có transaction thì ném lỗi.
- Tạo dòng `TonKho` còn thiếu bằng `INSERT ... ON CONFLICT DO NOTHING`, rồi `SELECT ... FOR UPDATE` các dòng liên quan theo thứ tự `(MaLo, MaViTri)` để tránh deadlock.
- Từ chối khi kết quả âm, khi số đã giữ vượt tồn, khi ô đang bị khóa kiểm kê (trừ chứng từ của chính phiếu kiểm kê đó).
- Ghi mỗi dòng vào `SoBienDong` với `MaKhoaChongLap = <khóa>#<số dòng>` (cột unique). Trước khi ghi kiểm `<khóa>#1` đã có chưa; có rồi thì trả "đã ghi trước đó" mà không ghi gì. Hai request cùng khóa chạy song song: request sau vướng unique index, bắt lỗi `23505` và trả "đã ghi trước đó".
- CSDL có thêm ràng buộc `CHECK (SoLuong >= 0 AND SoLuongDaGiu >= 0 AND SoLuongDaGiu <= SoLuong)` làm lưới an toàn cuối.

**A8. Khóa chống ghi lặp từ giao diện.** Form có ô ẩn `IdempotencyKey` (Guid sinh lúc dựng trang); `fetch` gửi header `Idempotency-Key`. Bấm lặp hoặc gửi lại form dùng cùng khóa.

**A9. Mã.** Khóa chính là mã chuỗi theo Bảng 2.34. Mã chứng từ sinh từ sequence PostgreSQL theo tiền tố, dạng `PN000001` (PN phiếu nhập, PX phiếu xuất, DM đơn mua, DD đơn đặt, LX lệnh xuất, DC điều chuyển, KK kiểm kê, L lô). Mã danh mục (sản phẩm, nhà cung cấp) do người dùng nhập, có gợi ý. Bảng nhật ký, sổ biến động, lịch sử gợi ý dùng khóa `bigint` tự tăng.

**A10. Kiểu dữ liệu.** Theo Bảng 2.34 và `CLAUDE.md`. Enum lưu dạng chuỗi tên không dấu (`ChuaThucHien`) kèm `CHECK` danh sách giá trị; tên tiếng Việt có dấu để hiển thị nằm trong một bảng ánh xạ ở `Common`. Thời điểm lưu `timestamptz` (UTC); "hôm nay" và hạn dùng tính theo giờ Việt Nam qua `TimeProvider` (thay được trong test).

**A11. Test.** Logic thuần viết unit test. Mọi thứ đụng CSDL viết integration test bằng `Testcontainers.PostgreSql`: một container cho cả lượt chạy, mỗi lớp test tạo một database riêng rồi chạy migration. CI trên GitHub có sẵn Docker nên chạy được. Ca đồng thời (hai người lấy cùng lúc, hai người xếp cùng ô) dùng `Task.WhenAll` với hai DbContext riêng.

**A12. Giao diện.** Bootstrap 5 có sẵn trong mẫu; biến CSS trong `wwwroot/css/site.css`; hiển thị ngày `dd/MM/yyyy`, số và tiền theo `vi-VN` qua helper (văn hóa request giữ InvariantCulture, xem nhiệm vụ Khung kiến trúc). Máy quét mã vạch USB hoạt động như bàn phím, gõ mã rồi Enter; ô quét mã là `<input autofocus>` bắt phím Enter. Không làm quét bằng camera.

**A13. Excel và mã vạch.** Excel dùng ClosedXML (MIT). Mã vạch Code 128 trên nhãn sinh ở máy chủ dạng SVG bằng ZXing.Net (Apache 2.0), không cần mạng ở máy in.

**A14. Migration và dữ liệu thử.** Mỗi nhiệm vụ tự thêm bảng của mình bằng một migration. Ở môi trường Development, app tự chạy migration lúc khởi động; dữ liệu thử nạp bằng `dotnet run --project quanlykho -- --seed`, chạy lại không nhân đôi.

**A15. Đơn vị trên dòng chứng từ.** Ngay từ đầu, mọi bảng dòng chứng từ có `TenDonVi`, `HeSo`, `SoLuongGoc` (đúng ERD). Trước giai đoạn 6 luôn ghi đơn vị tính chính với `HeSo = 1`; giai đoạn 6 chỉ mở thêm lựa chọn đơn vị trên giao diện, không phải đổi bảng.

---

## B. Bảng dữ liệu theo nhiệm vụ

ERD Hình 2.11 có 13 bảng. Các bảng thêm ngoài ERD được đánh dấu (+).

| Nhiệm vụ | Bảng |
| --- | --- |
| ERD và migration đầu tiên | `NhomHang` (+), `ThuocTinh` (+, thuộc tính theo nhóm), `SanPham`, `BienThe`, `DonViQuyDoi`, `NhaCungCap`, `NoiNhan` (+), `ViTri`, `NguoiDung`, `NguoiDungKho` (+), `MucTon` (+, tối thiểu, tối đa, thời gian chờ hàng theo biến thể và kho), `Lo`, `TonKho`, `SoBienDong`, `DonMua`, `ChiTietDonMua`, `PhieuNhap`, `ChiTietPhieuNhap`, `NhatKy` (+) |
| Xếp hàng | `ChuyenViTri` (+, mỗi lần chuyển một lô giữa hai ô; dùng lại cho chuyển vị trí trong kho) |
| Đơn đặt hàng | `DonDatHang`, `ChiTietDonDat` |
| Duyệt và giữ hàng | `GiuHang` (đơn nào giữ bao nhiêu ở lô, ô nào) |
| G1 | `LichSuGoiY` (dùng chung cho G1, G3) |
| Lệnh xuất | `LenhXuat`, `ChiTietLenhXuat`, `DongLayHang`, `PhieuXuat`, `ChiTietPhieuXuat` |
| Chuyển kho | `PhieuDieuChuyen`, `ChiTietDieuChuyen` |
| Kiểm kê | `PhieuKiemKe`, `ChiTietKiemKe`, `ThanhVienKiemKe` |
| Nhập Excel | `TepDaNhap` (mã băm tệp đã nhập) |

Cột thêm ngoài ERD: `SanPham.MaNhom, MoTa, TheoDoiHanDung, TrangThai`; `BienThe.MaVach, TrangThai`; `ViTri.MaKho, ThuTu, TrangThai, MaPhieuKiemKe, HeSoLapDay (cấp kho), DiaChi (cấp kho)`; `NguoiDung.Email, SoDienThoai, MatKhauBam, TrangThai, SecurityStamp, SoLanDangNhapSai, KhoaDenLuc`; `PhieuNhap.LoaiPhieu`; `DonViQuyDoi.TrangThai`.

---

## C. Kế hoạch từng nhiệm vụ

### Giai đoạn 0. Nền tảng

**Khung kiến trúc theo mảng**
- Tạo các thư mục ở A1; chuyển `/health/db` vào `Common` (giữ nguyên đường dẫn).
- `Common/Loi`: ba exception ở A6 và filter `XuLyLoiFilter` đăng ký toàn cục.
- Đăng ký `TimeProvider.System`; cố định văn hóa request là InvariantCulture để form gửi số `1.5` và ngày `yyyy-MM-dd` đọc đúng trên mọi máy (nếu đặt `vi-VN`, dấu thập phân thành dấu phẩy và ô `type=number` gửi lên bị đọc sai). Hiển thị kiểu Việt Nam qua helper `So()`, `Tien()`, `Ngay()` ở nhiệm vụ giao diện.
- Test: một API thử ném `NghiepVuException` trả 422 với JSON `loi`.
- Cập nhật mục Kiến trúc trong `CLAUDE.md` theo A1 đến A3.

**ERD và migration đầu tiên**
- Entity trong `Domain/`, cấu hình Fluent API trong `Data/Configurations/` cho các bảng ở dòng đầu bảng B: khóa, độ dài, kiểu `numeric`, `CHECK` (số lượng, kích thước, khối lượng dương; tồn không âm; `HeSo > 1`; `Cap` 1 đến 7; trạng thái trong danh sách), unique (`MaKhoaChongLap`, `NguoiDung.Email`, `BienThe.MaVach`, `(MaSanPham, ThuocTinh)`), cột tính `ThanhTien` lưu sẵn, `BienThe.ThuocTinh` kiểu `jsonb`.
- Sequence cho mã chứng từ; service `MaChungTu.TaoAsync("PN")`.
- Chỉ mục: `TonKho(MaViTri)`, `SoBienDong(MaLo, ThoiDiem)`, `SoBienDong(MaChungTu)`, `ViTri(MaKho, LoaiKhu)`, `Lo(MaBienThe, HanDung)`.
- Migration `KhoiTao`; app tự migrate ở Development.
- Thêm `Testcontainers.PostgreSql` vào dự án test và lớp nền `CsdlFixture` (A11).
- Test: migration chạy sạch trên DB trống; chèn `TonKho` âm bị CSDL từ chối; trùng `MaKhoaChongLap` bị từ chối.

**Dữ liệu thử**
- `Data/Seed/DuLieuThu.cs`, chạy bằng `-- --seed`, kiểm theo mã nên chạy lại không nhân đôi.
- 2 kho `K01`, `K02`, mỗi kho cây vị trí khoảng 20 ô: khu tiếp nhận, khu lưu trữ thường, khu lưu trữ lạnh, khu cách ly, khu xuất. Mỗi kho đánh dấu một ô tiếp nhận mặc định và một ô cách ly mặc định.
- 4 nhóm hàng (thời trang: size, màu; điện tử: dung lượng, màu; thực phẩm: quy cách, có hạn dùng; văn phòng phẩm: không thuộc tính), khoảng 12 sản phẩm sinh khoảng 50 biến thể có kích thước, khối lượng, mã vạch; 3 nhà cung cấp; 2 nơi nhận; mức tồn cho vài biến thể.
- Tài khoản mẫu: 1 quản trị viên, 2 quản lý kho (K01, K02), 3 thủ kho (2 ở K01 để thử người kiểm khác người lấy, 1 ở K02), 1 nhân viên bán hàng. Mật khẩu thử lấy từ `appsettings.Development.json` (khóa `DuLieuThu:MatKhau`).
- Tồn ban đầu và lịch sử xuất chưa nạp ở đây (cần cổng ghi tồn); nạp ở nhiệm vụ "Xếp hàng vào vị trí" (hàng đã xếp sẵn) và "Dữ liệu thử đầy đủ" (3 tháng lịch sử xuất).

**Bộ giao diện dùng chung và `/Styleguide`**
- `site.css`: biến màu, cỡ chữ, khoảng cách, màu cố định cho từng trạng thái.
- `_Layout`: thanh menu bên trái theo vai (lọc theo vai làm ở nhiệm vụ đăng nhập), tên người dùng, nút đăng xuất; co lại thành menu gập ở khổ máy tính bảng.
- Partial dùng chung trong `Views/Shared/`: bảng dữ liệu làm thành tag helper `<bang-du-lieu>` (trạng thái rỗng, phân trang giữ tham số lọc, model `TrangDuLieu<T>`) vì partial không bọc được nội dung bảng của từng trang, `_OQuetMa`, `_HuyHieuTrangThai`, `_HopXacNhan` (modal Bootstrap), `_ThongBao` (TempData), `_OChonKho`, `_TrangRong`, `_DangTai`; HTML helper `So()`, `Tien()`, `Ngay()`.
- Bốn trang mẫu trên `/Styleguide`: danh sách, biểu mẫu, phiếu có dòng hàng (thêm, xóa dòng bằng JS), màn hình quét.
- Kiểm tra: xem ở khổ 768 × 1024.

### Giai đoạn 1. Đăng nhập và nhập kho

**Đăng nhập, tài khoản, phân quyền**
- `Common/PhanQuyen`: A4, A5; policy cho từng vai; lọc menu theo vai.
- Mảng `TaiKhoan`: trang đăng nhập, đăng xuất; quản trị viên xem danh sách, thêm, sửa, đổi vai, gán kho, khóa, mở khóa, đặt lại mật khẩu.
- Luật: email không trùng; quản lý kho và thủ kho có ít nhất một kho; không khóa hoặc hạ vai quản trị viên đang hoạt động cuối cùng; không xóa tài khoản; đổi vai, kho hoặc khóa thì đổi `SecurityStamp`.
- Chống dò mật khẩu: sai 5 lần liên tiếp thì khóa tạm 15 phút (`SoLanDangNhapSai`, `KhoaDenLuc`; hai con số cấu hình trong `appsettings.json`); đăng nhập đúng thì đặt lại bộ đếm; thông báo lỗi chung "Email hoặc mật khẩu không đúng" để không lộ email nào tồn tại; ghi nhật ký lần bị khóa tạm. Quản trị viên mở khóa sớm được.
- `Common/NhatKy`: service chỉ thêm, ghi khi phân quyền, khóa, mở khóa (màn hình tra làm ở giai đoạn 5).
- Test: ma trận 4 vai với các trang và API mẫu (200, 403, 401); khóa xong request kế tiếp nhận 401; không khóa được quản trị viên cuối cùng; người K01 bị 403 với dữ liệu K02; sai 5 lần thì lần thứ 6 dù đúng mật khẩu vẫn bị từ chối, hết 15 phút (đổi giờ qua `TimeProvider` giả) thì đăng nhập được.

**Cổng ghi tồn**
- Làm theo A7. Chưa có màn hình.
- Test tích hợp: nhập rồi xuất, tổng `SoBienDong` bằng `TonKho`; cùng khóa gọi hai lần tuần tự và song song chỉ ghi một lần; xuất vượt tồn bị từ chối và không đổi gì; hai tác vụ song song cùng trừ một lô vượt tổng thì một thành công, một thất bại, tồn không âm; giữ vượt tồn bị từ chối; ghi vào ô đang khóa kiểm kê bị từ chối.

**Danh mục**
- Mảng `DanhMuc` (chỉ quản trị viên sửa, vai khác chỉ xem):
  - Nhóm hàng dạng cây; thuộc tính theo nhóm (tên, giá trị gợi ý); chỉ xóa nhóm khi chưa có sản phẩm.
  - Sản phẩm: mã, tên, đơn vị tính chính, nhóm, điều kiện bảo quản, theo dõi hạn dùng (chỉ đổi khi chưa có tồn), mô tả, trạng thái.
  - Sinh biến thể: chọn giá trị cho từng thuộc tính của nhóm, xem trước tổ hợp, tạo; mã biến thể `<MaSanPham>-01`...; tổ hợp trùng bị chặn. Sửa biến thể: kích thước, khối lượng, mã vạch, mức tồn theo kho.
  - Nhà cung cấp, nơi nhận: thêm, sửa, ngừng dùng.
  - Thứ đã có chứng từ thì chỉ ngừng sử dụng, không xóa.
- Test: unit test sinh tổ hợp; tích hợp: tổ hợp trùng bị chặn, đổi cờ hạn dùng khi có tồn bị chặn. Kiểm tra tay: thêm ngành "Đồ gia dụng" với thuộc tính "Công suất", "Bảo hành" trên giao diện.

**Đơn mua**
- Mảng `NhapKho`: tạo đơn (nhà cung cấp, kho, dòng biến thể, số lượng, đơn giá), lưu thành Chưa thực hiện; Nháp chỉ sinh từ G3 và chỉ thành chính thức khi quản lý kho xác nhận.
- Trạng thái do hệ thống tính từ phiếu nhập: chưa có phiếu là Chưa thực hiện; có phiếu thì Đang thực hiện (Chờ kiểm đếm khi còn phiếu Lưu tạm, ngược lại Chờ nhận hàng); đủ số đặt thì Hoàn thành. Hủy được khi chưa có phiếu Hoàn tất.
- Màn hình danh sách (lọc nhà cung cấp, kho, thời gian, trạng thái), biểu mẫu có dòng hàng, chi tiết có tiến độ nhận; API `GET, POST /api/don-mua`, `GET /api/don-mua/{ma}`.
- Test: số chưa nhận; chuyển trạng thái; mỗi biến thể một dòng.

**Hóa đơn nhập kho**
- Lập từ đơn mua (chọn dòng chưa nhận hết) hoặc phiếu nhập khác không cần đơn mua (`LoaiPhieu`). Phiếu ở Lưu tạm.
- Màn hình quét trên máy tính bảng: quét mã vạch ra biến thể, nhập số thực nhận, hạn dùng (bắt buộc khi theo dõi hạn dùng), chất lượng Đạt hoặc Lỗi; mỗi dòng sinh một lô mới. API `POST /api/phieu-nhap`, `POST /api/phieu-nhap/{ma}/dong`, xóa dòng khi còn Lưu tạm.
- Luật: số nhận lớn hơn 0; tổng nhận của các phiếu chưa hủy không vượt số đặt; thành tiền, tổng tiền do hệ thống tính; thủ kho không nhận được giá trong ViewModel và JSON.
- Test: chặn nhận vượt; thiếu hạn dùng bị chặn; JSON trả cho thủ kho không có trường giá.

**Xác nhận hóa đơn nhập**
- `POST /api/phieu-nhap/{ma}/xac-nhan` (quản lý kho, có `Idempotency-Key`): một transaction khóa phiếu, chuyển Hoàn tất, gọi cổng ghi tồn: dòng Đạt vào ô tiếp nhận mặc định, dòng Lỗi vào ô cách ly mặc định; cập nhật trạng thái đơn mua; ghi nhật ký.
- Hủy phiếu Hoàn tất bằng bút toán đảo, chỉ khi toàn bộ hàng của phiếu còn nguyên ở ô tiếp nhận hoặc cách ly.
- Test: bấm lặp chỉ ghi một lần; hàng đúng khu; đơn mua thành Hoàn thành khi đủ; hủy đảo đúng tồn.

### Giai đoạn 2. Vị trí và xếp hàng

**Vị trí cây 7 cấp**
- Mảng `Kho`: cây vị trí của từng kho (mở, thu từng nhánh); thêm con: cấp bằng cấp cha cộng 1, mã bằng mã cha + `-` + tiền tố cấp (`TA`, `TG`, `P`, `H`, `KE`, `O`) + số 2 chữ số; tên ghi đủ đường dẫn. Loại khu và điều kiện bảo quản khai ở cấp phòng – dãy, cấp con kế thừa. Ô bắt buộc kích thước và tải trọng. Khóa, mở khóa; chỉ xóa vị trí chưa từng có hàng và không có con. Xem hàng đang nằm ở một vị trí.
- API `GET /api/vi-tri/cay`, `POST /api/vi-tri`, `PUT /api/vi-tri/{ma}`.
- Test: sinh mã đúng; cấp 8 bị chặn; ô thiếu kích thước bị chặn; vị trí có hàng không xóa được.

**In nhãn mã vạch lô và ô**
- Service `NhanMaVach`: sinh SVG Code 128 bằng ZXing.Net cho một chuỗi mã.
- Trang in nhãn dùng CSS `@media print`, khổ nhãn cấu hình (mặc định 50 × 30 mm, nhiều nhãn trên một trang A4): nhãn ô có mã vạch mã ô và tên đầy đủ; nhãn lô có mã vạch mã lô, tên biến thể, hạn dùng, số lượng.
- Nút in: chọn nhiều ô trên cây vị trí rồi "In nhãn"; trên chi tiết hóa đơn nhập đã xác nhận, "In nhãn lô" (mỗi dòng một nhãn, số bản in nhập được). Ô quét mã ở mọi màn hình nhận được cả mã lô lẫn mã ô.
- Test: SVG sinh ra giải mã lại bằng ZXing ra đúng chuỗi; trang in trả 200 và chứa đủ mã.
- Kiểm tra tay: in thử ra PDF, quét bằng máy quét USB (hoặc ứng dụng quét trên điện thoại) ra đúng mã.

**Danh mục kho, gán người dùng theo kho**
- Danh sách kho (vị trí cấp 1): mã, tên, địa chỉ, loại hàng, hệ số lấp đầy (mặc định 0,85), trạng thái, người được gán; thêm kho; nút "Tạo khu mặc định" sinh nhánh tiếp nhận, lưu trữ, cách ly, xuất và đánh dấu ô mặc định. Gán người dùng dùng chung dữ liệu `NguoiDungKho` với màn hình tài khoản.
- Test: thêm kho thứ hai, gán người, người đó chỉ thấy kho của mình.

**Dịch vụ kiểm tra sức chứa, API ô phù hợp**
- Lớp thuần `SucChua` (toàn bộ `decimal`): vừa kích thước (so ba chiều đã sắp tăng dần); số xếp theo lưới (thử 6 cách xoay, lấy lớn nhất) cho ô chỉ chứa đúng biến thể này hoặc ô trống; số theo thể tích với hệ số lấp đầy cho ô nhiều biến thể; số theo tải trọng; `q_max` là nhỏ nhất.
- Service `OPhuHop`: ô cấp 7 thuộc khu lưu trữ, đúng điều kiện bảo quản, không khóa, cùng kho với lô, còn chỗ; trả số tối đa còn xếp, thể tích và tải trọng còn lại, sắp theo mã vị trí. API `GET /api/vi-tri/o-phu-hop?maLo=&soLuong=`.
- Test: đúng ba ví dụ ở mục 2.16.3 và các ca biên (ô đầy, khối lượng bằng đúng tải trọng).

**Xếp hàng vào vị trí**
- Màn hình quét: chọn hoặc quét lô đang ở khu tiếp nhận, xem ô phù hợp, quét ô, nhập số lượng. API `POST /api/xep-hang` (maLo, maViTriDi, maViTri, soLuong, `Idempotency-Key`).
- Transaction: `SELECT ... FOR UPDATE NOWAIT` dòng ô đích (vướng khóa thì 409 "Ô vừa được cập nhật, vui lòng thử lại"), kiểm lại toàn bộ Bảng 2.33, ghi `ChuyenViTri` và gọi cổng: trừ ở ô đi, cộng ở ô đích. Vượt sức chứa thì 422 kèm số tối đa.
- Dữ liệu thử: thêm phiếu nhập mẫu đã xác nhận và đã xếp cho mỗi kho.
- Test: hai tác vụ song song cùng xếp vào một ô vượt sức chứa thì chỉ một thành công; tổng tồn không đổi sau xếp.

### Giai đoạn 3. Xuất kho

Thứ tự trong giai đoạn: đơn đặt hàng, G1, duyệt và giữ hàng (dùng G1 để chọn lô, ô cần giữ), lệnh xuất.

**Đơn đặt hàng và tồn khả dụng theo kho**
- Service `TonKhaDung` (một truy vấn dùng chung cho đơn đặt, G1, giữ hàng, báo cáo): tổng `SoLuong - SoLuongDaGiu` của các dòng `TonKho` ở ô khu lưu trữ, không khóa kiểm kê, lô chưa hết hạn; theo biến thể và kho.
- Mảng `XuatKho`: nhân viên bán hàng tra tồn khả dụng (theo mã, tên, mã vạch, từng kho và tổng), tạo đơn (kho xuất, nơi nhận, dòng biến thể, số lượng, hạn giao), gửi duyệt, xem danh sách đơn của mình và trạng thái, hủy trước khi xuất.
- Test: tồn khả dụng loại trừ đúng hàng chờ kiểm, cách ly, hết hạn, đã giữ, đang khóa kiểm kê.

**Gợi ý lấy hàng G1**
- Lớp thuần `GoiYLayHang`: đầu vào danh sách ứng viên (lô, ô, khả dụng, hạn dùng, ngày nhập, mã ô) và `q`; sắp lô theo hạn dùng (FEFO, lô không có hạn xếp sau) rồi ngày nhập (FIFO); lấy lần lượt theo lô; trong cùng lô ưu tiên một ô lấy đủ phần còn thiếu, không có thì lấy ô nhiều hàng nhất trước; cuối cùng sắp thứ tự ghé theo mã ô. Mỗi dòng kèm lý do ("hết hạn sớm nhất 12/11/2026", "lấy đủ ở một kệ"). Thiếu hàng thì trả số còn thiếu.
- Service lấy ứng viên từ `TonKhaDung`, ghi `LichSuGoiY` (đầu vào, kết quả). API `GET /api/goi-y/lay-hang?maKho=&maBienThe=&soLuong=`.
- Test: không bao giờ chọn lô hết hạn; đúng thứ tự FEFO rồi FIFO; tổng đúng `q`; cùng hạn dùng thì ít kệ hơn; thiếu hàng báo đúng.

**Duyệt đơn và giữ hàng**
- Quản lý kho duyệt một hoặc nhiều đơn: với mỗi dòng gọi G1 chọn lô, ô; một transaction gọi `CongGhiTon.GiuAsync` và ghi `GiuHang`; đơn thành Đã duyệt. Thiếu thì 422 "Cần X, còn Y" cho từng dòng, không giữ gì. Bỏ giữ trả lại tồn khả dụng.
- Test: hai lần duyệt song song tranh cùng hàng thì không giữ trùng; thiếu báo đúng; chỉ giữ lô đạt, chưa hết hạn.

**Lệnh xuất, lấy hàng, kiểm tra, xác nhận xuất**
- Lập lệnh từ đơn Đã duyệt (một đơn một lệnh, đơn chỉ thuộc một kho), phân công thủ kho của kho đó.
- Chờ lấy hàng (máy tính bảng): danh sách lấy theo `GiuHang` sắp theo thứ tự ghé; quét ô rồi quét hàng, nhập số; chặn sai biến thể, vượt số yêu cầu, lô hết hạn, ô khác không có hàng khả dụng. Cho lấy ở ô khác nếu còn hàng chưa giữ (ghi `LichSuGoiY` là không chấp nhận gợi ý).
- Chờ kiểm tra: quản lý kho khác người lấy đối chiếu dòng; đúng thì Chờ xác nhận.
- Xác nhận: một transaction sinh `PhieuXuat` Hoàn tất, bỏ giữ phần đã giữ, gọi cổng trừ tồn theo `DongLayHang`, cập nhật số đã xuất của đơn, đơn Hoàn thành khi đủ; ghi nhật ký.
- Phiếu xuất khác và xuất trả nhà cung cấp: quản lý kho lập trực tiếp (dùng lại ở kiểm kê, xử lý hàng quá hạn).
- Test: luồng trọn Hình 2.6; người kiểm trùng người lấy bị 422; hai thủ kho lấy cùng lúc không làm tồn âm; bấm xác nhận lặp không trừ hai lần.

### Giai đoạn 4. Chuyển kho và kiểm kê

**Chuyển kho hai bước**
- Mảng `DieuChuyen`: lập phiếu (kho đi khác kho đến, ngày, phương tiện, người vận chuyển, lý do, dòng lô và số lượng), Lưu tạm hoặc Lưu. Lưu: kiểm tồn khả dụng, gọi cổng trừ ở kho đi, phiếu Chờ nhập kho. Hàng đang vận chuyển là phần chưa nhận của phiếu Chờ nhập kho và Nhận một phần (không nằm trong `TonKho`).
- Thủ kho kho đến nhận: nhập số thực nhận từng dòng, gọi cổng cộng vào ô tiếp nhận mặc định của kho đến (giữ nguyên mã lô), rồi xếp bằng màn hình xếp hàng. Nhận thiếu thì Nhận một phần và dòng có chênh lệch chờ xử lý. Menu của kho đến hiện số phiếu đang chờ nhận.
- Hủy khi chưa nhận: cổng cộng lại ở kho đi.
- Test: tổng tồn các kho cộng hàng đang vận chuyển không đổi qua mọi bước; nhận thiếu sinh chênh lệch.

**Kiểm kê**
- Mảng `KiemKe`: lập phiếu (kho, phạm vi: toàn kho, theo vị trí, theo lô; mục đích; ngày; thành viên). Bắt đầu: khóa các ô trong phạm vi (`ViTri.MaPhieuKiemKe`; ô đã thuộc phiếu khác thì chặn; ô có hàng đang giữ thì cảnh báo, quản lý kho xác nhận), chốt số sổ sách. Cổng ghi tồn tự chặn mọi ghi vào ô đang khóa (A7).
- Thành viên nhập số đếm từng dòng trên máy tính bảng, thêm hàng ngoài danh sách; hệ thống tính chênh lệch.
- Test: ô đang kiểm từ chối nhập, xuất, xếp, chuyển; chênh lệch tính đúng; một ô không thuộc hai phiếu.

**Duyệt điều chỉnh tồn**
- Kiểm kê: mỗi dòng chênh lệch khác 0 phải chọn loại và ghi kết luận; quản lý kho duyệt thì hệ thống sinh chứng từ qua cổng: thừa thành phiếu nhập khác; thiếu thành phiếu xuất khác; sai vị trí thành `ChuyenViTri`; sai biến thể thành xuất lô biến thể sai và nhập lô mới cho biến thể đúng (cùng hạn dùng); hư hỏng, hết hạn thành chuyển sang ô cách ly. Dòng đánh dấu Đã xử lý; hết dòng thì Hoàn thành kiểm kê và mở khóa ô.
- Chuyển kho: chênh lệch xử lý bằng nhập tại kho đến, nhập lại tại kho đi (cổng cộng ở kho đi) hoặc ghi giảm hao hụt (đóng dòng với lý do, không đổi `TonKho` vì hàng đã rời sổ kho đi); phiếu Hoàn thành khi mọi dòng đã đủ hoặc đã xử lý.
- Mọi xử lý bắt buộc lý do, người duyệt, ghi nhật ký.
- Test: kịch bản chuyển kho nhận thiếu và kiểm kê lệch ở Bảng 1.6; tổng biến động bằng tồn sau xử lý.

### Giai đoạn 5. Hỗ trợ quyết định, theo dõi, nhập Excel

**Đề xuất nhập bổ sung G3**
- Lớp thuần `DeXuatNhapBoSung` theo công thức mục 1.2.3. `d` là tổng xuất bán (phiếu xuất Hoàn tất) trong `N` ngày gần nhất chia `N`, với `N` là số ngày từ lần xuất đầu tiên, tối đa 90; bỏ qua khi `N < 14` hoặc `d = 0`. `L` lấy ở `MucTon.ThoiGianChoHang` (mặc định 7 ngày); `m` là tồn tối thiểu; số đang chờ nhập là phần chưa nhận của đơn mua chưa hoàn thành.
- Màn hình danh sách đề xuất theo kho, kèm lý do; chọn dòng rồi "Tạo đơn mua nháp" (gom theo nhà cung cấp chọn trên màn hình); ghi `LichSuGoiY`, đánh dấu chấp nhận khi đơn nháp được xác nhận.
- Test: các ca tính tay; `d = 0` và dưới 14 ngày không đề xuất; làm tròn lên.

**Chuyển vị trí trong cùng kho**
- Dùng lại API xếp hàng với ô đi là ô lưu trữ; chặn chuyển phần hàng đang giữ.
- Test: tồn kho không đổi, sổ biến động có cặp trừ, cộng.

**Cảnh báo và màn hình Tổng quan**
- Mảng `TongQuan`: 4 thẻ dùng truy vấn từ `TonKhaDung`, `MucTon`, `Lo`; số ngày cảnh báo nhập trong hộp "Chọn tham số" (mặc định 30, lưu cookie); dòng "Số liệu tính đến" và nút Tải lại.
- Test: dữ liệu mẫu cho từng thẻ ra đúng dòng.

**Báo cáo tồn tổng hợp và chi tiết, thẻ kho**
- Mảng `BaoCao`: tổng hợp theo kho và biến thể (tồn từng kho cạnh tổng); chi tiết theo lô và hạn dùng; theo vị trí; nhập xuất tồn theo kỳ (tồn đầu kỳ là tổng biến động trước ngày bắt đầu); thẻ kho (sổ biến động có số dư lũy kế); hàng đang vận chuyển và hàng ở khu tiếp nhận, cách ly tách riêng. Lọc theo kho, kỳ, sản phẩm, nhóm; phân trang.
- Test: tổng báo cáo bằng tổng `SoBienDong` từng kho.

**Nhật ký thao tác**
- Màn hình tra cho quản trị viên: lọc người, kho, loại thao tác, thời gian.
- Migration thêm trigger PostgreSQL chặn `UPDATE`, `DELETE` trên `NhatKy` và `SoBienDong`.
- Rà lại để chắc mọi thao tác duyệt, hủy, hoàn thành, điều chỉnh, phân quyền, khóa đã ghi nhật ký.
- Test: `UPDATE NhatKy` bị CSDL từ chối; mỗi kịch bản tích hợp sinh đủ dòng nhật ký.

**Nhập Excel danh mục**
- Mảng `NhapExcel`: tải tệp mẫu (sinh bằng ClosedXML, các sheet SanPham, BienThe, NhaCungCap); tải lên, đọc, kiểm lỗi toàn bộ dòng (thiếu cột bắt buộc, mã trùng trong tệp hoặc đã có, giá trị sai kiểu), hiện xem trước kèm danh sách lỗi theo số dòng; chỉ cho ghi khi không còn lỗi, ghi trong một transaction; lưu SHA-256 của tệp vào `TepDaNhap` để chặn nhập lặp.
- Tệp thử 300 dòng có 5 dòng lỗi sinh bằng code trong `quanlykho.Tests/TepMau/`.
- Test: đúng tiêu chí ở NHIEM_VU.md.

### Giai đoạn 6. Quy đổi đơn vị và tồn đầu kỳ

**Nền quy đổi đơn vị**
- Service `QuyDoiDonVi`: `QuyVeChinh(maBienThe, tenDonVi, soLuongGoc)` trả hệ số và số theo đơn vị chính; `HienThi(maBienThe, soLuong)` trả "9 thùng 19 chai"; tra mã vạch ra (biến thể, đơn vị). Màn hình khai báo đơn vị quy đổi trong biểu mẫu biến thể. Partial `_OChonDonVi` thêm vào `/Styleguide`.
- Test: 10 thùng ra 240 chai; hệ số không nguyên hoặc nhỏ hơn 2 bị từ chối; tên đơn vị trùng bị từ chối.

**Quy đổi trên chứng từ** (bốn nhiệm vụ: danh mục, đơn mua, đơn đặt; hóa đơn nhập; xuất kho, chuyển kho, báo cáo; kiểm kê)
- Mỗi màn hình có dòng hàng thêm `_OChonDonVi`; Service gọi `QuyVeChinh` trước mọi so sánh và trước khi gọi cổng, lưu `TenDonVi`, `HeSo`, `SoLuongGoc` vào dòng; thành tiền bằng `SoLuongGoc × DonGia`. Hiển thị tồn kèm `HienThi`. Kiểm kê cho nhập số thùng và số lẻ.
- Test: các ca số ở NHIEM_VU.md.

**Nhập tồn đầu kỳ từ Excel**
- Tệp mẫu (biến thể, số lô, hạn dùng, ô, số lượng, chất lượng) cho một kho; kiểm lỗi từng dòng gồm sức chứa ô theo `SucChua`; sinh phiếu nhập khác loại Tồn đầu kỳ ở Lưu tạm, hàng vào thẳng ô ghi trong tệp; quản lý kho xác nhận thì ghi qua cổng; chặn nhập lặp bằng mã băm.
- Test: đúng tiêu chí ở NHIEM_VU.md.

### Giai đoạn 7. Kiểm thử tổng và nghiệm thu

- Mở rộng `--seed`: nạp 3 tháng lịch sử xuất (phiếu xuất Hoàn tất rải theo ngày, ghi qua cổng) để G3 có dữ liệu; tệp Excel mẫu.
- Sao lưu và khôi phục: `scripts/sao-luu.ps1` chạy `docker exec quanlykho-db pg_dump -Fc` ra `backups/quanlykho-<yyyyMMdd-HHmm>.dump`, giữ 14 bản mới nhất; `scripts/khoi-phuc.ps1 <tệp>` dùng `pg_restore --clean` vào DB (hỏi xác nhận trước khi ghi đè). Thêm `backups/` vào `.gitignore`. Hướng dẫn trong `README.md` cách đặt Windows Task Scheduler chạy hằng ngày. Kiểm tra: sao lưu, xóa vài dòng, khôi phục, dữ liệu trở lại đủ.
- Đo thời gian tải các trang chính trên dữ liệu thử (công cụ DevTools); trang nào trên 3 giây thì thêm chỉ mục hoặc phân trang.
- Chạy từng tiêu chí Bảng 1.6 bằng tay theo kịch bản, đánh dấu ở NHIEM_VU.md; lỗi tìm được ghi thành nhiệm vụ sửa.
- Viết `README.md` (cài đặt, chạy, tài khoản thử, chạy test); gộp `develop` vào `main`, tag `v1.0`.

---

## D. Đơn giản hóa so với báo cáo

1. Không có tầng Repository riêng (A3).
2. Không làm chứng từ "yêu cầu kiểm kê" và "yêu cầu điều chuyển"; lập phiếu trực tiếp (báo cáo cho phép lập trực tiếp).
3. `TheoDoiHanDung` đặt ở `SanPham` (theo mục 2.8 dòng 5) thay vì `BienThe` như ERD.
4. Xếp hàng và chuyển vị trí trong kho ghi bằng bản ghi `ChuyenViTri`, không có phiếu riêng.
5. Một đơn đặt hàng sinh một lệnh xuất; không gộp nhiều đơn vào một lệnh, không nhân bản lệnh.
6. Thông báo cho kho đến là số đếm trên menu, không đẩy thời gian thực.
7. Hủy phiếu nhập đã hoàn tất chỉ khi hàng còn nguyên ở khu tiếp nhận hoặc cách ly.
8. Giai đoạn 3 làm G1 trước duyệt và giữ hàng.

## E. Rủi ro

- Phần đồng thời (giữ hàng, lấy hàng, xếp ô) dễ sai nhất: luôn viết test song song trước khi làm màn hình.
- Testcontainers cần Docker đang chạy khi `dotnet test`; quên bật Docker thì test tích hợp báo lỗi kết nối.
- Phạm vi lớn cho một người: nếu chậm, cắt theo thứ tự ở cuối mục 1.3 của báo cáo (bàn làm việc, rồi cảnh báo, báo cáo, chuyển vị trí, rồi bớt quy đổi ở kiểm kê, chuyển kho, cuối cùng bỏ tồn đầu kỳ từ Excel).
