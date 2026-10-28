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

public class LichSuGoiYConfiguration : IEntityTypeConfiguration<LichSuGoiY>
{
    public void Configure(EntityTypeBuilder<LichSuGoiY> b)
    {
        b.ToTable(t => t.ChiNhan<LoaiGoiY>(nameof(LichSuGoiY.Loai)));
        b.HasKey(x => x.MaGoiY);
        b.Property(x => x.MaGoiY).UseIdentityAlwaysColumn();
        b.Property(x => x.MaKho).HasMaxLength(MaViTri);
        b.Property(x => x.MaBienThe).HasMaxLength(Ma);
        b.Property(x => x.NguoiDung).HasMaxLength(Ma);
        b.Property(x => x.DauVao).HasColumnType("jsonb");
        b.Property(x => x.KetQua).HasColumnType("jsonb");
        b.Property(x => x.MaChungTu).HasMaxLength(Ma);
        b.HasOne<ViTri>().WithMany().HasForeignKey(x => x.MaKho);
        b.HasOne<BienThe>().WithMany().HasForeignKey(x => x.MaBienThe);
        b.HasOne<NguoiDung>().WithMany().HasForeignKey(x => x.NguoiDung);
        b.HasIndex(x => new { x.Loai, x.ThoiDiem });
    }
}

public class GiuHangConfiguration : IEntityTypeConfiguration<GiuHang>
{
    public void Configure(EntityTypeBuilder<GiuHang> b)
    {
        b.ToTable(t => t.Check("SoLuong", "\"SoLuong\" > 0"));
        b.HasKey(x => new { x.MaDonDat, x.MaLo, x.MaViTri });
        b.Property(x => x.MaDonDat).HasMaxLength(Ma);
        b.Property(x => x.MaLo).HasMaxLength(Ma);
        b.Property(x => x.MaViTri).HasMaxLength(MaViTri);
        b.Property(x => x.SoLuong).SoLuong();
        b.Property(x => x.NguoiGiu).HasMaxLength(Ma);
        b.HasOne<DonDatHang>().WithMany().HasForeignKey(x => x.MaDonDat);
        b.HasOne<Lo>().WithMany().HasForeignKey(x => x.MaLo);
        b.HasOne<ViTri>().WithMany().HasForeignKey(x => x.MaViTri);
        b.HasOne<LichSuGoiY>().WithMany().HasForeignKey(x => x.MaGoiY);
        b.HasOne<NguoiDung>().WithMany().HasForeignKey(x => x.NguoiGiu);
        b.HasIndex(x => new { x.MaLo, x.MaViTri });
    }
}
