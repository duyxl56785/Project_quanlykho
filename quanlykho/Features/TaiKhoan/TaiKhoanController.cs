using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using quanlykho.Common.PhanQuyen;
using quanlykho.Data;

namespace quanlykho.Features.TaiKhoan;

public class DangNhapModel
{
    [Required(ErrorMessage = "Nhập email.")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
    [Display(Name = "Email")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Nhập mật khẩu.")]
    [DataType(DataType.Password)]
    [Display(Name = "Mật khẩu")]
    public string MatKhau { get; set; } = "";

    public string? ReturnUrl { get; set; }
}

public class TaiKhoanController(TaiKhoanService taiKhoan) : Controller
{
    [AllowAnonymous, HttpGet("/DangNhap")]
    public IActionResult DangNhap(string? returnUrl) =>
        User.Identity?.IsAuthenticated == true ? LocalRedirect("/") : View(new DangNhapModel { ReturnUrl = returnUrl });

    [AllowAnonymous, HttpPost("/DangNhap")]
    public async Task<IActionResult> DangNhap(DangNhapModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var principal = await taiKhoan.DangNhapAsync(model.Email, model.MatKhau);
        if (principal is null)
        {
            ModelState.AddModelError(string.Empty, TaiKhoanService.LoiDangNhap);
            model.MatKhau = "";
            return View(model);
        }

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
        // Chỉ chuyển về đường dẫn trong hệ thống, tránh bị lợi dụng chuyển sang trang ngoài.
        return LocalRedirect(Url.IsLocalUrl(model.ReturnUrl) ? model.ReturnUrl! : "/");
    }

    [HttpPost("/DangXuat")]
    public async Task<IActionResult> DangXuat()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return LocalRedirect("/DangNhap");
    }

    [HttpGet("/TuChoi")]
    public IActionResult TuChoi()
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;
        return View();
    }
}

/// <summary>Thông tin người đang đăng nhập cho JS: GET /api/tai-khoan/toi.</summary>
[ApiController, Route("api/tai-khoan")]
public class TaiKhoanApiController(NguoiDungHienTai nguoiDung, AppDbContext db) : ControllerBase
{
    [HttpGet("toi")]
    public async Task<IActionResult> Toi()
    {
        var ten = await db.NguoiDung.Where(n => n.MaNguoiDung == nguoiDung.Ma).Select(n => n.Ten).SingleAsync();
        return Ok(new { ma = nguoiDung.Ma, ten, vaiTro = nguoiDung.VaiTro.ToString(), kho = await nguoiDung.KhoDuocGiaoAsync() });
    }
}
