using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using quanlykho.Domain;
using static quanlykho.Data.Configurations.CauHinhChung;

namespace quanlykho.Data.Configurations;

public class PhieuKiemKeConfiguration : IEntityTypeConfiguration<PhieuKiemKe>
{
    public void Configure(EntityTypeBuilder<PhieuKiemKe> b)
    {
        b.ToTable(t =>
        {
            t.ChiNhan<TrangThaiKiemKe>(nameof(PhieuKiemKe.TrangThai));
            t.ChiNhan<PhamViKiemKe>(nameof(PhieuKiemKe.PhamVi));
        });
        b.HasKey(x => x.MaPhieu);
        b.Property(x => x.MaPhieu).HasMaxLength(Ma);
        b.Property(x => x.MaKho).HasMaxLength(MaViTri);
        b.Property(x => x.MucDich).HasMaxLength(500);
        b.Property(x => x.NguoiLap).HasMaxLength(Ma);
        b.HasOne<ViTri>().WithMany().HasForeignKey(x => x.MaKho);
        b.HasOne<NguoiDung>().WithMany().HasForeignKey(x => x.NguoiLap);
        b.HasMany(x => x.ThanhVien).WithOne().HasForeignKey(x => x.MaPhieu);
        b.HasMany(x => x.ChiTiet).WithOne().HasForeignKey(x => x.MaPhieu);
        b.HasIndex(x => new { x.MaKho, x.TrangThai });
    }
}

public class ThanhVienKiemKeConfiguration : IEntityTypeConfiguration<ThanhVienKiemKe>
{
    public void Configure(EntityTypeBuilder<ThanhVienKiemKe> b)
    {
        b.HasKey(x => new { x.MaPhieu, x.MaNguoiDung });
        b.Property(x => x.MaPhieu).HasMaxLength(Ma);
        b.Property(x => x.MaNguoiDung).HasMaxLength(Ma);
        b.HasOne<NguoiDung>().WithMany().HasForeignKey(x => x.MaNguoiDung);
    }
}

public class ChiTietKiemKeConfiguration : IEntityTypeConfiguration<ChiTietKiemKe>
{
    public void Configure(EntityTypeBuilder<ChiTietKiemKe> b)
    {
        b.ToTable(t =>
        {
            t.Check("SoLuong", "\"SoLuongSoSach\" >= 0 AND (\"SoLuongThucTe\" IS NULL OR \"SoLuongThucTe\" >= 0)");
            t.ChiNhan<LoaiChenhLech>(nameof(ChiTietKiemKe.LoaiChenhLech));
            t.Check("DaDuyet", "\"NguoiDuyet\" IS NULL OR (\"LoaiChenhLech\" IS NOT NULL AND \"KetLuan\" IS NOT NULL)");
        });
        b.Property(x => x.KetLuan).HasMaxLength(500);
        b.Property(x => x.MaViTriDung).HasMaxLength(MaViTri);
        b.Property(x => x.MaBienTheDung).HasMaxLength(Ma);
        b.Property(x => x.NguoiDuyet).HasMaxLength(Ma);
        b.Property(x => x.MaChungTuXuLy).HasMaxLength(100);
        b.HasOne<ViTri>().WithMany().HasForeignKey(x => x.MaViTriDung);
        b.HasOne<BienThe>().WithMany().HasForeignKey(x => x.MaBienTheDung);
        b.HasOne<NguoiDung>().WithMany().HasForeignKey(x => x.NguoiDuyet);
        b.HasKey(x => new { x.MaPhieu, x.STT });
        b.Property(x => x.MaPhieu).HasMaxLength(Ma);
        b.Property(x => x.MaViTri).HasMaxLength(MaViTri);
        b.Property(x => x.MaLo).HasMaxLength(Ma);
        b.Property(x => x.MaBienThe).HasMaxLength(Ma);
        b.Property(x => x.SoLuongSoSach).SoLuong();
        b.Property(x => x.SoLuongThucTe).SoLuong();
        b.Property(x => x.NguoiDem).HasMaxLength(Ma);
        b.HasOne<ViTri>().WithMany().HasForeignKey(x => x.MaViTri);
        b.HasOne<Lo>().WithMany().HasForeignKey(x => x.MaLo);
        b.HasOne<BienThe>().WithMany().HasForeignKey(x => x.MaBienThe);
        b.HasOne<NguoiDung>().WithMany().HasForeignKey(x => x.NguoiDem);
        // Một (lô, ô) chỉ một dòng trong phiếu; hàng không nhãn lô gom theo biến thể.
        b.HasIndex(x => new { x.MaPhieu, x.MaViTri, x.MaLo, x.MaBienThe }).IsUnique().AreNullsDistinct(false);
    }
}
