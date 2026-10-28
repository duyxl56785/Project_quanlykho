# Nghiệm thu theo Bảng 1.6

Ngày 29/10/2026, nhánh `develop`. Bộ test tự động: 237 test (xUnit, test tích hợp chạy trên PostgreSQL thật qua Testcontainers), CI xanh. Kiểm tra tay trên trình duyệt (khung máy tính bảng 768 × 1024, điện thoại 375 × 812) với DB dev đã `--seed` đầy đủ. Tên test dạng `Lớp.PhươngThức` trong `quanlykho.Tests/`.

## Thủ kho

| Tiêu chí | Kết quả | Bằng chứng |
| --- | --- | --- |
| Chạy trọn nhập hàng bằng ô quét mã trên máy tính bảng | Đạt | Thử tay: quét mã hàng và mã thùng trên phiếu nhập (PN000004, hủy sau khi thử), xếp hàng bằng quét lô rồi quét ô. `PhieuNhapTests.LapTuDonMua_MoiDongMotLoMoi_GiaTheoDon`, `QuyDoiPhieuNhapTests.QuetMaThung_MuoiThung_GhiTon240Hop`, `XepHangTests.XepDung_GhiChuyenViTri_TongTonKhongDoi_BamLapKhongGhiHaiLan` |
| Chạy trọn lấy hàng bằng ô quét mã | Đạt | Thử tay: LX000001 quét ô, quét lô trên khung máy tính bảng tới PX000001. `LenhXuatTests.LuongTron_Hinh26_ChanSaiHang_HetHan_Vuot_NguoiKiemKhacNguoiLay`, `QuyDoiXuatChuyenTests.LayHangBangMaThung_TinhTheoHop` |
| Nhận hàng chuyển đến bằng màn hình quét | Đạt | Thử tay: nhận DC000001 (thiếu 1, xử lý ghi giảm hao hụt). `DieuChuyenTests.LuuTam_Luu_NhanDu_TongTonCongVanChuyenKhongDoi`, `DieuChuyenTests.NhanThieu_NhanMotPhan_SinhChenhLech` |
| G1 không vi phạm ràng buộc | Đạt | `GoiYLayHangTests`: `KhongBaoGioChonLoHetHan`, `FefoTruoc_LoKhongHanXepSau_RoiFifo`, `TongDungQ_KhongVuotKhaDungTungO`, `CungHanDung_ChonLoItKeHon`, `ThieuHang_BaoDungSoConThieu`; `GiuHangTests.Duyet_GiuTheoG1_ChiLoDatChuaHetHan` |
| Quét sai bị chặn | Đạt | `LenhXuatTests.LuongTron_Hinh26_ChanSaiHang_HetHan_Vuot_NguoiKiemKhacNguoiLay` (sai hàng, lô hết hạn, vượt số yêu cầu, ô không có hàng) |

## Quản lý kho

| Tiêu chí | Kết quả | Bằng chứng |
| --- | --- | --- |
| Tổng biến động bằng tồn từng kho | Đạt | `CongGhiTonTests.NhapRoiChuyen_TongBienDongBangTon`, `BaoCaoTests.TongHop_TungKhoCanhTong_TachKhuVaVanChuyen_KhopSoBienDong`, `LichSuXuatTests` (3 tháng dữ liệu). DB dev: K01 280 = 280, K02 232 = 232 |
| Chuyển kho không mất hay nhân đôi hàng | Đạt | `DieuChuyenTests.LuuTam_Luu_NhanDu_TongTonCongVanChuyenKhongDoi`, `HuyKhiChuaNhan_CongTraVeDungO`; `QuyDoiXuatChuyenTests.ChuyenHaiThung_Tru48Hop_TongHeThongKhongDoi` |
| Hai phiếu không giữ trùng | Đạt | `GiuHangTests.HaiLanDuyetSongSong_KhongGiuTrung`, `CongGhiTonTests.GiuSongSong_KhongGiuTrung` |
| Mọi chênh lệch có người duyệt | Đạt | `DieuChinhTonTests` (6 loại chênh lệch kiểm kê, 3 cách xử lý chênh lệch chuyển kho; bắt buộc loại, kết luận, người duyệt; chỉ quản lý kho) |
| 10 thùng xuất 5 chai còn 235 chai | Đạt | `QuyDoiXuatChuyenTests.XuatNamHopTuLoMuoiThung_Con235_BaoCaoHienThungLe`, `QuyDoiPhieuNhapTests.QuetMaThung_MuoiThung_GhiTon240Hop`, `QuyDoiKiemKeTests.DemChinThungMuoiChinHop_KhopSoSach235` |
| G3 không bỏ sót mặt hàng dưới điểm đặt hàng | Đạt | `DeXuatNhapBoSungTests` (các ca tính tay, đúng bằng điểm đặt hàng vẫn đề xuất), `DeXuatTests.TinhTheoKho_KhopTinhTay`, `LichSuXuatTests` (đủ 3 mặt hàng dưới điểm đặt của dữ liệu thử). DB dev: đề xuất giấy 44 ram, sữa 227 hộp ở K01 |

## Nhân viên bán hàng

| Tiêu chí | Kết quả | Bằng chứng |
| --- | --- | --- |
| Tồn khả dụng từng kho khớp số xuất được | Đạt | `DonDatTests.TonKhaDung_LoaiTruChoKiem_CachLy_HetHan_DaGiu_KhoaKiemKe`, `GiuHangTests.Thieu_Bao422CanConY_KhongGiuGi`, `TongQuanTests.MucTon_SoVoiTonKhaDung` (Tổng quan, báo cáo cùng một truy vấn tồn khả dụng) |
| Luôn xem được trạng thái đơn | Đạt | `DonDatTests.TaoDon_GuiDuyet_ChiNguoiTaoVaKhoDuocGiaoThay`. Thử tay trên điện thoại: danh sách và chi tiết đơn hiện trạng thái, không tràn ngang |

## Quản trị viên

| Tiêu chí | Kết quả | Bằng chứng |
| --- | --- | --- |
| Thêm ngành hàng mới không sửa mã rồi nhập, xuất được | Đạt | `NghiemThuTests.QuanTri_ThemNganhMoiKhongSuaMa_RoiNhapXepXuatDuoc` (ngành Đồ gia dụng, thuộc tính Công suất, Bảo hành, sinh 2 biến thể, nhập 10, xếp, xuất 4, còn 6) |
| Phân quyền theo vai và kho đúng | Đạt | `PhanQuyenTests` (ma trận vai, chặn kho không được giao, khóa tài khoản mất quyền ngay, luôn còn quản trị viên); test `ManHinh_*`, `Api_*` của từng mảng |
| Nhật ký đủ | Đạt | `NhatKyTests` (CSDL chặn sửa, xóa; các thao tác ghi đủ dòng; tra theo người, kho, loại, thời gian) và kiểm tra nhật ký trong test của từng mảng |
| Nhập Excel báo lỗi đúng dòng, nhập lại không nhân đôi | Đạt | `NhapExcelTests.TepMau300Dong_NamDongLoi_BaoDungDong_KhongGhiGi`, `SuaXong_GhiDu_NhapLaiCungTepBiChan`; `NhapTonDauKyTests` (300 dòng, vượt sức chứa báo đúng dòng, nhập lại bị chặn) |

## Chung

| Tiêu chí | Kết quả | Bằng chứng |
| --- | --- | --- |
| Không có tồn âm | Đạt | Ràng buộc CSDL `SoLuong >= 0`, `SoLuongDaGiu <= SoLuong` (`RangBuocCsdlTests`); khóa dòng khi ghi: `CongGhiTonTests.HaiNguoiTruSongSong_VuotTong_MotNguoiBiChan_TonKhongAm`, `LenhXuatTests.HaiThuKhoLayCungHangChuaGiu_KhongTonAm` |
| Thống kê được tỷ lệ gợi ý AI được chấp nhận | Đạt | Báo cáo `/BaoCao/GoiY` (API `/api/bao-cao/goi-y`): số lần gợi ý, số đã biết kết quả, số làm theo, tỷ lệ theo G1, G3, lọc kho và kỳ. `NghiemThuTests.ThongKeTyLeChapNhanGoiYAi` |

## Lỗi tìm được khi nghiệm thu

- Thiếu màn hình thống kê tỷ lệ chấp nhận gợi ý AI: đã làm báo cáo `/BaoCao/GoiY`.
- `site.css` bị ghi lặp ký tự xuống dòng (CR thừa) ở một commit trước: đã chuẩn hóa lại.
