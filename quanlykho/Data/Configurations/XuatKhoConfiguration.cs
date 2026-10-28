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

public class LenhXuatConfiguration : IEntityTypeConfiguration<LenhXuat>
{
    public void Configure(EntityTypeBuilder<LenhXuat> b)
    {
        b.ToTable(t => t.ChiNhan<TrangThaiLenhXuat>(nameof(LenhXuat.TrangThai)));
        b.HasKey(x => x.MaLenh);
        b.Property(x => x.MaLenh).HasMaxLength(Ma);
        b.Property(x => x.MaDonDat).HasMaxLength(Ma);
        b.Property(x => x.MaKho).HasMaxLength(MaViTri);
        b.Property(x => x.NguoiLap).HasMaxLength(Ma);
        b.Property(x => x.NguoiThucHien).HasMaxLength(Ma);
        b.Property(x => x.NguoiKiemTra).HasMaxLength(Ma);
        b.Property(x => x.GhiChuKiemTra).HasMaxLength(500);
        // Một đơn một lệnh.
        b.HasIndex(x => x.MaDonDat).IsUnique();
        b.HasIndex(x => new { x.MaKho, x.TrangThai });
        b.HasOne<DonDatHang>().WithMany().HasForeignKey(x => x.MaDonDat);
        b.HasOne<ViTri>().WithMany().HasForeignKey(x => x.MaKho);
        b.HasOne<NguoiDung>().WithMany().HasForeignKey(x => x.NguoiLap);
        b.HasOne<NguoiDung>().WithMany().HasForeignKey(x => x.NguoiThucHien);
        b.HasOne<NguoiDung>().WithMany().HasForeignKey(x => x.NguoiKiemTra);
        b.HasMany(x => x.ChiTiet).WithOne().HasForeignKey(x => x.MaLenh);
    }
}

public class ChiTietLenhXuatConfiguration : IEntityTypeConfiguration<ChiTietLenhXuat>
{
    public void Configure(EntityTypeBuilder<ChiTietLenhXuat> b)
    {
        b.ToTable(t =>
        {
            t.Check("HeSo", "\"HeSo\" >= 1");
            t.Check("SoLuongYeuCau", "\"SoLuongYeuCau\" > 0 AND \"SoLuongYeuCau\" = \"SoLuongGoc\" * \"HeSo\"");
        });
        b.HasKey(x => new { x.MaLenh, x.MaBienThe });
        b.Property(x => x.MaLenh).HasMaxLength(Ma);
        b.Property(x => x.MaBienThe).HasMaxLength(Ma);
        b.Property(x => x.TenDonVi).HasMaxLength(Ma);
        b.Property(x => x.SoLuongGoc).SoLuong();
        b.Property(x => x.SoLuongYeuCau).SoLuong();
        b.HasOne<BienThe>().WithMany().HasForeignKey(x => x.MaBienThe);
    }
}

public class DongLayHangConfiguration : IEntityTypeConfiguration<DongLayHang>
{
    public void Configure(EntityTypeBuilder<DongLayHang> b)
    {
        b.ToTable(t => t.Check("SoLuong", "\"SoLuong\" > 0 AND \"SoLuongGiuThem\" >= 0 AND \"SoLuongGiuThem\" <= \"SoLuong\""));
        b.HasKey(x => x.MaDong);
        b.Property(x => x.MaDong).UseIdentityAlwaysColumn();
        b.Property(x => x.MaLenh).HasMaxLength(Ma);
        b.Property(x => x.MaBienThe).HasMaxLength(Ma);
        b.Property(x => x.MaLo).HasMaxLength(Ma);
        b.Property(x => x.MaViTri).HasMaxLength(MaViTri);
        b.Property(x => x.SoLuong).SoLuong();
        b.Property(x => x.SoLuongGiuThem).SoLuong();
        b.Property(x => x.NguoiLay).HasMaxLength(Ma);
        b.HasOne<LenhXuat>().WithMany().HasForeignKey(x => x.MaLenh);
        b.HasOne<BienThe>().WithMany().HasForeignKey(x => x.MaBienThe);
        b.HasOne<Lo>().WithMany().HasForeignKey(x => x.MaLo);
        b.HasOne<ViTri>().WithMany().HasForeignKey(x => x.MaViTri);
        b.HasOne<NguoiDung>().WithMany().HasForeignKey(x => x.NguoiLay);
        b.HasIndex(x => x.MaLenh);
    }
}

public class PhieuXuatConfiguration : IEntityTypeConfiguration<PhieuXuat>
{
    public void Configure(EntityTypeBuilder<PhieuXuat> b)
    {
        b.ToTable(t =>
        {
            t.ChiNhan<LoaiPhieuXuat>(nameof(PhieuXuat.LoaiPhieu));
            t.ChiNhan<TrangThaiPhieu>(nameof(PhieuXuat.TrangThai));
            // Xuất bán gắn lệnh xuất; trả nhà cung cấp gắn nhà cung cấp.
            t.Check("TheoLoai", "(\"LoaiPhieu\" = 'XuatBan') = (\"MaLenh\" IS NOT NULL) AND (\"LoaiPhieu\" = 'TraNhaCungCap') = (\"MaNhaCungCap\" IS NOT NULL)");
        });
        b.HasKey(x => x.MaPhieuXuat);
        b.Property(x => x.MaPhieuXuat).HasMaxLength(Ma);
        b.Property(x => x.MaLenh).HasMaxLength(Ma);
        b.Property(x => x.MaKho).HasMaxLength(MaViTri);
        b.Property(x => x.MaNhaCungCap).HasMaxLength(Ma);
        b.Property(x => x.NguoiLap).HasMaxLength(Ma);
        b.Property(x => x.NguoiXacNhan).HasMaxLength(Ma);
        b.Property(x => x.LyDo).HasMaxLength(500);
        b.HasIndex(x => new { x.MaKho, x.NgayXuat });
        b.HasOne<LenhXuat>().WithMany().HasForeignKey(x => x.MaLenh);
        b.HasOne<ViTri>().WithMany().HasForeignKey(x => x.MaKho);
        b.HasOne<NhaCungCap>().WithMany().HasForeignKey(x => x.MaNhaCungCap);
        b.HasOne<NguoiDung>().WithMany().HasForeignKey(x => x.NguoiLap);
        b.HasOne<NguoiDung>().WithMany().HasForeignKey(x => x.NguoiXacNhan);
        b.HasMany(x => x.ChiTiet).WithOne().HasForeignKey(x => x.MaPhieuXuat);
    }
}

public class ChiTietPhieuXuatConfiguration : IEntityTypeConfiguration<ChiTietPhieuXuat>
{
    public void Configure(EntityTypeBuilder<ChiTietPhieuXuat> b)
    {
        b.ToTable(t =>
        {
            t.Check("HeSo", "\"HeSo\" >= 1");
            t.Check("SoLuong", "\"SoLuong\" > 0 AND \"SoLuong\" = \"SoLuongGoc\" * \"HeSo\"");
        });
        b.HasKey(x => new { x.MaPhieuXuat, x.STT });
        b.Property(x => x.MaPhieuXuat).HasMaxLength(Ma);
        b.Property(x => x.MaLo).HasMaxLength(Ma);
        b.Property(x => x.MaViTri).HasMaxLength(MaViTri);
        b.Property(x => x.TenDonVi).HasMaxLength(Ma);
        b.Property(x => x.SoLuongGoc).SoLuong();
        b.Property(x => x.SoLuong).SoLuong();
        b.HasOne<Lo>().WithMany().HasForeignKey(x => x.MaLo);
        b.HasOne<ViTri>().WithMany().HasForeignKey(x => x.MaViTri);
    }
}
