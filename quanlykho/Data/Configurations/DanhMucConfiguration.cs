using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using quanlykho.Domain;
using static quanlykho.Data.Configurations.CauHinhChung;

namespace quanlykho.Data.Configurations;

public class NhomHangConfiguration : IEntityTypeConfiguration<NhomHang>
{
    public void Configure(EntityTypeBuilder<NhomHang> b)
    {
        b.HasKey(x => x.MaNhom);
        b.Property(x => x.MaNhom).HasMaxLength(Ma);
        b.Property(x => x.MaCha).HasMaxLength(Ma);
        b.Property(x => x.Ten).HasMaxLength(Ten);
        b.HasOne<NhomHang>().WithMany().HasForeignKey(x => x.MaCha);
    }
}

public class ThuocTinhConfiguration : IEntityTypeConfiguration<ThuocTinh>
{
    public void Configure(EntityTypeBuilder<ThuocTinh> b)
    {
        b.HasKey(x => new { x.MaNhom, x.Ten });
        b.Property(x => x.MaNhom).HasMaxLength(Ma);
        b.Property(x => x.Ten).HasMaxLength(50);
        b.HasOne<NhomHang>().WithMany().HasForeignKey(x => x.MaNhom);
    }
}

public class SanPhamConfiguration : IEntityTypeConfiguration<SanPham>
{
    public void Configure(EntityTypeBuilder<SanPham> b)
    {
        b.ToTable(t =>
        {
            t.ChiNhan<DieuKienBaoQuan>(nameof(SanPham.DieuKienBaoQuan));
            t.ChiNhan<TrangThaiSuDung>(nameof(SanPham.TrangThai));
        });
        b.HasKey(x => x.MaSanPham);
        b.Property(x => x.MaSanPham).HasMaxLength(Ma);
        b.Property(x => x.Ten).HasMaxLength(Ten);
        b.Property(x => x.DonViTinh).HasMaxLength(Ma);
        b.Property(x => x.MaNhom).HasMaxLength(Ma);
        b.HasOne<NhomHang>().WithMany().HasForeignKey(x => x.MaNhom);
    }
}

public class BienTheConfiguration : IEntityTypeConfiguration<BienThe>
{
    public void Configure(EntityTypeBuilder<BienThe> b)
    {
        b.ToTable(t =>
        {
            t.Check("KichThuoc", "\"DaiDongGoi\" > 0 AND \"RongDongGoi\" > 0 AND \"CaoDongGoi\" > 0");
            t.Check("KhoiLuong", "\"KhoiLuong\" > 0");
            t.ChiNhan<TrangThaiSuDung>(nameof(BienThe.TrangThai));
        });
        b.HasKey(x => x.MaBienThe);
        b.Property(x => x.MaBienThe).HasMaxLength(Ma);
        b.Property(x => x.MaSanPham).HasMaxLength(Ma);
        b.Property(x => x.ThuocTinh)
            .HasColumnType("jsonb")
            .HasConversion(
                d => JsonSerializer.Serialize(d, (JsonSerializerOptions?)null),
                s => JsonSerializer.Deserialize<Dictionary<string, string>>(s, (JsonSerializerOptions?)null) ?? new())
            .Metadata.SetValueComparer(new ValueComparer<Dictionary<string, string>>(
                (a, c) => a!.Count == c!.Count && !a.Except(c).Any(),
                d => d.Aggregate(0, (h, kv) => h ^ HashCode.Combine(kv.Key, kv.Value)),
                d => new Dictionary<string, string>(d)));
        b.Property(x => x.DaiDongGoi).KichThuoc();
        b.Property(x => x.RongDongGoi).KichThuoc();
        b.Property(x => x.CaoDongGoi).KichThuoc();
        b.Property(x => x.KhoiLuong).KhoiLuong();
        b.Property(x => x.MaVach).HasMaxLength(MaVach);
        b.HasIndex(x => x.MaVach).IsUnique();
        // Mỗi tổ hợp thuộc tính của một sản phẩm là một biến thể duy nhất.
        b.HasIndex(x => new { x.MaSanPham, x.ThuocTinh }).IsUnique();
        b.HasOne(x => x.SanPham).WithMany().HasForeignKey(x => x.MaSanPham);
    }
}

public class DonViQuyDoiConfiguration : IEntityTypeConfiguration<DonViQuyDoi>
{
    public void Configure(EntityTypeBuilder<DonViQuyDoi> b)
    {
        b.ToTable(t =>
        {
            t.Check("HeSo", "\"HeSo\" > 1");
            t.ChiNhan<TrangThaiSuDung>(nameof(DonViQuyDoi.TrangThai));
        });
        b.HasKey(x => new { x.MaBienThe, x.TenDonVi });
        b.Property(x => x.MaBienThe).HasMaxLength(Ma);
        b.Property(x => x.TenDonVi).HasMaxLength(Ma);
        b.Property(x => x.MaVach).HasMaxLength(MaVach);
        b.HasIndex(x => x.MaVach).IsUnique();
        b.HasOne<BienThe>().WithMany().HasForeignKey(x => x.MaBienThe);
    }
}

public class NhaCungCapConfiguration : IEntityTypeConfiguration<NhaCungCap>
{
    public void Configure(EntityTypeBuilder<NhaCungCap> b)
    {
        b.ToTable(t => t.ChiNhan<TrangThaiSuDung>(nameof(NhaCungCap.TrangThai)));
        b.HasKey(x => x.MaNhaCungCap);
        b.Property(x => x.MaNhaCungCap).HasMaxLength(Ma);
        b.Property(x => x.Ten).HasMaxLength(Ten);
        b.Property(x => x.DiaChi).HasMaxLength(300);
        b.Property(x => x.NguoiLienHe).HasMaxLength(100);
        b.Property(x => x.SoDienThoai).HasMaxLength(Ma);
    }
}

public class NoiNhanConfiguration : IEntityTypeConfiguration<NoiNhan>
{
    public void Configure(EntityTypeBuilder<NoiNhan> b)
    {
        b.ToTable(t => t.ChiNhan<TrangThaiSuDung>(nameof(NoiNhan.TrangThai)));
        b.HasKey(x => x.MaNoiNhan);
        b.Property(x => x.MaNoiNhan).HasMaxLength(Ma);
        b.Property(x => x.Ten).HasMaxLength(Ten);
        b.Property(x => x.DiaChi).HasMaxLength(300);
        b.Property(x => x.NguoiLienHe).HasMaxLength(100);
        b.Property(x => x.SoDienThoai).HasMaxLength(Ma);
    }
}

public class MucTonConfiguration : IEntityTypeConfiguration<MucTon>
{
    public void Configure(EntityTypeBuilder<MucTon> b)
    {
        b.ToTable(t =>
        {
            t.Check("ToiThieu", "\"ToiThieu\" IS NULL OR \"ToiThieu\" >= 0");
            t.Check("ToiDa", "\"ToiDa\" IS NULL OR \"ToiThieu\" IS NULL OR \"ToiDa\" > \"ToiThieu\"");
            t.Check("ThoiGianChoHang", "\"ThoiGianChoHang\" >= 0");
        });
        b.HasKey(x => new { x.MaBienThe, x.MaKho });
        b.Property(x => x.MaBienThe).HasMaxLength(Ma);
        b.Property(x => x.MaKho).HasMaxLength(MaViTri);
        b.Property(x => x.ToiThieu).SoLuong();
        b.Property(x => x.ToiDa).SoLuong();
        b.HasOne<BienThe>().WithMany().HasForeignKey(x => x.MaBienThe);
        b.HasOne<ViTri>().WithMany().HasForeignKey(x => x.MaKho);
    }
}
