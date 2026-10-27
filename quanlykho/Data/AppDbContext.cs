using Microsoft.EntityFrameworkCore;

namespace quanlykho.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
}
