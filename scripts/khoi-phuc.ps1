# Khôi phục CSDL quanlykho từ một tệp sao lưu (pg_restore --clean): xóa rồi tạo lại mọi bảng, dữ liệu hiện tại bị thay bằng bản sao lưu.
# Tắt ứng dụng trước khi khôi phục. Dùng: powershell -ExecutionPolicy Bypass -File scripts\khoi-phuc.ps1 backups\quanlykho-20261029-0200.dump
param(
    [Parameter(Mandatory = $true)][string]$Tep,
    [string]$Container = "quanlykho-db",
    [string]$TenDb = "quanlykho",
    [string]$User = "quanlykho",
    # Bỏ hỏi xác nhận (chạy tự động).
    [switch]$KhongHoi
)
$ErrorActionPreference = "Stop"

$tepDay = (Resolve-Path $Tep).Path
if (-not $KhongHoi) {
    $tl = Read-Host "Khôi phục '$tepDay' vào CSDL '$TenDb' trong container '$Container'? Dữ liệu hiện tại sẽ bị ghi đè. Gõ CO để tiếp tục"
    if ($tl -ne "CO") { Write-Output "Đã hủy, không thay đổi gì."; exit 1 }
}

$trongContainer = "/tmp/khoi-phuc.dump"
docker cp $tepDay "${Container}:$trongContainer"
if ($LASTEXITCODE -ne 0) { throw "docker cp lỗi (mã $LASTEXITCODE)." }
# --single-transaction: lỗi giữa chừng thì không thay đổi gì.
docker exec $Container pg_restore -U $User -d $TenDb --clean --if-exists --no-owner --single-transaction $trongContainer
$ma = $LASTEXITCODE
docker exec $Container rm -f $trongContainer | Out-Null
if ($ma -ne 0) { throw "pg_restore lỗi (mã $ma); CSDL giữ nguyên như trước khi khôi phục." }
Write-Output "Đã khôi phục '$TenDb' từ $tepDay."
