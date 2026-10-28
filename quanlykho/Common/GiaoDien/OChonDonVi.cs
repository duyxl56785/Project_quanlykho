namespace quanlykho.Common.GiaoDien;

/// <summary>Một đơn vị của biến thể; đơn vị tính chính có hệ số 1.</summary>
public record DonViChon(string Ten, int HeSo);

/// <summary>Model của partial _OChonDonVi: ô chọn đơn vị trên dòng chứng từ; DonVi[0] là đơn vị tính chính.</summary>
public record OChonDonVi(string Ten, string? Chon, IReadOnlyList<DonViChon> DonVi, string? Id = null);
