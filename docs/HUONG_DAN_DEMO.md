# Hướng dẫn demo

Kịch bản trình diễn mọi tính năng đã làm, theo thứ tự một ngày làm việc của kho. Mỗi phần ghi tài khoản đăng nhập, các bước bấm và điều cần chỉ cho người xem. Toàn bộ khoảng 40–50 phút; thiếu thời gian thì chạy các phần 1, 2, 3, 4, 5, 6, 9.

## Chuẩn bị

Dùng CSDL sạch để mã chứng từ, mã lô khớp với hướng dẫn này (xóa hết dữ liệu cũ):

```bash
docker compose down -v
docker compose up -d
dotnet run --project quanlykho -- --seed
dotnet run --project quanlykho
```

Mở http://localhost:5166. Mật khẩu chung của mọi tài khoản ở khóa `DuLieuThu:MatKhau` trong `quanlykho/appsettings.Development.json`. Nên mở hai trình duyệt (hoặc một cửa sổ ẩn danh) để đăng nhập hai vai cùng lúc. Bấm F12, bật chế độ thiết bị (iPad, 768 × 1024) khi demo màn hình thủ kho; iPhone (375 px) khi demo nhân viên bán hàng.

| Tài khoản | Vai | Kho |
| --- | --- | --- |
| `admin@kho.test` | Quản trị viên | Mọi kho |
| `ql.k01@kho.test` | Quản lý kho | K01 Kho Hà Nội |
| `ql.k02@kho.test` | Quản lý kho | K02 Kho Bắc Ninh |
| `tk1.k01@kho.test`, `tk2.k01@kho.test` | Thủ kho | K01 |
| `tk.k02@kho.test` | Thủ kho | K02 |
| `banhang@kho.test` | Nhân viên bán hàng | Mọi kho (chỉ xem tồn khả dụng, đặt hàng) |

### Dữ liệu mẫu có sẵn sau `--seed`

- Danh mục: 2 kho, mỗi kho cây vị trí 7 cấp với 19 ô (khu tiếp nhận, lưu trữ thường, lưu trữ lạnh, cách ly, xuất); 4 nhóm hàng, 15 sản phẩm, 50 biến thể; 3 nhà cung cấp, 2 nơi nhận; mức tồn tối thiểu, tối đa cho vài hàng.
- 3 tháng lịch sử xuất bán ở cả hai kho (G3 và báo cáo có số liệu).
- Đơn vị quy đổi có mã vạch thùng:

| Hàng | Đơn vị chính | Quy đổi | Mã vạch hàng | Mã vạch thùng |
| --- | --- | --- | --- | --- |
| SUATUOI-01 Sữa tươi 180ml không đường | hộp | 1 thùng = 48 hộp | 8930000000035 | 8930099000011 |
| NUOCSUOI-01 Nước suối 500ml | chai | 1 thùng = 24 chai | 8930000000045 | 8930099000028 |
| MIGOI-01 Mì gói tôm chua cay | gói | 1 thùng = 30 gói | 8930000000042 | 8930099000035 |

  Mã vạch khác hay dùng: AOTHUN-01 `8930000000001`, AOTHUN-03 `8930000000003`, BUTBI-01 `8930000000047`, GIAYA4-01 `8930000000048`.
- Tình huống dựng sẵn ở K01 (hóa đơn nhập PN000005, ghi chú "Dữ liệu demo"):
  - Lô L000010: 96 hộp sữa, còn 10 ngày hết hạn (cảnh báo sắp hết hạn, G1 chọn trước).
  - Lô L000011: 12 hộp sữa đã hết hạn 3 ngày (cảnh báo quá hạn, không giữ, không xuất được).
  - Lô L000014: 8 áo thun AOTHUN-03 còn ở khu tiếp nhận, chờ xếp (chưa tính vào tồn khả dụng).
  - Lô L000015: 10 áo thun AOTHUN-02 chất lượng Lỗi ở khu cách ly.
  - DTA-01 điện thoại chỉ còn 3 chiếc, dưới mức tối thiểu 5.
- Chứng từ đang chờ xử lý:
  - DM000001: đơn mua K01 từ NCC03 (4 thùng nước suối, 200 bút bi), chưa nhận hàng.
  - DD000003: đơn đặt hàng K01 (1 thùng sữa, 5 áo thun), chờ duyệt.
  - DD000004: đơn đặt hàng K02 (1 thùng mì), chờ duyệt.
  - DC000001: chuyển 2 ram giấy từ K01 sang K02, đang vận chuyển, chờ K02 nhận.
- Tệp Excel thử ở `quanlykho.Tests/TepMau/`: `DanhMuc_300dong.xlsx` (hợp lệ), `DanhMuc_300dong_5loi.xlsx` (5 dòng lỗi), `TonDauKy_K01_300dong.xlsx`, `TonDauKy_K01_300dong_vuotsucchua.xlsx`.

Các mã chứng từ, mã lô ở trên chỉ đúng khi seed trên CSDL trống. Nếu dùng CSDL đang có dữ liệu, tìm chứng từ có ghi chú "Dữ liệu demo".

## 1. Tổng quan và cảnh báo (quản lý kho)

Đăng nhập `ql.k01@kho.test`.

1. Trang Tổng quan có 4 thẻ: tồn dưới mức tối thiểu (AOTHUN-03, DTA-01, GIAYA4-01), vượt mức tối đa, sắp hết hạn (lô L000010), quá hạn (lô L000011).
2. Đổi "số ngày cảnh báo trước hạn dùng" (ví dụ 5) rồi bấm Áp dụng: lô còn 10 ngày biến khỏi thẻ sắp hết hạn. Bấm Làm mới trên từng thẻ.
3. Chỉ ra: menu chỉ có kho K01, không thấy K02 (quyền theo kho).

## 2. Nhập kho từ đơn mua, quét mã thùng (thủ kho + quản lý kho)

1. `ql.k01`: Nhập kho → Đơn mua → mở DM000001 (trạng thái Chưa thực hiện, có đơn giá, thành tiền). Có thể demo thêm: Lập đơn mua mới, chọn đơn vị "thùng" ở ô đơn vị, Lưu đơn, Xác nhận đơn.
2. Đăng nhập `tk1.k01@kho.test` (khung máy tính bảng): Hóa đơn nhập → Lập hóa đơn nhập → chọn đơn DM000001 → Lập và bắt đầu nhận hàng.
3. Ô quét mã tự nhận con trỏ. Gõ (giả máy quét) `8930099000028` rồi Enter, hoặc bấm nút Camera và đưa mã vạch (in ra hoặc hiện trên điện thoại khác) trước webcam: hệ thống nhận ra nước suối, đơn vị thùng. Nhập số 4 → Thêm dòng. Dòng ghi "4 thùng = 96 chai".
4. Quét `8930000000047` (bút bi), số 250 → báo lỗi vượt số chưa nhận ("còn 200 cây"). Sửa 200 → Thêm dòng.
5. Chỉ ra: thủ kho không thấy đơn giá. Thủ kho không có nút xác nhận.
6. `ql.k01` mở hóa đơn vừa lập → Xác nhận. Hàng vào ô khu tiếp nhận, đơn mua chuyển Hoàn thành. Bấm In nhãn lô để xem nhãn có mã lô.
7. Chỉ ra: mỗi lần ghi tồn mang một khóa chống ghi lặp; bấm hai lần không ghi tồn hai lần.

## 3. Xếp hàng vào ô, kiểm tra sức chứa (thủ kho)

`tk1.k01`: Nhập kho → Xếp hàng.

1. Danh sách hàng chờ xếp ở khu tiếp nhận: lô AOTHUN-03 (L000014) và các lô vừa nhận ở phần 2.
2. Bấm Chọn ở lô nước suối → hệ thống liệt kê ô phù hợp (vừa kích thước có xoay, tải trọng, cùng điều kiện bảo quản). Quét hoặc chọn ô → nhập số → Xếp vào ô.
3. Demo lỗi sức chứa: chọn một ô nhỏ trong khu lạnh và nhập số lớn → báo 422 kèm "tối đa còn xếp được X".
4. Xếp lô AOTHUN-03 vào ô khu lưu trữ thường. Về Tổng quan: AOTHUN-03 vẫn dưới tối thiểu nhưng tồn khả dụng giờ là 8 (trước là 0 vì hàng ở khu tiếp nhận không tính).

Chuyển vị trí trong kho: Kho → Chuyển vị trí, quét ô đang chứa hàng, chọn lô, quét ô đích, nhập số → Chuyển sang ô.

## 4. Tồn khả dụng và đơn đặt hàng (nhân viên bán hàng)

Đăng nhập `banhang@kho.test` ở khung điện thoại.

1. Xuất kho → Tồn khả dụng, tìm `SUATUOI`: K01 hiện 166 hộp khả dụng, đã trừ lô hết hạn L000011 (12 hộp) và hàng đang giữ; có cột quy ra thùng ("3 thùng 22 hộp"). Bảng xếp thành thẻ trên màn hình hẹp.
2. Đơn đặt hàng: thấy DD000003, DD000004 ở trạng thái Chờ duyệt (chỉ thấy đơn mình tạo).
3. Tạo đơn mới: kho K01, nơi nhận NN01, dòng AOTHUN-01 số 2000 → gửi duyệt báo thiếu hàng dạng "Cần 2.000, còn ...". Sửa 3 → gửi duyệt.
4. Chỉ ra: menu không có nhập kho, kiểm kê, báo cáo; gõ thẳng `/PhieuNhap` trên thanh địa chỉ → bị chặn (403).

## 5. Duyệt, giữ hàng theo G1 (quản lý kho)

`ql.k01`: Đơn đặt hàng → DD000003 → Duyệt và giữ hàng → Đồng ý.

1. Bảng "Hàng đã giữ": sữa giữ 48 hộp ở lô L000010 (hết hạn sớm nhất còn dùng được); lô L000011 đã hết hạn bị bỏ qua. Dòng chú thích nói lô, ô do G1 chọn (FEFO, rồi FIFO, ít kệ).
2. Mở lại Tồn khả dụng: số khả dụng của sữa giảm 48, từ 166 còn 118.
3. Chọn thủ kho `Thủ kho Hà Nội 1` → Lập lệnh xuất.

## 6. Lấy hàng bằng quét mã, kiểm tra chéo (thủ kho lấy, quản lý kho kiểm)

1. `tk1.k01` (máy tính bảng): Xuất kho → Lệnh xuất → mở lệnh vừa lập. Danh sách lấy hàng sắp theo thứ tự ghé ô.
2. Quét mã ô rồi mã lô theo từng dòng (gõ mã như trên danh sách rồi Enter). Demo lỗi: quét lô L000011 (hết hạn) → bị chặn; quét sai hàng → bị chặn; nhập số vượt yêu cầu → bị chặn.
3. Lấy đủ → Lấy xong, chờ kiểm tra.
4. Chỉ ra: thủ kho không có nút kiểm tra; bước này của quản lý kho, và người kiểm phải khác người lấy (một quản lý kho tự lấy hàng thì cũng không tự kiểm được).
5. `ql.k01` mở lệnh → Đúng, chờ xác nhận (hoặc Sai, trả lại lấy hàng kèm ghi chú dòng sai).
6. `ql.k01` → Xác nhận xuất kho. Sinh phiếu xuất Hoàn tất, đơn đặt chuyển Hoàn thành; nhân viên bán hàng thấy trạng thái mới.

Xuất khác (không qua đơn): Xuất kho → Phiếu xuất → Xuất khác, trả nhà cung cấp, chọn lô, ô từ tồn khả dụng, có thể chọn đơn vị thùng.

## 7. Chuyển kho hai bước (K01 → K02)

1. `ql.k01`: Kho → Chuyển kho → DC000001 trạng thái Chờ nhập kho: tồn đã trừ ở K01, hàng ở trạng thái đang vận chuyển. Có thể lập thêm phiếu mới: Lập phiếu điều chuyển → chọn lô, ô → Lưu, xuất kho đi.
2. Đăng nhập `tk.k02@kho.test`: menu Chuyển kho có số đếm phiếu chờ nhận. Mở DC000001 → nhập số thực nhận 1 (thiếu 1) → Xác nhận nhận hàng. Phiếu thành Nhận một phần, sinh dòng chênh lệch.
3. `ql.k02@kho.test`: mở phiếu → xử lý chênh lệch (ví dụ ghi giảm hao hụt, nhập lý do) → Xử lý. Phiếu Hoàn thành.
4. `tk.k02`: bấm Xếp hàng vào kệ để đưa hàng từ khu tiếp nhận K02 vào ô.
5. Chỉ ra: tổng tồn toàn hệ thống trước và sau không đổi (báo cáo tổng hợp tồn có cột đang vận chuyển).

## 8. Kiểm kê và điều chỉnh tồn

1. `ql.k01`: Kho → Kiểm kê → Lập phiếu kiểm kê, chọn kho K01 và ô khu lạnh đang có sữa → Lập phiếu → Bắt đầu kiểm kê. Các ô được khóa.
2. Trong lúc kiểm: thử xếp hàng vào ô đó hoặc lấy hàng từ ô đó → bị chặn (ô đang kiểm kê).
3. `tk1.k01`: mở phiếu, nhập số đếm từng lô. Có thể đếm theo thùng và lẻ: 1 thùng 0 hộp cho lô 48 hộp. Cố ý đếm thiếu một lô → Lưu số đếm.
4. Dòng lô L000011 (hết hạn): phân loại "hết hạn".
5. `ql.k01`: với mỗi chênh lệch chọn loại (thiếu, thừa, hư hỏng, hết hạn, sai vị trí, sai biến thể), nhập lý do → Duyệt điều chỉnh. Hệ thống sinh chứng từ điều chỉnh, không sửa thẳng số tồn → Hoàn thành kiểm kê.

## 9. Đề xuất nhập bổ sung G3

`ql.k01`: Nhập kho → Đề xuất nhập bổ sung.

1. Kho K01, phần "Cần nhập" có 1 dòng: GIAYA4-01, đề xuất 36 ram. Dòng lý do ghi đủ cơ sở tính: A = 7, d = 109/90 ngày ≈ 1,2 ram/ngày, L = 5, m = 10, điểm đặt hàng ≈ 16. Số đề xuất = d × (5 + 30) − 7 − 0, làm tròn lên.
2. Phần "Không đề xuất": sữa (166 hộp, trên điểm đặt hàng ≈ 75 nhờ các lô demo vừa nhập), áo thun AOTHUN-01 đủ hàng; AOTHUN-03, DTA-01 "chưa có lịch sử xuất bán" nên không đề xuất dù dưới tối thiểu. Đổi sang K02: giấy, mì đều đủ hàng.
3. Tích dòng giấy, chọn nhà cung cấp NCC03, có thể sửa số (ví dụ 40) → Tạo đơn mua nháp từ dòng đã chọn: sinh đơn mua Nháp; AI không tự xác nhận. Cách khác: tích dòng, ghi lý do → Bỏ qua (không tạo đơn, vẫn lưu để thống kê).
4. Bấm Tính lại: giấy chuyển xuống phần "Không đề xuất" với lý do "đã có 40 đang chờ nhập", vì số trên đơn nháp đã tính vào số đang chờ nhập.

## 10. Báo cáo (quản lý kho)

Kho → Báo cáo tồn:

- Tổng hợp tồn: theo kho, tách khu tiếp nhận, cách ly, đang vận chuyển, đã giữ, khả dụng; số quy ra thùng.
- Tồn theo lô (hạn dùng), tồn theo vị trí (theo ô).
- Nhập – xuất – tồn theo kỳ (chọn từ ngày, đến ngày).
- Thẻ kho: chọn SUATUOI-01 → sổ biến động với số dư lũy kế, số dư cuối bằng tồn hiện tại.
- Tỷ lệ chấp nhận gợi ý AI: số lần gợi ý G1, G3 và tỷ lệ người dùng làm theo (sau phần 5 và 9 đã có số).

## 11. Quản trị viên

Đăng nhập `admin@kho.test`.

1. **Danh mục cấu hình trên giao diện:** Danh mục → Nhóm hàng → thêm nhóm "GD – Đồ gia dụng", thuộc tính "Công suất" (1000W, 1500W), "Bảo hành" (12 tháng). Hàng hóa → thêm sản phẩm "Nồi cơm điện" nhóm GD → Sinh biến thể: ra 2 biến thể. Mở một biến thể, khai đơn vị quy đổi (ví dụ 1 thùng = 4 chiếc, có mã vạch). Không phải sửa mã nguồn.
2. **Kho và vị trí:** Danh mục kho (thêm kho mới), Kho và vị trí (cây 7 cấp, thêm ô có kích thước, tải trọng). Ngừng sử dụng thay vì xóa với dữ liệu đã có chứng từ.
3. **Người dùng:** tạo tài khoản thủ kho mới, gán kho K02. Khóa tài khoản `tk2.k01` trong lúc người đó đang đăng nhập ở trình duyệt kia → thao tác kế tiếp bị đăng xuất (401). Thử khóa quản trị viên cuối cùng → bị chặn.
4. **Nhập Excel danh mục:** Danh mục → Nhập Excel → chọn `DanhMuc_300dong_5loi.xlsx` → Kiểm tra: báo đúng 5 dòng lỗi, không ghi gì. Chọn `DanhMuc_300dong.xlsx` → Kiểm tra → Ghi vào hệ thống. Chọn lại tệp đó → bị chặn vì đã nhập.
5. **Nhập tồn đầu kỳ:** Nhập kho → Nhập tồn đầu kỳ, kho K01. Tệp `TonDauKy_K01_300dong_vuotsucchua.xlsx` báo dòng vượt sức chứa ô. Tệp `TonDauKy_K01_300dong.xlsx` → Kiểm tra và sinh phiếu: ra hóa đơn nhập Lưu tạm loại tồn đầu kỳ, quản lý kho xác nhận mới ghi tồn. Tệp rải áo thun và sữa vào 16 ô lưu trữ K01; đã thử chạy được trên dữ liệu demo.
6. **Nhật ký:** Hệ thống → Nhật ký, lọc theo người, kho, loại thao tác: thấy duyệt đơn, xác nhận nhập, xuất, điều chỉnh, khóa tài khoản vừa làm. Nhật ký chỉ thêm, CSDL chặn sửa, xóa.
7. **Styleguide:** Hệ thống → Styleguide, các thành phần giao diện dùng chung.

## 12. Hủy chứng từ bằng bút toán đảo (tùy chọn)

`ql.k01`: mở hóa đơn nhập ở phần 2 khi hàng còn ở khu tiếp nhận → Hủy hóa đơn (bút toán đảo). Thẻ kho hiện thêm dòng âm, không xóa dòng cũ. Hóa đơn đã xếp hàng đi thì không hủy được.

## Điểm nên nhấn mạnh khi trình bày

- Mọi thay đổi tồn đi qua một cổng ghi tồn, có khóa chống ghi lặp; thẻ kho luôn khớp tồn.
- Quyền theo vai và theo kho kiểm ở máy chủ (gõ thẳng URL vẫn bị chặn), không chỉ ẩn nút.
- Tồn khả dụng loại trừ khu tiếp nhận, cách ly, lô hết hạn, đang vận chuyển, đã giữ.
- AI (G1, G3) chỉ gợi ý; người dùng quyết định; có thống kê tỷ lệ chấp nhận.
- Quy đổi đơn vị một tầng: nhập, xuất, kiểm kê theo thùng, sổ sách theo đơn vị chính.
