using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using quanlykho.Domain;
using static quanlykho.Data.Configurations.CauHinhChung;

namespace quanlykho.Data.Configurations;

public class DonDatHangConfiguration : IEntityTypeConfiguration<DonDatHang>
{
    public void Configure(EntityTypeBuilder<DonDatHang> b)
    {
        b.ToTable(t => t.ChiNhan<TrangThaiDonDat>(nameof(DonDatHang.TrangThai)));
        b.HasKey(x => x.MaDonDat);
        b.Property(x => x.MaDonDat).HasMaxLength(Ma);
        b.Property(x => x.MaKho).HasMaxLength(MaViTri);
        b.Property(x => x.MaNoiNhan).HasMaxLength(Ma);
        b.Property(x => x.NguoiTao).HasMaxLength(Ma);
        b.Property(x => x.GhiChu).HasMaxLength(500);
        b.HasOne<ViTri>().WithMany().HasForeignKey(x => x.MaKho);
        b.HasOne<NoiNhan>().WithMany().HasForeignKey(x => x.MaNoiNhan);
        b.HasOne<NguoiDung>().WithMany().HasForeignKey(x => x.NguoiTao);
        b.HasMany(x => x.ChiTiet).WithOne().HasForeignKey(x => x.MaDonDat);
        b.HasIndex(x => new { x.MaKho, x.TrangThai });
        b.HasIndex(x => x.NguoiTao);
    }
}

public class ChiTietDonDatConfiguration : IEntityTypeConfiguration<ChiTietDonDat>
{
    public void Configure(EntityTypeBuilder<ChiTietDonDat> b)
    {
        b.ToTable(t =>
        {
            t.Check("HeSo", "\"HeSo\" >= 1");
            t.Check("SoLuongDat", "\"SoLuongDat\" > 0");
            t.Check("SoLuongDaXuat", "\"SoLuongDaXuat\" >= 0 AND \"SoLuongDaXuat\" <= \"SoLuongDat\" * \"HeSo\"");
        });
        b.HasKey(x => new { x.MaDonDat, x.MaBienThe });
        b.Property(x => x.MaDonDat).HasMaxLength(Ma);
        b.Property(x => x.MaBienThe).HasMaxLength(Ma);
        b.Property(x => x.TenDonVi).HasMaxLength(Ma);
        b.Property(x => x.SoLuongDat).SoLuong();
        b.Property(x => x.SoLuongDaXuat).SoLuong();
        b.HasOne<BienThe>().WithMany().HasForeignKey(x => x.MaBienThe);
    }
}
