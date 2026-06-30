param([string]$ClarionRoot = "D:\Clarion11")

$ErrorActionPreference = "Continue"
$projectDir = $PSScriptRoot
$addinName  = "ClarionWindowFormatter"
$src        = Join-Path $projectDir "bin\Debug"

Write-Host "Building $addinName..."

$msbuild = & "${env:ProgramFiles(x86)}\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe" 2>$null
if (-not $msbuild) {
    $msbuild = (Get-Command msbuild -ErrorAction SilentlyContinue)?.Source
}

& msbuild "$projectDir\$addinName.csproj" /p:Configuration=Debug /p:ClarionRoot="$ClarionRoot" /v:minimal
if ($LASTEXITCODE -ne 0) { Write-Error "Build failed"; exit 1 }

# Generar .addin desde template
$template  = Join-Path $projectDir "$addinName.addin.template"
$addinOut  = Join-Path $src "$addinName.addin"
Copy-Item $template $addinOut -Force

# Deploy
$dest = Join-Path $ClarionRoot "accessory\addins\$addinName"
if (-not (Test-Path $ClarionRoot)) { Write-Warning "Clarion root not found: $ClarionRoot"; exit 0 }
if (-not (Test-Path $dest)) { New-Item -ItemType Directory -Path $dest -Force | Out-Null }

$files = @("$addinName.dll", "$addinName.pdb", "$addinName.addin")
foreach ($f in $files) {
    $s = Join-Path $src $f
    if (Test-Path $s) {
        try { Copy-Item $s $dest -Force; Write-Host "  OK  $f" }
        catch { Write-Warning "  FAIL $f - $($_.Exception.Message)" }
    }
}

Write-Host "Deploy completo -> $dest"
