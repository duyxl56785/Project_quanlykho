using quanlykho.Data;

namespace quanlykho.Common;

public static class KiemTraCsdl
{
    /// <summary>GET /health/db: 200 khi kết nối được CSDL, 503 khi không.</summary>
    public static void MapKiemTraCsdl(this WebApplication app) =>
        app.MapGet("/health/db", async (AppDbContext db) =>
            await db.Database.CanConnectAsync() ? Results.Ok("OK") : Results.StatusCode(503));
}
