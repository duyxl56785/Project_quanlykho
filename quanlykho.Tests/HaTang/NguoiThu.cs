using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using quanlykho.Common;
using quanlykho.Common.PhanQuyen;
using quanlykho.Data;

namespace quanlykho.Tests.HaTang;

public static class NguoiThu
{
    /// <summary>Người dùng đang đăng nhập cho test gọi Service trực tiếp (mã, vai theo dữ liệu thử).</summary>
    public static NguoiDungHienTai HienTai(AppDbContext db, string ma, string vai) => new(new HttpContextAccessor
    {
        HttpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, ma), new Claim(ClaimTypes.Role, vai)], "thu"))
        }
    }, db);

    public static GhiNhatKy NhatKy(AppDbContext db, NguoiDungHienTai hienTai) => new(db, hienTai, TimeProvider.System);

    public static async Task<string> TokenAsync(HttpClient client, string trang)
    {
        var html = await client.GetStringAsync(trang);
        var m = Regex.Match(html, "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"");
        return m.Success ? m.Groups[1].Value : Regex.Match(html, "name=\"xsrf-token\" content=\"([^\"]+)\"").Groups[1].Value;
    }

    public static async Task<HttpResponseMessage> PostFormAsync(HttpClient client, string url, string trangLayToken, Dictionary<string, string>? duLieu = null)
    {
        duLieu ??= [];
        duLieu["__RequestVerificationToken"] = await TokenAsync(client, trangLayToken);
        var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = new FormUrlEncodedContent(duLieu) };
        request.Headers.Referrer = new Uri(client.BaseAddress!, trangLayToken);
        return await client.SendAsync(request);
    }
}
