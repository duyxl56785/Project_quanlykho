using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using quanlykho.Domain;
using static quanlykho.Data.Configurations.CauHinhChung;

namespace quanlykho.Data.Configurations;

public class LoConfiguration : IEntityTypeConfiguration<Lo>
{
    public void Configure(EntityTypeBuilder<Lo> b)
    {
        b.HasKey(x => x.MaLo);
        b.Property(x => x.MaLo).HasMaxLength(Ma);
        b.Property(x => x.MaBienThe).HasMaxLength(Ma);
        b.HasOne<BienThe>().WithMany().HasForeignKey(x => x.MaBienThe);
        b.HasIndex(x => new { x.MaBienThe, x.HanDung });
    }
}

public class TonKhoConfiguration : IEntityTypeConfiguration<TonKho>
{
    public void Configure(EntityTypeBuilder<TonKho> b)
    {
        // Lưới an toàn cuối: tồn không âm, số đã giữ không vượt tồn.
        b.ToTable(t => t.Check("SoLuong", "\"SoLuong\" >= 0 AND \"SoLuongDaGiu\" >= 0 AND \"SoLuongDaGiu\" <= \"SoLuong\""));
        b.HasKey(x => new { x.MaLo, x.MaViTri });
        b.Property(x => x.MaLo).HasMaxLength(Ma);
        b.Property(x => x.MaViTri).HasMaxLength(MaViTri);
        b.Property(x => x.SoLuong).SoLuong();
        b.Property(x => x.SoLuongDaGiu).SoLuong();
        b.HasOne<Lo>().WithMany().HasForeignKey(x => x.MaLo);
        b.HasOne<ViTri>().WithMany().HasForeignKey(x => x.MaViTri);
        b.HasIndex(x => x.MaViTri);
    }
}

public class SoBienDongConfiguration : IEntityTypeConfiguration<SoBienDong>
{
    public void Configure(EntityTypeBuilder<SoBienDong> b)
    {
        b.ToTable(t => t.Check("SoLuongThayDoi", "\"SoLuongThayDoi\" <> 0"));
        b.HasKey(x => x.MaBienDong);
        b.Property(x => x.MaBienDong).UseIdentityAlwaysColumn();
        b.Property(x => x.MaLo).HasMaxLength(Ma);
        b.Property(x => x.MaViTri).HasMaxLength(MaViTri);
        b.Property(x => x.SoLuongThayDoi).SoLuong();
        b.Property(x => x.LoaiChungTu).HasMaxLength(30);
        b.Property(x => x.MaChungTu).HasMaxLength(Ma);
        b.Property(x => x.MaKhoaChongLap).HasMaxLength(MaVach);
        b.HasIndex(x => x.MaKhoaChongLap).IsUnique();
        b.Property(x => x.NguoiThucHien).HasMaxLength(Ma);
        b.HasOne<Lo>().WithMany().HasForeignKey(x => x.MaLo);
        b.HasOne<ViTri>().WithMany().HasForeignKey(x => x.MaViTri);
        b.HasOne<NguoiDung>().WithMany().HasForeignKey(x => x.NguoiThucHien);
        b.HasIndex(x => new { x.MaLo, x.ThoiDiem });
        b.HasIndex(x => x.MaChungTu);
    }
}
