using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace quanlykho.Common.Loi;

/// <summary>
/// Đổi lỗi nghiệp vụ thành phản hồi: API ([ApiController]) trả JSON { loi, chiTiet } với 422, 409, 403;
/// trang Razor hiện lại view của action với lỗi trong ModelState (giữ nguyên dữ liệu người dùng đã nhập);
/// action không có biểu mẫu hoặc không có view trùng tên thì báo qua TempData["Loi"] và quay lại trang trước; 403 chuyển tới /TuChoi.
/// </summary>
public class XuLyLoiFilter(IModelMetadataProvider metadataProvider) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var executed = await next();
        if (executed.Exception is null || executed.ExceptionHandled)
            return;

        var (status, chiTiet) = executed.Exception switch
        {
            NghiepVuException e => (StatusCodes.Status422UnprocessableEntity, e.ChiTiet),
            XungDotException => (StatusCodes.Status409Conflict, null),
            KhongCoQuyenException => (StatusCodes.Status403Forbidden, null),
            _ => (0, null)
        };
        if (status == 0)
            return;

        var thongDiep = executed.Exception.Message;
        executed.ExceptionHandled = true;

        if (context.ActionDescriptor.EndpointMetadata.OfType<IApiBehaviorMetadata>().Any())
        {
            executed.Result = new ObjectResult(new { loi = thongDiep, chiTiet }) { StatusCode = status };
            return;
        }

        if (status == StatusCodes.Status403Forbidden)
        {
            executed.Result = new ForbidResult(); // cookie auth chuyển tới trang /TuChoi
            return;
        }

        var model = context.ActionArguments.Values.FirstOrDefault(v => v is not null && !v.GetType().IsValueType && v is not string);
        var coView = !context.ActionDescriptor.RouteValues.TryGetValue("action", out var tenView) || tenView is null
            || (context.HttpContext.RequestServices?.GetService<ICompositeViewEngine>()?.FindView(context, tenView, isMainPage: true).Success ?? true);
        if (model is null || !coView)
        {
            // Action chỉ nhận mã (nút Khóa, Hủy...): không có biểu mẫu để hiện lại, báo lỗi qua TempData và quay lại trang trước.
            var http = context.HttpContext;
            http.RequestServices!.GetRequiredService<ITempDataDictionaryFactory>().GetTempData(http)["Loi"] = thongDiep;
            var truoc = http.Request.Headers.Referer.ToString();
            executed.Result = new LocalRedirectResult(Uri.TryCreate(truoc, UriKind.Absolute, out var u) && u.Authority == http.Request.Host.Value
                ? u.PathAndQuery : "/");
            return;
        }

        context.ModelState.AddModelError(string.Empty, thongDiep);
        executed.Result = new ViewResult { ViewData = new ViewDataDictionary(metadataProvider, context.ModelState) { Model = model } };
    }
}
