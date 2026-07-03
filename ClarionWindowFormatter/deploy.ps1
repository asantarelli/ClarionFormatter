param([string]$ClarionRoot = "D:\Clarion11")

$ErrorActionPreference = "Continue"
$projectDir = $PSScriptRoot
$addinName  = "ClarionWindowFormatter"
$src        = Join-Path $projectDir "bin\Debug"
$template   = Join-Path $projectDir "$addinName.addin.template"
$dest       = Join-Path $ClarionRoot "accessory\addins\$addinName"

# Generar .addin desde template ANTES de copiar
if (-not (Test-Path $template)) { Write-Error "Template no encontrado: $template"; exit 1 }
$addinOut = Join-Path $dest "$addinName.addin"

if (-not (Test-Path $ClarionRoot)) { Write-Warning "Clarion root no encontrado: $ClarionRoot"; exit 0 }
if (-not (Test-Path $dest)) { New-Item -ItemType Directory -Path $dest -Force | Out-Null }

# Copiar DLL y PDB
foreach ($f in @("$addinName.dll", "$addinName.pdb")) {
    $s = Join-Path $src $f
    if (Test-Path $s) {
        try { Copy-Item $s $dest -Force; Write-Host "  OK  $f" }
        catch { Write-Warning "  FAIL $f - $($_.Exception.Message)" }
    } else {
        Write-Warning "  NO ENCONTRADO: $f (compilar primero)"
    }
}

# Copiar .addin directamente desde template al destino (no pasa por bin\Debug)
try { Copy-Item $template $addinOut -Force; Write-Host "  OK  $addinName.addin (desde template)" }
catch { Write-Warning "  FAIL $addinName.addin - $($_.Exception.Message)" }

Write-Host "Deploy completo -> $dest"
