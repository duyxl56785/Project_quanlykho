using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using quanlykho.Domain;
using static quanlykho.Data.Configurations.CauHinhChung;

namespace quanlykho.Data.Configurations;

public class PhieuDieuChuyenConfiguration : IEntityTypeConfiguration<PhieuDieuChuyen>
{
    public void Configure(EntityTypeBuilder<PhieuDieuChuyen> b)
    {
        b.ToTable(t =>
        {
            t.ChiNhan<TrangThaiDieuChuyen>(nameof(PhieuDieuChuyen.TrangThai));
            t.Check("HaiKhoKhacNhau", "\"MaKhoDi\" <> \"MaKhoDen\"");
        });
        b.HasKey(x => x.MaPhieu);
        b.Property(x => x.MaPhieu).HasMaxLength(Ma);
        b.Property(x => x.MaKhoDi).HasMaxLength(MaViTri);
        b.Property(x => x.MaKhoDen).HasMaxLength(MaViTri);
        b.Property(x => x.PhuongTien).HasMaxLength(100);
        b.Property(x => x.NguoiVanChuyen).HasMaxLength(100);
        b.Property(x => x.LyDo).HasMaxLength(500);
        b.Property(x => x.NguoiLap).HasMaxLength(Ma);
        b.Property(x => x.NguoiNhan).HasMaxLength(Ma);
        b.HasOne<ViTri>().WithMany().HasForeignKey(x => x.MaKhoDi);
        b.HasOne<ViTri>().WithMany().HasForeignKey(x => x.MaKhoDen);
        b.HasOne<NguoiDung>().WithMany().HasForeignKey(x => x.NguoiLap);
        b.HasOne<NguoiDung>().WithMany().HasForeignKey(x => x.NguoiNhan);
        b.HasMany(x => x.ChiTiet).WithOne().HasForeignKey(x => x.MaPhieu);
        b.HasIndex(x => new { x.MaKhoDen, x.TrangThai });
        b.HasIndex(x => new { x.MaKhoDi, x.TrangThai });
    }
}

public class ChiTietDieuChuyenConfiguration : IEntityTypeConfiguration<ChiTietDieuChuyen>
{
    public void Configure(EntityTypeBuilder<ChiTietDieuChuyen> b)
    {
        b.ToTable(t =>
        {
            t.Check("HeSo", "\"HeSo\" >= 1");
            t.Check("SoLuong", "\"SoLuong\" > 0 AND \"SoLuong\" = \"SoLuongGoc\" * \"HeSo\"");
            t.Check("SoLuongNhan", "\"SoLuongNhan\" IS NULL OR (\"SoLuongNhan\" >= 0 AND \"SoLuongNhan\" <= \"SoLuong\")");
        });
        b.HasKey(x => new { x.MaPhieu, x.STT });
        b.Property(x => x.MaPhieu).HasMaxLength(Ma);
        b.Property(x => x.MaLo).HasMaxLength(Ma);
        b.Property(x => x.MaViTriDi).HasMaxLength(MaViTri);
        b.Property(x => x.TenDonVi).HasMaxLength(Ma);
        b.Property(x => x.SoLuongGoc).SoLuong();
        b.Property(x => x.SoLuong).SoLuong();
        b.Property(x => x.SoLuongNhan).SoLuong();
        b.HasOne<Lo>().WithMany().HasForeignKey(x => x.MaLo);
        b.HasOne<ViTri>().WithMany().HasForeignKey(x => x.MaViTriDi);
    }
}
