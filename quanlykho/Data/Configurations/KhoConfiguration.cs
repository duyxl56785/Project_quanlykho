using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using quanlykho.Domain;
using static quanlykho.Data.Configurations.CauHinhChung;

namespace quanlykho.Data.Configurations;

public class ViTriConfiguration : IEntityTypeConfiguration<ViTri>
{
    public void Configure(EntityTypeBuilder<ViTri> b)
    {
        b.ToTable(t =>
        {
            t.Check("Cap", "\"Cap\" BETWEEN 1 AND 7");
            t.Check("MaCha", "(\"Cap\" = 1) = (\"MaCha\" IS NULL)");
            t.Check("MaKho", "\"Cap\" <> 1 OR \"MaKho\" = \"MaViTri\"");
            // Ô bắt buộc kích thước trong và tải trọng; mọi kích thước, tải trọng có khai báo đều dương.
            t.Check("SucChuaO", "\"Cap\" <> 7 OR (\"Dai\" IS NOT NULL AND \"Rong\" IS NOT NULL AND \"Cao\" IS NOT NULL AND \"TaiTrongToiDa\" IS NOT NULL)");
            t.Check("KichThuocDuong", "COALESCE(\"Dai\", 1) > 0 AND COALESCE(\"Rong\", 1) > 0 AND COALESCE(\"Cao\", 1) > 0 AND COALESCE(\"TaiTrongToiDa\", 1) > 0");
            t.Check("HeSoLapDay", "\"HeSoLapDay\" IS NULL OR (\"HeSoLapDay\" > 0 AND \"HeSoLapDay\" <= 1)");
            t.ChiNhan<LoaiKhu>(nameof(ViTri.LoaiKhu));
            t.ChiNhan<DieuKienBaoQuan>(nameof(ViTri.DieuKienBaoQuan));
        });
        b.HasKey(x => x.MaViTri);
        b.Property(x => x.MaViTri).HasMaxLength(MaViTri);
        b.Property(x => x.MaCha).HasMaxLength(MaViTri);
        b.Property(x => x.MaKho).HasMaxLength(MaViTri);
        b.Property(x => x.Ten).HasMaxLength(Ten);
        b.Property(x => x.TenDayDu).HasMaxLength(1000);
        b.Property(x => x.Dai).KichThuoc();
        b.Property(x => x.Rong).KichThuoc();
        b.Property(x => x.Cao).KichThuoc();
        b.Property(x => x.TaiTrongToiDa).KhoiLuong();
        b.Property(x => x.MaPhieuKiemKe).HasMaxLength(Ma);
        b.Property(x => x.DiaChi).HasMaxLength(300);
        b.Property(x => x.LoaiHangLuuTru).HasMaxLength(Ten);
        b.Property(x => x.HeSoLapDay).HasPrecision(4, 3);
        b.HasOne<ViTri>().WithMany().HasForeignKey(x => x.MaCha);
        b.HasOne<ViTri>().WithMany().HasForeignKey(x => x.MaKho);
        b.HasIndex(x => new { x.MaKho, x.LoaiKhu });
        // Mỗi kho chỉ một ô mặc định cho mỗi loại khu.
        b.HasIndex(x => new { x.MaKho, x.LoaiKhu }, "IX_ViTri_OMacDinh").IsUnique().HasFilter("\"LaOMacDinh\"");
    }
}

public class NguoiDungConfiguration : IEntityTypeConfiguration<NguoiDung>
{
    public void Configure(EntityTypeBuilder<NguoiDung> b)
    {
        b.ToTable(t =>
        {
            t.ChiNhan<VaiTro>(nameof(NguoiDung.VaiTro));
            t.ChiNhan<TrangThaiTaiKhoan>(nameof(NguoiDung.TrangThai));
            t.Check("SoLanDangNhapSai", "\"SoLanDangNhapSai\" >= 0");
            t.Check("Email", "\"Email\" = lower(\"Email\")");
        });
        b.HasKey(x => x.MaNguoiDung);
        b.Property(x => x.MaNguoiDung).HasMaxLength(Ma);
        b.Property(x => x.Ten).HasMaxLength(100);
        b.Property(x => x.Email).HasMaxLength(Ten);
        b.HasIndex(x => x.Email).IsUnique();
        b.Property(x => x.SoDienThoai).HasMaxLength(Ma);
        b.Property(x => x.MatKhauBam).HasMaxLength(Ten);
        b.Property(x => x.SecurityStamp).HasMaxLength(MaVach);
    }
}

public class NguoiDungKhoConfiguration : IEntityTypeConfiguration<NguoiDungKho>
{
    public void Configure(EntityTypeBuilder<NguoiDungKho> b)
    {
        b.HasKey(x => new { x.MaNguoiDung, x.MaKho });
        b.Property(x => x.MaNguoiDung).HasMaxLength(Ma);
        b.Property(x => x.MaKho).HasMaxLength(MaViTri);
        b.HasOne<NguoiDung>().WithMany().HasForeignKey(x => x.MaNguoiDung);
        b.HasOne<ViTri>().WithMany().HasForeignKey(x => x.MaKho);
    }
}

public class NhatKyConfiguration : IEntityTypeConfiguration<NhatKy>
{
    public void Configure(EntityTypeBuilder<NhatKy> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).UseIdentityAlwaysColumn();
        b.Property(x => x.MaNguoiDung).HasMaxLength(Ma);
        b.Property(x => x.MaKho).HasMaxLength(MaViTri);
        b.Property(x => x.LoaiThaoTac).HasMaxLength(50);
        b.Property(x => x.DoiTuong).HasMaxLength(50);
        b.Property(x => x.MaDoiTuong).HasMaxLength(MaViTri);
        b.Property(x => x.ChiTiet).HasColumnType("jsonb");
        b.HasOne<NguoiDung>().WithMany().HasForeignKey(x => x.MaNguoiDung);
        b.HasIndex(x => x.ThoiDiem);
    }
}
