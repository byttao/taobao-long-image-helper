$ErrorActionPreference = "Stop"

$publishDir = Join-Path $PSScriptRoot "publish-win-x64"

dotnet restore $PSScriptRoot
dotnet publish $PSScriptRoot `
  --configuration Release `
  --runtime win-x64 `
  --self-contained false `
  --output $publishDir

Copy-Item -LiteralPath (Join-Path $PSScriptRoot "README.md") -Destination (Join-Path $publishDir "README.md") -Force

$runtimeDownloads = Join-Path $publishDir "downloads"
if (Test-Path -LiteralPath $runtimeDownloads) {
  Remove-Item -LiteralPath $runtimeDownloads -Recurse -Force
}

Write-Host "已发布到：$publishDir"
Write-Host "运行：$publishDir\TaobaoLongImageHelper.exe"
Write-Host "部署到其他 Windows 设备时，需要目标设备已安装 .NET 8 Desktop Runtime 和 Google Chrome。"
