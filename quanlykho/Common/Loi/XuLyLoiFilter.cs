using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace quanlykho.Common.Loi;

/// <summary>
/// Đổi lỗi nghiệp vụ thành phản hồi: API ([ApiController]) trả JSON { loi, chiTiet } với 422, 409, 403;
/// trang Razor hiện lại view của action với lỗi trong ModelState (giữ nguyên dữ liệu người dùng đã nhập).
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
            executed.Result = new StatusCodeResult(status);
            return;
        }

        context.ModelState.AddModelError(string.Empty, thongDiep);
        executed.Result = new ViewResult
        {
            ViewData = new ViewDataDictionary(metadataProvider, context.ModelState)
            {
                Model = context.ActionArguments.Values.FirstOrDefault(v => v is not null && !v.GetType().IsPrimitive && v is not string)
            }
        };
    }
}
