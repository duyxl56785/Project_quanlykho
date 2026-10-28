# Sao lưu CSDL quanlykho bằng pg_dump (định dạng custom) chạy trong container, lưu vào backups/quanlykho-<yyyyMMdd-HHmm>.dump
# và chỉ giữ $Giu bản mới nhất. Dùng: powershell -ExecutionPolicy Bypass -File scripts\sao-luu.ps1
param(
    [string]$Container = "quanlykho-db",
    [string]$TenDb = "quanlykho",
    [string]$User = "quanlykho",
    [string]$ThuMuc = (Join-Path $PSScriptRoot "..\backups"),
    [int]$Giu = 14
)
$ErrorActionPreference = "Stop"

New-Item -ItemType Directory -Force $ThuMuc | Out-Null
$ten = "$TenDb-$(Get-Date -Format 'yyyyMMdd-HHmm').dump"
$trongContainer = "/tmp/$ten"

# Ghi tệp trong container rồi docker cp ra ngoài: chuyển dữ liệu nhị phân qua stdout của PowerShell 5.1 sẽ làm hỏng tệp.
docker exec $Container pg_dump -U $User -d $TenDb -Fc -f $trongContainer
if ($LASTEXITCODE -ne 0) { throw "pg_dump lỗi (mã $LASTEXITCODE)." }
docker cp "${Container}:$trongContainer" (Join-Path $ThuMuc $ten)
if ($LASTEXITCODE -ne 0) { throw "docker cp lỗi (mã $LASTEXITCODE)." }
docker exec $Container rm -f $trongContainer | Out-Null

Get-ChildItem $ThuMuc -Filter "$TenDb-*.dump" | Sort-Object LastWriteTime -Descending | Select-Object -Skip $Giu |
    ForEach-Object { Remove-Item $_.FullName -Force -Confirm:$false }

$tep = Get-Item (Join-Path $ThuMuc $ten)
Write-Output "Đã sao lưu: $($tep.FullName) ($([math]::Round($tep.Length / 1KB)) KB)"
