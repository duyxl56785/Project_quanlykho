# Bảng nhiệm vụ

Theo dõi tiến độ từ lúc dựng khung đến khi dự án chạy đủ Must và Should. Thứ tự lấy theo lộ trình sprint ở mục 1.3 của [báo cáo](BaoCao_DoAn_QuanLyKho.md), gộp lại cho một người làm. Mã trong ngoặc là mã yêu cầu (FR, NFR) hoặc mã việc (S1-05...) trong báo cáo.

Đánh dấu `[x]` khi nhiệm vụ đạt hết điều kiện "Xong khi" và đã gộp vào `develop`. Ghi mã commit gộp sau dấu `—` để tra lại.

## Quy trình làm một nhiệm vụ

1. Đọc mục cùng tên trong [KE_HOACH.md](KE_HOACH.md) và mục báo cáo liên quan. Cần làm khác kế hoạch thì sửa KE_HOACH.md và hỏi trước khi code.
2. Tạo nhánh `feature/<ten-ngan>` từ `develop`.
3. Viết ca kiểm thử cho điều kiện "Xong khi" trước khi code (với logic nghiệp vụ).
4. Code: entity, migration, service, controller, API, view.
5. Kiểm tra: `dotnet build`, `dotnet test`, chạy app với `docker compose up -d` và thử trọn luồng trên trình duyệt (cả khổ máy tính bảng nếu là màn hình thủ kho).
6. Commit trên nhánh feature, gộp `--no-ff` vào `develop`, push `develop`.
7. Tích nhiệm vụ trong file này.

Cuối mỗi giai đoạn: chạy lại toàn bộ test, gộp `develop` vào `main` và gắn tag (`v0.1`, `v0.2`...).

---

## Giai đoạn 0. Nền tảng

- [x] Môi trường, Git, nhánh `main` và `develop`, repo Private — a0c63d0
- [x] Báo cáo vào `docs/`, `CLAUDE.md` — e9116ad, 8d6778f
- [x] PostgreSQL bằng Docker, EF Core, `/health/db` — 252d004
- [x] Dự án test và CI (S1-09) — 7539460
  - Xong khi: có dự án xUnit trong `quanlykho.slnx`, một test mẫu chạy được bằng `dotnet test`; GitHub Actions chạy build và test trên mỗi lần push `develop`.
- [x] Khung kiến trúc theo mảng (S1-02) — 7b6f60a
  - Xong khi: có quy ước thư mục Controller, Service, Repository theo mảng; ghi vào `CLAUDE.md`.
- [x] ERD và migration đầu tiên (S1-05, S1-06) — c139f09
  - Xong khi: entity và migration cho danh mục, vị trí, lô, tồn kho, sổ biến động, đơn mua, phiếu nhập, đơn vị quy đổi theo Bảng 2.32, 2.34 và Hình 2.11; `dotnet ef database update` chạy sạch trên DB trống; ràng buộc (khóa, không âm, unique) có trong CSDL.
- [x] Dữ liệu thử (S1-06) — b6ae3f0
  - Xong khi: seed tạo 2 kho, khoảng 40 ô, khoảng 50 biến thể thuộc 4 ngành (thời trang, điện tử, thực phẩm, văn phòng phẩm), tài khoản mẫu cho 4 vai; chạy lại không nhân đôi dữ liệu.
- [x] Bộ giao diện dùng chung và `/Styleguide` (S1-07) — b73837d
  - Xong khi: một file CSS biến, `_Layout` mới, partial cho bảng dữ liệu, ô biểu mẫu, ô quét mã, huy hiệu trạng thái, hộp xác nhận, thông báo, ô chọn kho; trang `/Styleguide` hiện đủ; hiển thị tốt ở khổ máy tính bảng.

## Giai đoạn 1. Đăng nhập và nhập kho

- [x] Đăng nhập, tài khoản, phân quyền theo vai và kho (FR09, NFR02, S2-01) — 22df0d7
  - Xong khi: 4 vai; quản lý kho và thủ kho gán theo kho; quyền kiểm tra ở máy chủ (401, 403); khóa tài khoản thì mất quyền ngay; luôn còn ít nhất một quản trị viên; sai mật khẩu 5 lần liên tiếp thì khóa tạm 15 phút; test phân quyền cho cả 4 vai đều đạt.
- [x] Cổng ghi tồn (NFR01, S2-02) — 489445e
  - Xong khi: một service duy nhất cập nhật `TonKho` và ghi `SoBienDong` trong một transaction; `Idempotency-Key` trùng thì không ghi lần hai; không cho tồn âm; test tổng biến động bằng tồn.
- [x] Danh mục: nhóm hàng, thuộc tính, sản phẩm, biến thể, nhà cung cấp, nơi nhận (FR10, NFR06, S2-03) — a72c3b1
  - Xong khi: thêm ngành hàng mới với thuộc tính mới trên giao diện, sinh biến thể từ tổ hợp thuộc tính, khai báo kích thước, khối lượng, cờ hạn dùng, điều kiện bảo quản; không xóa cứng thứ đã có chứng từ.
- [x] Đơn mua (FR01, S2-04) — 77ba3cb
  - Xong khi: lập, duyệt đơn mua theo nhà cung cấp và kho; số chưa nhận tự cập nhật; trạng thái theo Bảng 2.14 do hệ thống tính.
- [x] Hóa đơn nhập kho (FR01, S2-05) — dfb703d
  - Xong khi: lập từ đơn mua, quét mã, ghi số thực nhận, lô tự sinh, bắt hạn dùng, phân loại đạt hoặc lỗi; tính thành tiền, tổng tiền; chặn số nhận vượt số chưa nhận; thủ kho không thấy giá; dùng được trên máy tính bảng.
- [x] Xác nhận hóa đơn nhập (FR01, S2-06) — e39753c
  - Xong khi: xác nhận ghi tồn qua cổng; hàng đạt vào khu tiếp nhận, hàng lỗi vào khu cách ly (Hình 2.5); bấm lặp không ghi hai lần.

## Giai đoạn 2. Vị trí và xếp hàng

- [x] Vị trí cây 7 cấp (FR11, S3-01) — 0571a5c
  - Xong khi: tạo được mã `K01-TA02-TG03-P04-H05-KE06-O07` trên giao diện; loại khu, điều kiện bảo quản, kích thước và tải trọng ô; vị trí có hàng chỉ khóa, không xóa.
- [x] In nhãn mã vạch lô và ô — fc21699
  - Xong khi: in được nhãn Code 128 cho nhiều ô và cho các lô của một hóa đơn nhập; nhãn in ra quét lại bằng máy quét ra đúng mã; ô quét mã nhận cả mã lô và mã ô.
- [x] Danh mục kho, gán người dùng theo kho (S3-03) — 8c3a4bc
  - Xong khi: thêm kho thứ hai, gán thủ kho và quản lý kho; người dùng chỉ thấy kho được giao.
- [x] Dịch vụ kiểm tra sức chứa, API ô phù hợp (FR12, S3-02) — 897fda9
  - Xong khi: đạt các ca biên ở mục 2.16.3 (ví dụ ô 1 m³, 50 kg, thùng 12 kg cho q_max = 4; thanh 1,2 m bị chặn; kiện 0,4 × 1 × 0,5 m ra 4 chứ không phải 5); chỉ trả ô đúng bảo quản, không khóa, còn chỗ.
- [x] Xếp hàng vào vị trí (FR12, S3-04) — f815599
  - Xong khi: quét lô, quét ô, hiện chỗ còn lại; khóa dòng ô và ghi qua cổng trong một transaction; hai người xếp cùng ô thì người sau nhận 409; đi trọn luồng Hình 2.10.

## Giai đoạn 3. Xuất kho

- [x] Đơn đặt hàng và tồn khả dụng theo kho (FR05, FR06, S4-01) — 75bdb00
  - Xong khi: nhân viên bán hàng xem tồn khả dụng từng kho (trừ hàng chờ kiểm, cách ly, hết hạn, đang vận chuyển, đã giữ), tạo đơn, xem trạng thái.
- [x] Gợi ý lấy hàng G1 (FR20, S4-02) — 3747758
  - Xong khi: không bao giờ gợi ý lô hết hạn; ưu tiên FEFO, rồi FIFO, rồi ít kệ; trả lý do; ghi lịch sử gợi ý và việc người dùng chấp nhận hay không; unit test cho các ca ràng buộc.
- [x] Duyệt đơn và giữ hàng (FR02, S4-01) — a4addd1
  - Xong khi: hai đơn không giữ trùng một số hàng; thiếu thì báo "Cần X, còn Y" (422); chỉ giữ lô đạt chất lượng, chưa hết hạn.
- [x] Lệnh xuất, lấy hàng, kiểm tra, xác nhận xuất (FR03, FR04, S4-03) — 35e0605
  - Xong khi: quét sai hàng, lấy vượt số, lấy lô hết hạn đều bị chặn; người kiểm khác người lấy; phiếu xuất ghi qua cổng; test hai người lấy cùng lúc không làm tồn âm; đi trọn luồng Hình 2.6 trên máy tính bảng.

## Giai đoạn 4. Chuyển kho và kiểm kê

- [x] Chuyển kho hai bước (FR07, S5-01) — c40920e
  - Xong khi: kho đi lưu thì trừ tồn, phiếu Chờ nhập kho; kho đến nhận, xếp; nhận thiếu thì Nhận một phần và sinh chênh lệch; tổng tồn toàn hệ thống trước và sau bằng nhau (Hình 2.7).
- [x] Kiểm kê (FR08, S5-02) — f201de6
  - Xong khi: lập yêu cầu, phiếu, chọn phạm vi; vị trí đang kiểm bị khóa với mọi thao tác; nhập số đếm, hệ thống tính chênh lệch; không sửa thẳng tồn (Hình 2.8).
- [x] Duyệt điều chỉnh tồn (S5-03) — 45d3399
  - Xong khi: chênh lệch chuyển kho (nhập lại, ghi giảm hao hụt) và chênh lệch kiểm kê (6 loại) đều phải phân loại, có lý do, có người duyệt, sinh chứng từ qua cổng; đạt kịch bản chuyển kho nhận thiếu và kiểm kê lệch.

## Giai đoạn 5. Hỗ trợ quyết định, theo dõi, nhập Excel

- [x] Đề xuất nhập bổ sung G3 (FR21, S6-01) — 656d7e6
  - Xong khi: công thức ở mục 1.2.3 theo từng kho; không đề xuất khi d = 0 hoặc dưới 14 ngày lịch sử; tạo đơn mua nháp; khớp với tính tay trên dữ liệu mẫu (Hình 2.9).
- [x] Chuyển vị trí trong cùng kho (FR16, S6-02) — b4ef867
  - Xong khi: chuyển một lô từ ô này sang ô khác, kiểm sức chứa ô đến, không đổi tồn kho, ghi sổ biến động.
- [x] Cảnh báo và màn hình Tổng quan (FR13, S6-04) — 3473e2c
  - Xong khi: 4 thẻ (dưới tối thiểu, vượt tối đa, sắp hết hạn, quá hạn); số ngày cảnh báo cấu hình được; số liệu khớp báo cáo.
- [x] Báo cáo tồn tổng hợp và chi tiết, thẻ kho (FR14, S6-04) — 70441c7
  - Xong khi: theo kho, lô, hạn dùng, vị trí; tồn từng kho cạnh tồn tổng hợp; hàng đang vận chuyển tách riêng; báo cáo khớp thẻ kho.
- [x] Nhật ký thao tác (FR18, NFR03, S6-05) — 9ffd736
  - Xong khi: ghi duyệt, hủy, hoàn thành, điều chỉnh tồn, phân quyền, khóa tài khoản; chỉ thêm, không sửa, không xóa; tra theo người, kho, loại, thời gian.
- [x] Nhập Excel danh mục (FR17, S6-07) — 4014fd3
  - Xong khi: tệp mẫu `.xlsx` 300 dòng có 5 dòng lỗi thì báo đúng số dòng và không ghi gì; sửa xong nhập lại thì ghi đủ; nhập lại cùng tệp bị chặn bằng mã băm.

## Giai đoạn 6. Quy đổi đơn vị và tồn đầu kỳ

- [x] Nền quy đổi đơn vị (FR28, S6-03, S6-08) — bc690f6
  - Xong khi: bảng `DonViQuyDoi`, cột `TenDonVi`, `HeSo`, `SoLuongGoc` trên dòng chứng từ; dịch vụ quy đổi; 10 thùng (1 thùng = 24 chai) ra 240 chai; hệ số không nguyên hoặc nhỏ hơn 2 bị từ chối; ô chọn đơn vị có trên `/Styleguide`.
- [x] Quy đổi ở danh mục, đơn mua, đơn đặt hàng (S7-04) — 438fd79
  - Xong khi: đặt 3 thùng giữ đúng 72 chai; tồn 235 chai hiện "9 thùng 19 chai".
- [x] Quy đổi ở hóa đơn nhập (S7-01) — 531999c
  - Xong khi: nhập 10 thùng ghi tồn 240 chai; quét mã thùng nhận ra đơn vị thùng.
- [x] Quy đổi ở xuất kho, chuyển kho, báo cáo (S7-05) — 43da386
  - Xong khi: xuất 5 chai từ lô 10 thùng còn 235 chai; chuyển 2 thùng trừ đúng 48 chai; tổng biến động bằng tồn.
- [x] Quy đổi ở kiểm kê (S7-02) — f636cdc
  - Xong khi: đếm 9 thùng 19 chai so đúng với sổ sách 235 chai.
- [x] Nhập tồn đầu kỳ từ Excel (FR17, S7-06) — c375b6a
  - Xong khi: tệp 300 dòng sinh phiếu nhập khác loại Tồn đầu kỳ ở trạng thái Lưu tạm; quản lý kho xác nhận thì ghi đúng tồn; dòng vượt sức chứa bị báo lỗi; nhập lại cùng tệp bị chặn.

## Giai đoạn 7. Kiểm thử tổng và nghiệm thu

- [x] Dữ liệu thử đầy đủ: 3 tháng lịch sử xuất, tệp Excel mẫu khoảng 300 dòng có dòng lỗi (S7-03) — 7f81f38
- [x] Sao lưu và khôi phục bằng `pg_dump` (NFR09) — 78ba367
  - Xong khi: script sao lưu tạo tệp có ngày giờ và giữ 14 bản mới nhất; script khôi phục đưa DB về đúng bản sao lưu; có hướng dẫn đặt lịch chạy hằng ngày.
- [x] Đo thời gian tải các trang dưới 3 giây (NFR04) — 19969f3, kết quả ở [DO_TAI_TRANG.md](DO_TAI_TRANG.md)
- [ ] Màn hình tra tồn và trạng thái đơn hiển thị tốt trên điện thoại (NFR07)
- [ ] Đạt tiêu chí nghiệm thu Bảng 1.6:
  - [ ] Thủ kho: chạy trọn nhập hàng, lấy hàng, nhận hàng chuyển đến bằng ô quét mã trên máy tính bảng; G1 không vi phạm ràng buộc; quét sai bị chặn.
  - [ ] Quản lý kho: tổng biến động bằng tồn từng kho; chuyển kho không mất hay nhân đôi hàng; hai phiếu không giữ trùng; mọi chênh lệch có người duyệt; ca 10 thùng xuất 5 chai còn 235 chai; G3 không bỏ sót mặt hàng dưới điểm đặt hàng.
  - [ ] Nhân viên bán hàng: tồn khả dụng từng kho khớp số xuất được; luôn xem được trạng thái đơn.
  - [ ] Quản trị viên: thêm ngành hàng mới không sửa mã rồi nhập, xuất được; phân quyền theo vai và kho đúng; nhật ký đủ; nhập Excel báo lỗi đúng dòng, nhập lại không nhân đôi.
  - [ ] Chung: không có tồn âm; thống kê được tỷ lệ gợi ý AI được chấp nhận.
- [ ] Gộp `develop` vào `main`, gắn tag `v1.0`, ghi hướng dẫn chạy vào `README.md`

## Nếu còn thời gian (Could)

- [ ] Bàn làm việc (FR19)
- [ ] Xuất Excel báo cáo tồn (NFR08)
