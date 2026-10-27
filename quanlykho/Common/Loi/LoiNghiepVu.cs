namespace quanlykho.Common.Loi;

/// <summary>Vi phạm luật nghiệp vụ: API trả 422, trang hiện lỗi trên biểu mẫu.</summary>
public class NghiepVuException(string thongDiep, object? chiTiet = null) : Exception(thongDiep)
{
    public object? ChiTiet { get; } = chiTiet;
}

/// <summary>Xung đột ghi đồng thời, người dùng cần thử lại: 409.</summary>
public class XungDotException(string thongDiep) : Exception(thongDiep);

/// <summary>Không có quyền theo vai hoặc theo kho: 403.</summary>
public class KhongCoQuyenException(string thongDiep = "Bạn không có quyền thực hiện thao tác này.") : Exception(thongDiep);
