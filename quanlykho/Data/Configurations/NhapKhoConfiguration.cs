using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using quanlykho.Domain;
using static quanlykho.Data.Configurations.CauHinhChung;

namespace quanlykho.Data.Configurations;

public class DonMuaConfiguration : IEntityTypeConfiguration<DonMua>
{
    public void Configure(EntityTypeBuilder<DonMua> b)
    {
        b.ToTable(t => t.ChiNhan<TrangThaiDonMua>(nameof(DonMua.TrangThai)));
        b.HasKey(x => x.MaDonMua);
        // Khóa phụ để PhieuNhap tham chiếu (MaDonMua, MaKho): phiếu nhập từ đơn mua phải cùng kho.
        b.HasAlternateKey(x => new { x.MaDonMua, x.MaKho });
        b.Property(x => x.MaDonMua).HasMaxLength(Ma);
        b.Property(x => x.MaNhaCungCap).HasMaxLength(Ma);
        b.Property(x => x.MaKho).HasMaxLength(MaViTri);
        b.Property(x => x.NguoiTao).HasMaxLength(Ma);
        b.Property(x => x.GhiChu).HasMaxLength(500);
        b.HasOne<NhaCungCap>().WithMany().HasForeignKey(x => x.MaNhaCungCap);
        b.HasOne<ViTri>().WithMany().HasForeignKey(x => x.MaKho);
        b.HasOne<NguoiDung>().WithMany().HasForeignKey(x => x.NguoiTao);
        b.HasMany(x => x.ChiTiet).WithOne().HasForeignKey(x => x.MaDonMua);
    }
}

public class ChiTietDonMuaConfiguration : IEntityTypeConfiguration<ChiTietDonMua>
{
    public void Configure(EntityTypeBuilder<ChiTietDonMua> b)
    {
        b.ToTable(t =>
        {
            t.Check("HeSo", "\"HeSo\" >= 1");
            t.Check("SoLuongDat", "\"SoLuongDat\" > 0");
            t.Check("DonGia", "\"DonGia\" >= 0");
        });
        b.HasKey(x => new { x.MaDonMua, x.MaBienThe });
        b.Property(x => x.MaDonMua).HasMaxLength(Ma);
        b.Property(x => x.MaBienThe).HasMaxLength(Ma);
        b.Property(x => x.TenDonVi).HasMaxLength(Ma);
        b.Property(x => x.SoLuongDat).SoLuong();
        b.Property(x => x.DonGia).Tien();
        b.HasOne<BienThe>().WithMany().HasForeignKey(x => x.MaBienThe);
    }
}

public class PhieuNhapConfiguration : IEntityTypeConfiguration<PhieuNhap>
{
    public void Configure(EntityTypeBuilder<PhieuNhap> b)
    {
        b.ToTable(t =>
        {
            t.ChiNhan<LoaiPhieuNhap>(nameof(PhieuNhap.LoaiPhieu));
            t.ChiNhan<TrangThaiPhieu>(nameof(PhieuNhap.TrangThai));
            t.Check("DonMua", "(\"LoaiPhieu\" = 'MuaHang') = (\"MaDonMua\" IS NOT NULL)");
        });
        b.HasKey(x => x.MaPhieuNhap);
        b.Property(x => x.MaPhieuNhap).HasMaxLength(Ma);
        b.Property(x => x.MaDonMua).HasMaxLength(Ma);
        b.Property(x => x.MaKho).HasMaxLength(MaViTri);
        b.Property(x => x.NguoiLap).HasMaxLength(Ma);
        b.Property(x => x.NguoiXacNhan).HasMaxLength(Ma);
        b.Property(x => x.GhiChu).HasMaxLength(500);
        b.HasOne<DonMua>().WithMany()
            .HasForeignKey(x => new { x.MaDonMua, x.MaKho })
            .HasPrincipalKey(x => new { x.MaDonMua, x.MaKho });
        b.HasOne<ViTri>().WithMany().HasForeignKey(x => x.MaKho);
        b.HasOne<NguoiDung>().WithMany().HasForeignKey(x => x.NguoiLap);
        b.HasOne<NguoiDung>().WithMany().HasForeignKey(x => x.NguoiXacNhan);
        b.HasMany(x => x.ChiTiet).WithOne().HasForeignKey(x => x.MaPhieuNhap);
        b.HasIndex(x => new { x.MaKho, x.NgayNhap });
    }
}

public class ChiTietPhieuNhapConfiguration : IEntityTypeConfiguration<ChiTietPhieuNhap>
{
    public void Configure(EntityTypeBuilder<ChiTietPhieuNhap> b)
    {
        b.ToTable(t =>
        {
            t.Check("HeSo", "\"HeSo\" >= 1");
            t.Check("SoLuong", "\"SoLuongGoc\" > 0 AND \"SoLuongThucNhan\" = \"SoLuongGoc\" * \"HeSo\"");
            t.Check("DonGia", "\"DonGia\" >= 0");
            t.ChiNhan<ChatLuong>(nameof(ChiTietPhieuNhap.ChatLuong));
        });
        b.HasKey(x => new { x.MaPhieuNhap, x.STT });
        b.Property(x => x.MaPhieuNhap).HasMaxLength(Ma);
        b.Property(x => x.MaLo).HasMaxLength(Ma);
        // Không unique: phiếu nhập khác điều chỉnh kiểm kê (hàng thừa) cộng vào lô đã có.
        b.HasIndex(x => x.MaLo);
        b.Property(x => x.TenDonVi).HasMaxLength(Ma);
        b.Property(x => x.SoLuongGoc).SoLuong();
        b.Property(x => x.SoLuongThucNhan).SoLuong();
        b.Property(x => x.DonGia).Tien();
        b.Property(x => x.ThanhTien).Tien()
            .HasComputedColumnSql("round(\"SoLuongGoc\" * \"DonGia\", 2)", stored: true);
        b.HasOne<Lo>().WithMany().HasForeignKey(x => x.MaLo);
    }
}
