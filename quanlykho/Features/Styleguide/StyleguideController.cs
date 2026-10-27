using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using quanlykho.Common.GiaoDien;
using quanlykho.Common.Loi;
using quanlykho.Domain;

namespace quanlykho.Features.Styleguide;

/// <summary>
/// Trang /Styleguide (Bảng 1.12): mọi thành phần giao diện thật và bốn trang mẫu (danh sách, biểu mẫu, phiếu có dòng hàng, màn hình quét).
/// Màn hình mới sao chép từ trang mẫu gần nhất. Chỉ mở ở Development (thêm điều kiện quản trị viên ở nhiệm vụ đăng nhập).
/// Dữ liệu trên các trang mẫu là dữ liệu giả trong bộ nhớ.
/// </summary>
public class StyleguideController(IWebHostEnvironment env) : Controller
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (!env.IsDevelopment())
            context.Result = NotFound();
    }

    public IActionResult Index() => View();

    public IActionResult DanhSach(string? tuKhoa, TrangThaiDonMua? trangThai, int trang = 1)
    {
        var loc = PhieuMau.Where(p =>
            (string.IsNullOrWhiteSpace(tuKhoa) || p.Ma.Contains(tuKhoa, StringComparison.OrdinalIgnoreCase) || p.NhaCungCap.Contains(tuKhoa, StringComparison.OrdinalIgnoreCase))
            && (trangThai is null || p.TrangThai == trangThai));
        ViewData["TuKhoa"] = tuKhoa;
        ViewData["TrangThai"] = trangThai;
        return View(TrangDuLieu<PhieuMauDong>.Tao(loc, trang, 10));
    }

    [HttpGet]
    public IActionResult BieuMau() => View(new BieuMauMau());

    [HttpPost]
    public IActionResult BieuMau(BieuMauMau model)
    {
        if (!ModelState.IsValid)
            return View(model);
        // Minh họa lỗi nghiệp vụ: XuLyLoiFilter hiện lại biểu mẫu kèm lỗi, giữ nguyên dữ liệu đã nhập.
        if (model.Ma.Equals("TRUNG", StringComparison.OrdinalIgnoreCase))
            throw new NghiepVuException($"Mã {model.Ma} đã có trong danh mục.");
        TempData["ThongBao"] = $"Đã lưu nhà cung cấp {model.Ten}.";
        return RedirectToAction(nameof(BieuMau));
    }

    public IActionResult Phieu() => View();

    public IActionResult Quet() => View();

    // ---------- Dữ liệu giả ----------

    public record PhieuMauDong(string Ma, string NhaCungCap, string Kho, DateTimeOffset NgayTao, decimal TongTien, TrangThaiDonMua TrangThai);

    private static readonly string[] TenNhaCungCap = ["Việt Tiến Phát", "Điện tử Sao Mai", "Thực phẩm Xanh"];

    public static readonly IReadOnlyList<PhieuMauDong> PhieuMau = Enumerable.Range(1, 57).Select(i => new PhieuMauDong(
        $"DM{i:D6}", TenNhaCungCap[i % 3], i % 2 == 0 ? "K01" : "K02",
        new DateTimeOffset(2026, 10, 1, 2, 0, 0, TimeSpan.Zero).AddHours(i * 7),
        1_250_000m * i + 500m, (TrangThaiDonMua)(i % 5))).Reverse().ToList();

    public class BieuMauMau
    {
        [Required(ErrorMessage = "Nhập mã.")]
        [StringLength(20, ErrorMessage = "Mã tối đa 20 ký tự.")]
        [RegularExpression("^[A-Za-z0-9_-]+$", ErrorMessage = "Mã chỉ gồm chữ không dấu, số, gạch ngang, gạch dưới.")]
        [Display(Name = "Mã nhà cung cấp")]
        public string Ma { get; set; } = "";

        [Required(ErrorMessage = "Nhập tên.")]
        [StringLength(200)]
        [Display(Name = "Tên")]
        public string Ten { get; set; } = "";

        [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]
        [Display(Name = "Số điện thoại")]
        public string? SoDienThoai { get; set; }

        [Display(Name = "Địa chỉ")]
        public string? DiaChi { get; set; }

        [Display(Name = "Kho mặc định")]
        public string? MaKho { get; set; }
    }
}
