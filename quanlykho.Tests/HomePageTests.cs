using System.Net;
using quanlykho.Tests.HaTang;

namespace quanlykho.Tests;

public class HomePageTests(UngDungFactory factory) : IClassFixture<UngDungFactory>
{
    [Fact]
    public async Task TrangChu_TraVe200()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
