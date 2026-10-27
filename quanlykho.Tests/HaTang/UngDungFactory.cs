using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace quanlykho.Tests.HaTang;

/// <summary>App trong bộ nhớ cho test; không tự migrate CSDL dev của máy đang chạy test.</summary>
public class UngDungFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder) =>
        builder.ConfigureAppConfiguration(c => c.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Csdl:TuDongMigrate"] = "false"
        }));
}
