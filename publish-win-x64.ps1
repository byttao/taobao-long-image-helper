$ErrorActionPreference = "Stop"

$appName = "淘宝商品长图提取工具"
$version = "v1.12.0"
$publishRoot = Join-Path $PSScriptRoot "publish-win-x64"
$appDir = Join-Path $publishRoot $appName
$zipPath = Join-Path $PSScriptRoot "$appName-$version-win-x64.zip"

$resolvedScriptRoot = [System.IO.Path]::GetFullPath($PSScriptRoot)
$resolvedPublishRoot = [System.IO.Path]::GetFullPath($publishRoot)
if (-not $resolvedPublishRoot.StartsWith($resolvedScriptRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
  throw "发布目录不在项目目录内，已停止。"
}

if (Test-Path -LiteralPath $publishRoot) {
  Remove-Item -LiteralPath $publishRoot -Recurse -Force
}

dotnet restore $PSScriptRoot
dotnet publish $PSScriptRoot `
  --configuration Release `
  --runtime win-x64 `
  --self-contained false `
  --output $appDir

Copy-Item -LiteralPath (Join-Path $PSScriptRoot "README.md") -Destination (Join-Path $appDir "README.md") -Force

$runtimeDownloads = Join-Path $appDir "downloads"
if (Test-Path -LiteralPath $runtimeDownloads) {
  Remove-Item -LiteralPath $runtimeDownloads -Recurse -Force
}

if (Test-Path -LiteralPath $zipPath) {
  Remove-Item -LiteralPath $zipPath -Force
}

Compress-Archive -Path $appDir -DestinationPath $zipPath -Force

Write-Host "已发布到：$appDir"
Write-Host "运行：$appDir\$appName.exe"
Write-Host "压缩包：$zipPath"
Write-Host "部署到其他 Windows 设备时，需要目标设备已安装 .NET 8 Desktop Runtime 和 Google Chrome。"
