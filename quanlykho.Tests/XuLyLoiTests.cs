using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using quanlykho.Tests.HaTang;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using quanlykho.Common.Loi;

namespace quanlykho.Tests;

[ApiController]
[Route("api/thu-loi")]
public class ThuLoiApiController : ControllerBase
{
    [HttpGet("nghiep-vu")]
    public IActionResult NghiepVu() => throw new NghiepVuException("Cần 10, còn 4", new { can = 10, con = 4 });

    [HttpGet("xung-dot")]
    public IActionResult XungDot() => throw new XungDotException("Ô vừa được cập nhật, vui lòng thử lại");

    [HttpGet("khong-quyen")]
    public IActionResult KhongQuyen() => throw new KhongCoQuyenException();
}

public class XuLyLoiTests(UngDungFactory factory) : IClassFixture<UngDungFactory>
{
    private HttpClient TaoClient() => factory.WithWebHostBuilder(b => b.ConfigureServices(s =>
        s.AddControllers().AddApplicationPart(typeof(ThuLoiApiController).Assembly))).CreateClient();

    [Theory]
    [InlineData("nghiep-vu", HttpStatusCode.UnprocessableEntity, "Cần 10, còn 4")]
    [InlineData("xung-dot", HttpStatusCode.Conflict, "Ô vừa được cập nhật, vui lòng thử lại")]
    [InlineData("khong-quyen", HttpStatusCode.Forbidden, "Bạn không có quyền thực hiện thao tác này.")]
    public async Task Api_DoiLoiThanhMaHttpVaJson(string duongDan, HttpStatusCode maMongDoi, string loiMongDoi)
    {
        var response = await TaoClient().GetAsync($"/api/thu-loi/{duongDan}");

        Assert.Equal(maMongDoi, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(loiMongDoi, json.GetProperty("loi").GetString());
    }

    [Fact]
    public async Task Api_LoiNghiepVu_KemChiTiet()
    {
        var response = await TaoClient().GetAsync("/api/thu-loi/nghiep-vu");
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(4, json.GetProperty("chiTiet").GetProperty("con").GetInt32());
    }

    [Fact]
    public async Task Trang_LoiNghiepVu_HienLaiViewVoiLoiTrongModelState()
    {
        var model = new { Ten = "đã nhập" };
        var actionContext = new ActionContext(new DefaultHttpContext(), new RouteData(), new ControllerActionDescriptor());
        var executing = new ActionExecutingContext(actionContext, [], new Dictionary<string, object?> { ["model"] = model }, controller: new object());
        var filter = new XuLyLoiFilter(new EmptyModelMetadataProvider());
        ActionExecutedContext? executed = null;

        await filter.OnActionExecutionAsync(executing, () =>
        {
            executed = new ActionExecutedContext(actionContext, [], new object()) { Exception = new NghiepVuException("Số nhận vượt số chưa nhận") };
            return Task.FromResult(executed);
        });

        var view = Assert.IsType<ViewResult>(executed!.Result);
        Assert.True(executed.ExceptionHandled);
        Assert.Same(model, view.ViewData.Model);
        Assert.Equal("Số nhận vượt số chưa nhận", actionContext.ModelState[string.Empty]!.Errors.Single().ErrorMessage);
    }

    [Fact]
    public async Task LoiKhac_KhongBiNuot()
    {
        var actionContext = new ActionContext(new DefaultHttpContext(), new RouteData(), new ControllerActionDescriptor());
        var executing = new ActionExecutingContext(actionContext, [], new Dictionary<string, object?>(), controller: new object());
        ActionExecutedContext? executed = null;

        await new XuLyLoiFilter(new EmptyModelMetadataProvider()).OnActionExecutionAsync(executing, () =>
            Task.FromResult(executed = new ActionExecutedContext(actionContext, [], new object()) { Exception = new InvalidOperationException() }));

        Assert.False(executed!.ExceptionHandled);
    }
}
