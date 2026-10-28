namespace quanlykho.Domain;

// Lưu trong CSDL bằng tên (chuỗi không dấu), có CHECK danh sách giá trị.

public enum VaiTro { QuanTriVien, QuanLyKho, ThuKho, NhanVienBanHang }

public enum TrangThaiTaiKhoan { DangHoatDong, DaKhoa }

public enum TrangThaiSuDung { DangSuDung, NgungSuDung }

public enum DieuKienBaoQuan { Thuong, Lanh }

public enum LoaiKhu { TiepNhan, LuuTru, CachLy, Xuat }

public enum ChatLuong { Dat, Loi }

/// <summary>Đơn mua (Bảng 2.14 dòng 1). Bước Chờ nhận hàng, Chờ kiểm đếm tính từ phiếu nhập, không lưu.</summary>
public enum TrangThaiDonMua { Nhap, ChuaThucHien, DangThucHien, HoanThanh, DaHuy }

/// <summary>Phiếu nhập, phiếu xuất ghi sổ (Bảng 2.14 dòng 5).</summary>
public enum TrangThaiPhieu { LuuTam, HoanTat, DaHuy }

public enum LoaiPhieuNhap { MuaHang, NhapKhac, TonDauKy }

/// <summary>Đơn đặt hàng (Bảng 2.14 dòng 2). Hủy chỉ trước khi xuất.</summary>
public enum TrangThaiDonDat { Nhap, ChoDuyet, DaDuyet, DangXuat, HoanThanh, DaHuy }

/// <summary>G1 gợi ý lấy hàng, G3 gợi ý nhập bổ sung.</summary>
public enum LoaiGoiY { LayHang, NhapBoSung }

/// <summary>Lệnh xuất (Bảng 2.14 dòng 3): Đang thực hiện gồm ba bước Chờ lấy hàng, Chờ kiểm tra, Chờ xác nhận.</summary>
public enum TrangThaiLenhXuat { ChuaThucHien, ChoLayHang, ChoKiemTra, ChoXacNhan, HoanThanh }

public enum LoaiPhieuXuat { XuatBan, XuatKhac, TraNhaCungCap }
