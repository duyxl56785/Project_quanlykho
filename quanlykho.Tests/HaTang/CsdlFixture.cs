using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;
using quanlykho.Data;
using quanlykho.Data.Seed;
using quanlykho.Domain;
using Testcontainers.PostgreSql;

namespace quanlykho.Tests.HaTang;

/// <summary>
/// Một container PostgreSQL cho cả lượt chạy test (cần Docker đang chạy).
/// Mỗi lớp test gọi TaoCsdlAsync để có database riêng đã chạy migration.
/// </summary>
public class CsdlFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17-alpine").Build();

    public Task InitializeAsync() => _container.StartAsync();

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();

    public async Task<string> TaoCsdlAsync()
    {
        var ten = $"t_{Guid.NewGuid():N}";
        await using (var conn = new NpgsqlConnection(_container.GetConnectionString()))
        {
            await conn.OpenAsync();
            await using var cmd = new NpgsqlCommand($"CREATE DATABASE \"{ten}\"", conn);
            await cmd.ExecuteNonQueryAsync();
        }

        var chuoiKetNoi = new NpgsqlConnectionStringBuilder(_container.GetConnectionString()) { Database = ten }.ConnectionString;
        await using var db = TaoDbContext(chuoiKetNoi);
        await db.Database.MigrateAsync();
        return chuoiKetNoi;
    }

    public const string MatKhauThu = "MatKhauThu@1";

    public async Task<string> TaoCsdlCoDuLieuThuAsync()
    {
        var chuoi = await TaoCsdlAsync();
        await using var db = TaoDbContext(chuoi);
        await new DuLieuThu(db, new PasswordHasher<NguoiDung>(), new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["DuLieuThu:MatKhau"] = MatKhauThu }).Build()).NapAsync();
        return chuoi;
    }

    public static AppDbContext TaoDbContext(string chuoiKetNoi) =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(chuoiKetNoi).Options);
}

[CollectionDefinition(Ten)]
public class CsdlCollection : ICollectionFixture<CsdlFixture>
{
    public const string Ten = "CSDL";
}
