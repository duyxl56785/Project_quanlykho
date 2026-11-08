# Hướng dẫn sử dụng theo từng tài khoản

Tài liệu này đi lần lượt từng tài khoản thử: đăng nhập, menu bên trái có những mục nào, mỗi mục dùng để làm gì và một tình huống thực tế để hình dung. Muốn trình diễn theo kịch bản liền mạch thì xem [HUONG_DAN_DEMO.md](HUONG_DAN_DEMO.md).

## Trước khi bắt đầu

1. Bật CSDL và nạp dữ liệu thử (một lần):

   ```bash
   docker compose up -d
   dotnet run --project quanlykho -- --seed
   ```

2. Chạy ứng dụng rồi mở http://localhost:5166:

   ```bash
   dotnet run --project quanlykho
   ```

3. Trang đăng nhập hỏi Email và Mật khẩu. Mọi tài khoản thử dùng chung một mật khẩu, ghi ở khóa `DuLieuThu:MatKhau` trong `quanlykho/appsettings.Development.json`.
4. Đăng xuất: nút Đăng xuất ở góc trên bên phải. Muốn mở hai tài khoản cùng lúc, dùng thêm một cửa sổ ẩn danh.

### Doanh nghiệp trong dữ liệu thử

Một công ty có hai kho:

- **K01 – Kho Hà Nội** (Số 1 Cầu Giấy)
- **K02 – Kho Bắc Ninh** (KCN Tiên Sơn)

Mỗi kho được chia thành các khu:

| Khu | Dùng để |
| --- | --- |
| Khu tiếp nhận | Hàng mới về đặt tạm ở đây |
| Khu lưu trữ thường | Cất quần áo, điện thoại, văn phòng phẩm |
| Khu lưu trữ lạnh | Cất sữa, sữa chua |
| Khu cách ly | Giữ hàng lỗi |
| Khu xuất | Hàng chuẩn bị giao đi |

Mỗi ô có mã 7 cấp, ví dụ `K01-TA01-TG01-P02-H01-KE01-O03`: kho K01, tòa 1, tầng 1, phòng (khu) 2, hàng 1, kệ 1, ô 3.

Hàng hóa chia thành 4 nhóm: Thời trang, Điện tử, Thực phẩm, Văn phòng phẩm. Hàng giao cho khách đi tới hai nơi nhận là Cửa hàng Cầu Giấy và Đại lý Từ Sơn.

### Ai thấy mục nào trên menu

Menu chỉ hiện những mục tài khoản đó được mở. Máy chủ vẫn kiểm tra quyền khi gửi yêu cầu, nên gõ thẳng địa chỉ của một mục bị ẩn cũng bị chặn.

| Mục menu | Quản trị viên | Quản lý kho | Thủ kho | Bán hàng |
| --- | --- | --- | --- | --- |
| Tổng quan | cảnh báo | cảnh báo | trống | trống |
| Đơn mua | toàn quyền | toàn quyền | xem | – |
| Hóa đơn nhập | toàn quyền | toàn quyền | lập, nhận hàng | – |
| Xếp hàng | ✓ | ✓ | ✓ | – |
| Đề xuất nhập bổ sung | ✓ | ✓ | – | – |
| Nhập tồn đầu kỳ | ✓ | ✓ | – | – |
| Tồn khả dụng | ✓ | ✓ | ✓ | ✓ |
| Đơn đặt hàng | tạo, duyệt | duyệt | – | tạo |
| Lệnh xuất | toàn quyền | toàn quyền | lấy hàng | – |
| Phiếu xuất | toàn quyền | toàn quyền | xem | – |
| Chuyển kho | toàn quyền | toàn quyền | nhận hàng | – |
| Chuyển vị trí | ✓ | ✓ | ✓ | – |
| Kiểm kê | toàn quyền | toàn quyền | đếm | – |
| Báo cáo tồn | ✓ | ✓ | ✓ | – |
| Hàng hóa, Nhóm hàng | sửa | xem | xem | xem |
| Danh mục kho | ✓ | – | – | – |
| Kho và vị trí | sửa | sửa (kho mình) | xem | – |
| Nhà cung cấp, nơi nhận | sửa | xem | xem | xem |
| Nhập Excel (danh mục) | ✓ | – | – | – |
| Người dùng, Nhật ký, Styleguide | ✓ | – | – | – |

Quản lý kho và thủ kho chỉ thấy dữ liệu của kho được giao. Quản trị viên và nhân viên bán hàng thấy mọi kho.

---

## 1. Nhân viên bán hàng – `banhang@kho.test`

**Là ai:** nhân viên kinh doanh. Không làm việc trong kho, cần biết còn bao nhiêu hàng để hứa với khách, rồi đặt hàng cho kho giao.

Sau khi đăng nhập, trang **Tổng quan** chỉ có dòng "Chọn chức năng ở menu bên trái". Cảnh báo tồn kho là việc của quản lý.

### Tồn khả dụng

- **Làm gì:** tra số hàng còn bán được ở từng kho.
- **Cách tính:** số khả dụng đã trừ các phần không giao được ngay:
  - hàng mới về còn ở khu tiếp nhận,
  - hàng lỗi ở khu cách ly,
  - lô đã hết hạn,
  - hàng đang chở giữa hai kho,
  - hàng đã giữ cho đơn khác.
- **Cách dùng:** gõ mã hoặc tên hàng (ví dụ `SUATUOI`), chọn kho hoặc để tất cả.
- Số lượng hiện kèm cách quy ra thùng, ví dụ "166 hộp = 3 thùng 22 hộp".
- **Tình huống:** Cửa hàng Cầu Giấy gọi hỏi "Mai giao được 2 thùng sữa tươi không?". Nhân viên tra `SUATUOI-01` thấy K01 còn hơn 3 thùng khả dụng, trả lời được ngay. Trong kho thực tế có thêm 12 hộp đã hết hạn, nhưng phần đó không được tính.

### Đơn đặt hàng

- **Làm gì:** tạo đơn yêu cầu kho giao hàng cho một nơi nhận, rồi theo dõi trạng thái đơn.
- **Cách dùng:**
  1. Bấm Tạo đơn, chọn kho xuất, nơi nhận, hạn giao.
  2. Thêm dòng hàng. Có thể đặt theo thùng.
  3. Lưu nháp hoặc gửi duyệt ngay.
- Gửi duyệt mà kho không đủ hàng thì hệ thống báo dạng "Cần X, còn Y".
- **Trạng thái đơn:**

  | Trạng thái | Ý nghĩa |
  | --- | --- |
  | Nháp | Chưa gửi, còn sửa được |
  | Chờ duyệt | Đã gửi, chờ quản lý kho |
  | Đã duyệt | Kho đã giữ hàng cho đơn |
  | Đang xuất | Thủ kho đang lấy hàng |
  | Hoàn thành | Đã xuất kho |
  | Đã hủy | Nhân viên tự hủy được khi đơn chưa xuất |

- Nhân viên chỉ thấy đơn của chính mình.
- **Tình huống:**
  1. Đại lý Từ Sơn cần 1 thùng mì gói giao trong 3 ngày.
  2. Nhân viên tạo đơn từ kho K02, dòng `MIGOI-01` 1 thùng, rồi gửi duyệt. Đơn mẫu DD000004 chính là đơn này.
  3. Chiều hôm đó đại lý hỏi lại. Nhân viên mở danh sách, thấy đơn đã sang "Đã duyệt", tức là kho đã giữ hàng.

### Hàng hóa, Nhóm hàng, Nhà cung cấp, nơi nhận (chỉ xem)

- **Làm gì:** tra cứu danh mục: tên hàng, biến thể (size, màu, dung lượng...), đơn vị tính, mã vạch, nơi nhận.
- **Tình huống:** khách hỏi áo thun có những size nào. Nhân viên mở Hàng hóa → `AOTHUN` để xem các biến thể S, M, L, XL, trắng, đen.

---

## 2. Thủ kho – `tk1.k01@kho.test` (K01)

Các tài khoản thủ kho khác: `tk2.k01@kho.test` (K01) và `tk.k02@kho.test` (K02).

**Là ai:** người trực tiếp nhận hàng, cất hàng, lấy hàng và đếm hàng trong kho. Thường cầm máy tính bảng và máy quét mã vạch.

**Ô quét mã:** các màn hình làm việc đều có một ô quét mã tự nhận con trỏ. Có ba cách đưa mã vào:

- **Máy quét USB hoặc Bluetooth:** bóp cò, máy tự gõ mã rồi nhấn Enter.
- **Camera:** bấm nút **Camera** (biểu tượng máy ảnh) cạnh ô. Lần đầu trình duyệt hỏi quyền dùng camera, bấm Cho phép. Đưa mã vạch vào giữa khung và giữ yên. Đọc được mã thì hộp tự đóng, kết quả như vừa quét bằng máy. Máy tính bảng và điện thoại tự dùng camera sau.
- **Gõ tay:** gõ mã rồi nhấn Enter.

Camera đọc được mã vạch trên hàng, mã vạch thùng, và nhãn ô, nhãn lô in từ hệ thống. Trình duyệt chỉ cho mở camera khi trang mở bằng `https://` hoặc `localhost`. Ví dụ, mở bằng `http://192.168.x.x` trên điện thoại thì nút Camera báo không dùng được.

Trang **Tổng quan** trống, cảnh báo dành cho quản lý.

### Đơn mua (chỉ xem)

- **Làm gì:** xem kho sắp nhận hàng gì, của nhà cung cấp nào, đã nhận được bao nhiêu.
- Thủ kho không thấy đơn giá.
- **Tình huống:** đầu ca, thủ kho mở Đơn mua thấy DM000001 của NCC03, gồm 4 thùng nước suối và 200 bút bi, để biết sáng nay xe nào sẽ tới.

### Hóa đơn nhập

- **Làm gì:** ghi nhận hàng thực tế nhận được từ xe giao.
- **Cách dùng:**
  1. Bấm Lập hóa đơn nhập, chọn đơn mua (hoặc nhập khác), rồi Lập và bắt đầu nhận hàng.
  2. Quét mã từng món và nhập số lượng. Quét mã thùng thì hệ thống tự hiểu đơn vị là thùng.
  3. Chọn chất lượng: Đạt hoặc Lỗi.
- Hàng có hạn dùng thì bắt buộc nhập hạn dùng.
- Nhận vượt số còn lại trên đơn mua thì bị chặn.
- Thủ kho chỉ lập và ghi dòng. Quản lý kho xác nhận thì hàng mới vào tồn: hàng Đạt vào khu tiếp nhận, hàng Lỗi vào khu cách ly.
- Bấm In nhãn lô để in nhãn dán lên hàng.
- **Tình huống:**
  1. Xe NCC03 tới.
  2. Thủ kho lập hóa đơn nhập từ DM000001 và quét mã thùng nước suối `8930099000028`. Màn hình hiện "nước suối, thùng 24 chai". Nhập 4.
  3. Quét bút bi `8930000000047`, gõ nhầm 250. Hệ thống báo chỉ còn 200 cây chưa nhận. Sửa lại 200.
  4. Báo quản lý kho xác nhận.

### Xếp hàng

- **Làm gì:** chuyển hàng từ khu tiếp nhận lên ô cất trữ.
- **Cách dùng:**
  1. Bấm Chọn ở lô cần xếp. Hệ thống liệt kê các ô phù hợp: vừa kích thước (có tính xoay thùng), không quá tải trọng, đúng điều kiện bảo quản (hàng lạnh vào khu lạnh).
  2. Quét mã ô, nhập số lượng, bấm Xếp vào ô.
- Ô không đủ chỗ thì hệ thống báo "tối đa còn xếp được X".
- Ô đang kiểm kê thì không xếp vào được.
- **Tình huống:** 8 áo thun `AOTHUN-03` vẫn nằm ở khu tiếp nhận (lô L000014). Trong lúc đó tồn khả dụng của mặt hàng này là 0, nên bán hàng chưa bán được. Thủ kho xếp vào một ô khu lưu trữ thường. Tồn khả dụng lên 8.

### Tồn khả dụng

Giống phần của nhân viên bán hàng, nhưng chỉ thấy kho mình. Thủ kho dùng để kiểm nhanh xem một mặt hàng còn bán được bao nhiêu.

### Lệnh xuất

- **Làm gì:** lấy hàng giao theo đơn đặt.
- **Cách dùng:**
  1. Mở lệnh được giao. Danh sách lấy hàng đã chỉ sẵn lô nào, ô nào, bao nhiêu, sắp theo thứ tự đi trong kho.
  2. Quét mã ô rồi mã lô cho từng dòng, nhập số đã lấy.
  3. Lấy đủ thì bấm Lấy xong, chờ kiểm tra.
- Quét sai hàng, quét lô hết hạn hoặc lấy quá số yêu cầu đều bị chặn.
- Sau bước này, quản lý kho kiểm tra hàng. Người kiểm tra phải khác người đã lấy.
- **Tình huống:**
  1. Lệnh xuất cho đơn DD000003 yêu cầu 1 thùng sữa và 5 áo thun.
  2. Thủ kho đi tới ô lạnh, quét ô rồi quét lô L000010 (lô sắp hết hạn, hệ thống chọn xuất trước), lấy 48 hộp.
  3. Ngay cạnh đó là lô L000011 đã hết hạn. Nếu quét nhầm lô này, máy báo lỗi ngay.

### Phiếu xuất (chỉ xem)

- **Làm gì:** xem các lần xuất kho đã ghi sổ: xuất theo đơn và xuất khác.

### Chuyển kho

- **Làm gì:** với thủ kho, nhận hàng từ kho khác chuyển tới.
- **Cách dùng:**
  1. Mục menu có số đếm phiếu đang chờ nhận.
  2. Mở phiếu, nhập số thực nhận từng dòng, bấm Xác nhận nhận hàng.
  3. Bấm Xếp hàng vào kệ để cất hàng vừa nhận.
- Nhận thiếu thì phiếu thành "Nhận một phần". Quản lý kho xử lý phần chênh lệch.
- **Tình huống:**
  1. Đăng nhập `tk.k02@kho.test`. Menu Chuyển kho báo 1 phiếu chờ: DC000001, 2 ram giấy từ Hà Nội.
  2. Xe tới nhưng chỉ có 1 ram, ram còn lại bị ướt và đã bỏ dọc đường.
  3. Thủ kho nhập thực nhận 1 rồi xác nhận. Phần thiếu chờ quản lý kho K02 xử lý.

### Chuyển vị trí

- **Làm gì:** dời hàng từ ô này sang ô khác trong cùng kho.
- **Cách dùng:** quét ô đang chứa hàng, chọn lô, quét ô đích, nhập số, bấm Chuyển sang ô.
- **Tình huống:** kệ 1 sắp đầy, hàng bán chạy cần đưa ra kệ gần cửa. Thủ kho chuyển 20 áo thun sang ô kệ 2.

### Kiểm kê

- **Làm gì:** với thủ kho, đếm thực tế các ô trong phiếu kiểm kê do quản lý kho lập.
- **Cách dùng:**
  1. Mở phiếu, nhập số đếm từng lô. Với hàng có đơn vị thùng thì nhập số thùng và số lẻ.
  2. Thấy hàng lạ không có trong danh sách thì thêm dòng.
- Trong thời gian kiểm, các ô đó bị khóa: không nhập, xuất, xếp hay chuyển được.
- **Tình huống:** cuối tháng, quản lý lập phiếu kiểm ô lạnh. Thủ kho đếm lô sữa 96 hộp được 2 thùng 0 hộp, khớp sổ. Lô hết hạn đếm được 10 hộp, thiếu 2 so với sổ. Thủ kho lưu số đếm, phần chênh lệch để quản lý duyệt.

### Báo cáo tồn

Xem các báo cáo tồn của kho mình (chi tiết ở phần Quản lý kho).

### Hàng hóa, Nhóm hàng, Kho và vị trí, Nhà cung cấp (chỉ xem)

- **Kho và vị trí:** cây 7 cấp của kho. Bấm vào một ô để xem trong ô đang có hàng gì. Có thể in nhãn mã ô để dán lên kệ.
- **Tình huống:** thủ kho mới chưa thuộc kho, mở Kho và vị trí để biết khu lạnh nằm ở phòng 3.

---

## 3. Quản lý kho – `ql.k01@kho.test` (K01)

Tài khoản quản lý kho còn lại: `ql.k02@kho.test` (K02).

**Là ai:** người chịu trách nhiệm một kho. Quản lý kho đặt mua hàng, xác nhận nhập xuất, duyệt đơn và duyệt chênh lệch, theo dõi cảnh báo. Quản lý kho được xem giá, thủ kho thì không. Quản lý kho có mọi mục của thủ kho, cộng thêm các quyền dưới đây.

### Tổng quan

- **Làm gì:** bảng cảnh báo khi vừa đăng nhập, gồm 4 thẻ:
  - tồn dưới mức tối thiểu,
  - tồn vượt mức tối đa,
  - lô sắp hết hạn,
  - lô đã quá hạn.
- Ô "số ngày cảnh báo trước hạn dùng" đổi được, ví dụ 30 hay 7 ngày.
- **Tình huống:** sáng thứ Hai, quản lý K01 mở máy và thấy:
  - điện thoại DTA-01 chỉ còn 3 chiếc, dưới mức tối thiểu 5, nên cần đặt thêm;
  - lô sữa L000010 còn 10 ngày hết hạn, nên ưu tiên bán;
  - lô L000011 đã quá hạn, nên cần kiểm kê và loại bỏ.

### Đơn mua

- **Làm gì:** đặt hàng với nhà cung cấp, đóng vai trò lệnh nhập cho thủ kho.
- **Cách dùng:**
  1. Bấm Lập đơn mua, chọn nhà cung cấp, kho nhập.
  2. Thêm dòng hàng, số lượng, đơn giá. Có thể đặt theo thùng.
  3. Lưu nháp hoặc Xác nhận đơn để thủ kho nhận hàng theo đơn.
- **Trạng thái đơn:** Nháp → Chưa thực hiện → Đang thực hiện → Hoàn thành. Hệ thống tự chuyển trạng thái theo số hàng đã nhận.
- **Tình huống:** quản lý gọi NCC03 đặt 4 thùng nước suối, giá 96.000 đ/thùng, và 200 bút bi, rồi lập đơn DM000001 trên hệ thống để thủ kho biết sẽ nhận gì.

### Hóa đơn nhập

- **Làm gì:** ngoài những việc thủ kho làm được, quản lý kho còn:
  - **Xác nhận** để hàng vào tồn.
  - **Hủy** hóa đơn. Hóa đơn đã xác nhận thì hủy bằng bút toán đảo: sổ ghi thêm dòng trừ, không xóa dòng cũ. Chỉ hủy được khi hàng còn nguyên ở chỗ đã nhận vào.
  - Lập phiếu **nhập khác** (không theo đơn mua), có nhập đơn giá.
- **Tình huống:** thủ kho báo đã nhận xong xe NCC03. Quản lý mở hóa đơn, đối chiếu với giấy giao hàng rồi xác nhận. Đơn mua DM000001 tự chuyển sang Hoàn thành.

### Đề xuất nhập bổ sung (gợi ý G3)

- **Làm gì:** hệ thống tính mặt hàng nào sắp thiếu và nên đặt thêm bao nhiêu.
- **Cách tính:**
  - Hệ thống lấy lượng bán trung bình mỗi ngày trong 3 tháng gần nhất.
  - So với thời gian chờ hàng về và mức tồn tối thiểu.
  - Trừ đi số đang chờ nhập.
- Hàng có ít hơn 14 ngày lịch sử xuất thì không đề xuất.
- **Cách dùng:** tích các dòng muốn đặt rồi bấm "Tạo đơn mua nháp từ dòng đã chọn". Hệ thống tạo mỗi nhà cung cấp một đơn nháp.
- Hệ thống chỉ gợi ý; quản lý vẫn phải tự xác nhận đơn. Dòng không muốn đặt thì bấm Bỏ qua.
- **Tình huống:**
  - Màn hình K01 đề xuất mua 36 ram giấy A4. Giấy bán trung bình hơn 1 ram/ngày, chờ hàng 5 ngày, tối thiểu 10 ram, mà kho chỉ còn 7 ram.
  - Sữa tươi còn 166 hộp, đủ bán qua thời gian chờ hàng, nên nằm ở phần "Không đề xuất".
  - Quản lý tích dòng giấy, chọn nhà cung cấp, sửa số cho tròn 40 ram rồi tạo đơn nháp. Mở đơn nháp, kiểm tra rồi xác nhận gửi nhà cung cấp.

### Nhập tồn đầu kỳ

- **Làm gì:** khi mới đưa hệ thống vào dùng, nạp toàn bộ hàng đang có trong kho từ một tệp Excel thay vì gõ tay.
- **Cách dùng:**
  1. Tải tệp mẫu, điền lô, hạn dùng, ô, số lượng.
  2. Bấm Kiểm tra và sinh phiếu.
  3. Hệ thống báo lỗi đúng dòng (ví dụ ô không đủ sức chứa). Tệp hợp lệ sinh một hóa đơn nhập Lưu tạm để quản lý xem lại rồi xác nhận.
- Một tệp chỉ nhập được một lần.
- **Tình huống:** kho Bắc Ninh mới chuyển sang dùng phần mềm. Cuối ngày kiểm hàng xong, quản lý gửi tệp Excel 300 dòng. Hệ thống báo dòng 152 vượt tải trọng ô. Sửa dòng đó, nhập lại thì thành công.

### Đơn đặt hàng

- **Làm gì:** duyệt đơn của bán hàng.
- **Cách dùng:**
  1. Mở đơn Chờ duyệt, bấm Duyệt và giữ hàng.
  2. Hệ thống (gợi ý G1) chọn lô và ô để giữ: lô hết hạn sớm nhất trước, sau đó lô nhập trước, và ít phải đi nhiều kệ. Bảng "Hàng đã giữ" cho biết đã giữ lô nào, ở ô nào.
  3. Chọn thủ kho thực hiện, bấm Lập lệnh xuất.
- Đổi ý thì bấm Bỏ giữ.
- **Tình huống:** quản lý duyệt DD000003. Hệ thống giữ 48 hộp ở lô L000010, lô còn 10 ngày hết hạn, và tự bỏ qua lô đã hết hạn. Quản lý giao cho Thủ kho Hà Nội 1 lấy hàng.

### Lệnh xuất

- **Làm gì:** sau khi thủ kho báo lấy xong, quản lý kho làm hai bước:
  1. **Kiểm tra** hàng đã lấy. Đúng thì bấm "Đúng, chờ xác nhận". Sai thì bấm "Sai, trả lại lấy hàng" và ghi rõ dòng sai.
  2. **Xác nhận xuất kho.** Hệ thống trừ tồn, sinh phiếu xuất Hoàn tất và chuyển đơn đặt sang Hoàn thành.
- Người kiểm tra không được là người đã lấy hàng.
- **Tình huống:**
  1. Thủ kho báo lấy xong đơn DD000003.
  2. Quản lý ra khu xuất đếm lại. Thấy áo thun lấy nhầm màu, quản lý trả lại kèm ghi chú "dòng 2 sai màu".
  3. Thủ kho lấy lại đúng màu, báo lấy xong lần nữa. Quản lý kiểm đúng rồi xác nhận xuất.

### Phiếu xuất

- **Làm gì:**
  - Xem các phiếu xuất.
  - Lập phiếu **Xuất khác, trả nhà cung cấp** cho hàng xuất không qua đơn đặt: trả hàng lỗi, hàng mẫu trưng bày. Chọn lô và ô từ tồn khả dụng, có thể chọn đơn vị thùng.
- **Tình huống:** công ty lấy 4 chiếc điện thoại đi trưng bày ở hội chợ. Quản lý lập phiếu xuất khác, lý do "Trưng bày".

### Chuyển kho

- **Làm gì:**
  - Ở kho đi: lập phiếu điều chuyển, chọn lô và ô, bấm "Lưu, xuất kho đi". Tồn kho đi bị trừ, hàng chuyển sang trạng thái đang vận chuyển.
  - Ở kho đến: xử lý chênh lệch khi thủ kho nhận thiếu, ví dụ ghi giảm hao hụt có lý do.
  - Hủy phiếu khi kho đến chưa nhận.
- Trong lúc vận chuyển, tổng tồn toàn công ty không đổi.
- **Tình huống:**
  1. Bắc Ninh hết giấy, Hà Nội còn dư. `ql.k01` lập phiếu chuyển 2 ram sang K02 và đặt xe đi.
  2. Kho Bắc Ninh nhận 1 ram.
  3. `ql.k02` mở phiếu, chọn ghi giảm hao hụt 1 ram, lý do "ướt khi vận chuyển". Phiếu Hoàn thành.

### Chuyển vị trí, Xếp hàng

Giống phần của thủ kho.

### Kiểm kê

- **Làm gì:** đối chiếu số thực tế với sổ sách và sửa sổ cho đúng, có người duyệt.
- **Cách dùng:**
  1. Lập phiếu kiểm kê, chọn kho và các ô cần kiểm.
  2. Bấm Bắt đầu kiểm kê. Các ô đó bị khóa.
  3. Thủ kho đếm.
  4. Với mỗi dòng chênh lệch, chọn loại (thiếu, thừa, hư hỏng, hết hạn, sai vị trí, sai biến thể) và ghi lý do.
  5. Bấm Duyệt điều chỉnh. Hệ thống sinh chứng từ điều chỉnh, không sửa thẳng số tồn.
  6. Bấm Hoàn thành kiểm kê để mở khóa các ô.
- **Tình huống:**
  1. Kiểm ô lạnh cuối tháng.
  2. Lô hết hạn L000011 sổ ghi 12 hộp, đếm được 10. Quản lý phân loại "thiếu", lý do "hộp móp bị loại từ trước".
  3. Quản lý duyệt. Thẻ kho ghi thêm dòng điều chỉnh, ai duyệt và lúc nào đều được lưu lại.

### Báo cáo tồn

Gồm 6 báo cáo:

| Báo cáo | Cho biết |
| --- | --- |
| Tổng hợp tồn | Theo từng kho: tổng tồn, phần ở khu tiếp nhận, cách ly, đang vận chuyển, đã giữ, còn khả dụng; quy ra thùng |
| Tồn theo lô | Từng lô kèm hạn dùng |
| Tồn theo vị trí | Từng ô đang chứa gì |
| Nhập – xuất – tồn | Theo kỳ: tồn đầu, nhập, xuất, tồn cuối, giá trị |
| Thẻ kho | Mọi lần nhập xuất của một mặt hàng và số dư sau từng lần |
| Tỷ lệ chấp nhận gợi ý AI | Bao nhiêu lần người dùng làm theo gợi ý G1, G3 |

**Tình huống:** kế toán hỏi vì sao tháng này sữa hao 2 hộp. Quản lý mở Thẻ kho `SUATUOI-01`, chỉ ra dòng điều chỉnh kiểm kê ngày 30, người duyệt là quản lý K01.

### Kho và vị trí

- **Làm gì:** quản lý kho được thêm, sửa, khóa ô trong kho mình: kích thước trong, tải trọng, loại khu, điều kiện bảo quản.
- Ô đã từng chứa hàng thì không xóa được, chỉ khóa.
- **Tình huống:** kho lắp thêm một kệ lạnh. Quản lý thêm kệ và 4 ô với kích thước 1 × 0,8 × 1 m, tải trọng 200 kg. Từ đó màn hình Xếp hàng gợi ý được các ô này.

---

## 4. Quản trị viên – `admin@kho.test`

**Là ai:** người quản trị hệ thống cho cả công ty.

- Thấy mọi kho, làm được mọi việc của quản lý kho ở mọi kho.
- Tạo được đơn đặt hàng như bán hàng.
- Có thêm quyền cấu hình danh mục và tài khoản.

Phần này chỉ nói các mục riêng của quản trị viên. Các mục còn lại xem phần Quản lý kho.

### Hàng hóa, Nhóm hàng

- **Làm gì:** cấu hình danh mục ngay trên giao diện, không cần sửa mã nguồn.
- **Nhóm hàng:** thêm nhóm và các thuộc tính của nhóm, ví dụ Size, Màu, Dung lượng.
- **Hàng hóa:**
  1. Thêm sản phẩm: tên, đơn vị tính, nhóm, điều kiện bảo quản, có theo dõi hạn dùng hay không.
  2. Bấm **Sinh biến thể**. Hệ thống tạo mọi tổ hợp giá trị thuộc tính.
  3. Mở từng biến thể để nhập kích thước đóng gói, khối lượng, mã vạch và **đơn vị quy đổi** (ví dụ 1 thùng = 48 hộp, kèm mã vạch thùng).
- Hàng đã có chứng từ thì không xóa được, chỉ ngừng sử dụng.
- **Tình huống:**
  1. Công ty mở thêm ngành đồ gia dụng.
  2. Quản trị viên tạo nhóm "GD – Đồ gia dụng" với thuộc tính Công suất (1000W, 1500W) và Bảo hành (12 tháng).
  3. Thêm sản phẩm "Nồi cơm điện", bấm sinh biến thể, được 2 mã NOICOM-01 và NOICOM-02.
  4. Ngay chiều đó kho đã nhập được nồi cơm.

### Danh mục kho

- **Làm gì:**
  - Thêm kho mới.
  - Sửa thông tin kho.
  - Khóa kho.
  - Đặt ô mặc định cho khu tiếp nhận và khu cách ly của kho.
- **Tình huống:** công ty thuê thêm kho ở Hải Phòng. Quản trị viên thêm kho K03, sau đó quản lý kho K03 tự dựng cây vị trí trong Kho và vị trí.

### Kho và vị trí

Giống quản lý kho, nhưng làm được trên mọi kho.

### Nhà cung cấp, nơi nhận

- **Làm gì:** thêm, sửa, ngừng sử dụng nhà cung cấp và nơi nhận hàng.
- **Tình huống:** có đại lý mới ở Bắc Giang. Quản trị viên thêm nơi nhận NN03 để bán hàng chọn được khi đặt đơn.

### Nhập Excel (danh mục)

- **Làm gì:** nạp hàng loạt sản phẩm và biến thể từ Excel.
- **Cách dùng:**
  1. Tải tệp mẫu, điền dữ liệu, bấm Kiểm tra.
  2. Có lỗi thì hệ thống báo đúng dòng, đúng cột và không ghi gì.
  3. Hết lỗi thì bấm Ghi vào hệ thống.
- Một tệp đã ghi thì không nhập lại được, tránh nhân đôi.
- **Tình huống:** phòng mua hàng gửi danh sách 300 mặt hàng mới. Kiểm tra lần đầu báo 5 dòng lỗi (thiếu đơn vị, trùng mã...). Sửa xong ghi một lần là đủ.
- Tệp thử nằm ở `quanlykho.Tests/TepMau/`.

### Người dùng

- **Làm gì:**
  - Tạo tài khoản và chọn vai.
  - Gán kho cho quản lý kho và thủ kho.
  - Đổi vai, đổi kho.
  - Khóa và mở khóa tài khoản.
  - Đặt lại mật khẩu.
- Khóa tài khoản thì người đó mất quyền ngay ở thao tác kế tiếp, kể cả đang đăng nhập.
- Hệ thống không cho khóa quản trị viên cuối cùng.
- **Tình huống:**
  - Thủ kho Hà Nội 2 nghỉ việc. Quản trị viên khóa `tk2.k01` ngay. Máy tính bảng người đó đang cầm bị đăng xuất ở lần bấm tiếp theo.
  - Tuần sau có người mới. Quản trị viên tạo tài khoản thủ kho mới và gán kho K01.

### Nhật ký

- **Làm gì:** tra lại ai làm gì, lúc nào, ở kho nào: duyệt, hủy, xác nhận nhập xuất, điều chỉnh tồn, phân quyền, khóa tài khoản.
- Lọc được theo người, kho, loại thao tác và thời gian.
- Nhật ký chỉ ghi thêm. CSDL chặn mọi lệnh sửa hoặc xóa.
- **Tình huống:** một lô sữa bị hủy hóa đơn nhập. Quản trị viên lọc nhật ký theo loại "hủy" và tìm được người hủy và thời điểm hủy.

### Styleguide

- **Làm gì:** trang cho người phát triển. Hiện các thành phần giao diện dùng chung (bảng, ô quét mã, huy hiệu trạng thái, hộp xác nhận...).
- Chỉ mở được khi chạy ở chế độ Development. Người dùng nghiệp vụ không cần trang này.

---

## Một vòng đời hàng hóa, nhìn qua các tài khoản

1. **Quản lý kho** lập đơn mua với nhà cung cấp.
2. **Thủ kho** nhận hàng, quét mã, lập hóa đơn nhập.
3. **Quản lý kho** xác nhận hóa đơn nhập. Hàng vào khu tiếp nhận.
4. **Thủ kho** xếp hàng lên ô. Hàng thành tồn khả dụng.
5. **Bán hàng** xem tồn khả dụng và tạo đơn đặt hàng.
6. **Quản lý kho** duyệt đơn. G1 giữ lô hợp lý. Quản lý lập lệnh xuất.
7. **Thủ kho** quét lấy hàng.
8. **Quản lý kho** kiểm tra hàng đã lấy, rồi xác nhận xuất.
9. **Bán hàng** thấy đơn Hoàn thành.
10. Cuối tháng **quản lý kho** lập kiểm kê, **thủ kho** đếm, **quản lý kho** duyệt chênh lệch.
11. **Quản lý kho** xem G3 để đặt hàng bổ sung, và vòng lặp bắt đầu lại.
12. **Quản trị viên** theo dõi nhật ký và quản lý tài khoản, danh mục.
