using System.Net;
using System.Text.RegularExpressions;
using quanlykho.Tests.HaTang;

namespace quanlykho.Tests;

public class StyleguideTests(UngDungFactory factory) : IClassFixture<UngDungFactory>
{
    [Theory]
    [InlineData("/Styleguide")]
    [InlineData("/Styleguide/DanhSach")]
    [InlineData("/Styleguide/BieuMau")]
    [InlineData("/Styleguide/Phieu")]
    [InlineData("/Styleguide/Quet")]
    public async Task TrangMau_Tra200(string url)
    {
        var response = await factory.TaoClient().GetAsync(url);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task DanhSach_LocVaPhanTrangGiuThamSo()
    {
        var html = await factory.TaoClient().GetStringAsync("/Styleguide/DanhSach?trangThai=HoanThanh&trang=2");

        // 57 dòng mẫu, trạng thái i % 5 == 3 (HoanThanh) có 11 dòng: trang 2 là dòng 11–11.
        Assert.Contains("11–11 / 11 dòng", html);
        Assert.Matches(new Regex("href=\"/Styleguide/DanhSach\\?trangThai=HoanThanh&amp;trang=1\""), html);
        Assert.DoesNotContain("Chưa thực hiện</span>", html);
    }

    [Fact]
    public async Task DanhSach_KhongCoKetQua_HienTrangThaiRong()
    {
        var html = await factory.TaoClient().GetStringAsync("/Styleguide/DanhSach?tuKhoa=khongcoma");
        Assert.Contains("Không có đơn mua nào khớp bộ lọc.", html);
    }

    [Fact]
    public async Task BieuMau_LoiNghiepVu_HienLaiBieuMauGiuDuLieu()
    {
        var client = factory.TaoClient();
        var trang = await client.GetStringAsync("/Styleguide/BieuMau");
        var token = Regex.Match(trang, "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"").Groups[1].Value;

        var response = await client.PostAsync("/Styleguide/BieuMau", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Ma"] = "TRUNG", ["Ten"] = "Nhà cung cấp thử", ["__RequestVerificationToken"] = token
        }));
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Mã TRUNG đã có trong danh mục.", html);
        Assert.Contains("value=\"Nhà cung cấp thử\"", html);
    }

    [Fact]
    public async Task Post_KhongCoTokenChongCsrf_BiTuChoi()
    {
        var response = await factory.TaoClient().PostAsync("/Styleguide/BieuMau",
            new FormUrlEncodedContent(new Dictionary<string, string> { ["Ma"] = "A", ["Ten"] = "B" }));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
