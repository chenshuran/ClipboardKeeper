# File: build.ps1
# Purpose: Builds Clipboard Keeper with the .NET Framework C# compiler, embeds generated icons, and writes the executable to bin.

param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$outDir = Join-Path $root "bin"
$objDir = Join-Path $root "obj"
$outFile = Join-Path $outDir "ClipboardKeeper.exe"
$buildFile = Join-Path $objDir "ClipboardKeeper.exe"
$sources = Get-ChildItem -Path $root -Filter "*.cs" | Sort-Object FullName | Select-Object -ExpandProperty FullName
$assemblyInfo = Join-Path $objDir "GeneratedAssemblyInfo.cs"
$manifestFile = Join-Path $root "app.manifest"
$iconScript = Join-Path $root "generate-icon.ps1"
$uiIconScript = Join-Path $root "generate-ui-icons.ps1"
$iconFile = Join-Path $root "assets\ClipboardKeeper.ico"
$uiIconDir = Join-Path $root "assets\ui-icons"

$candidates = @(
    (Join-Path $env:WINDIR "Microsoft.NET\Framework64\v4.0.30319\csc.exe"),
    (Join-Path $env:WINDIR "Microsoft.NET\Framework\v4.0.30319\csc.exe")
)

$csc = $candidates | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $csc) {
    throw "Could not find the .NET Framework C# compiler."
}

New-Item -ItemType Directory -Force $outDir | Out-Null
New-Item -ItemType Directory -Force $objDir | Out-Null

$running = Get-Process -ErrorAction SilentlyContinue | Where-Object {
    try {
        $_.Path -and ([System.IO.Path]::GetFullPath($_.Path) -ieq [System.IO.Path]::GetFullPath($outFile))
    }
    catch {
        $false
    }
}

if ($running) {
    $ids = ($running | Select-Object -ExpandProperty Id) -join ", "
    throw "ClipboardKeeper.exe is still running (PID: $ids). Please exit it from the tray before building."
}

if (Test-Path $buildFile) {
    Remove-Item -LiteralPath $buildFile -Force
}

& $iconScript -OutputPath $iconFile
& $uiIconScript -OutputDirectory $uiIconDir

$now = Get-Date
$fileVersion = "1.0.$($now.DayOfYear).$($now.ToString('HHmm'))"
@"
using System.Reflection;

[assembly: AssemblyTitle("Clipboard Keeper")]
[assembly: AssemblyDescription("Local clipboard history keeper for text and images")]
[assembly: AssemblyCompany("Ryan Chen")]
[assembly: AssemblyProduct("Clipboard Keeper")]
[assembly: AssemblyCopyright("Copyright Ryan Chen")]
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("$fileVersion")]
[assembly: AssemblyInformationalVersion("$fileVersion")]
"@ | Set-Content -LiteralPath $assemblyInfo -Encoding ASCII

$resourceArgs = Get-ChildItem -Path $uiIconDir -Filter *.png | Sort-Object Name | ForEach-Object {
    "/resource:$($_.FullName),ClipboardKeeper.UiIcons.$($_.BaseName).png"
}

$compilerArgs = @(
    "/nologo",
    "/target:winexe",
    "/platform:anycpu",
    "/optimize+",
    "/win32icon:$iconFile",
    "/win32manifest:$manifestFile",
    "/out:$buildFile",
    "/reference:System.dll",
    "/reference:System.Core.dll",
    "/reference:System.Drawing.dll",
    "/reference:System.Security.dll",
    "/reference:System.Windows.Forms.dll",
    "/reference:System.Xml.dll"
)

$compilerArgs += $resourceArgs
$compilerArgs += $sources
$compilerArgs += $assemblyInfo

& $csc @compilerArgs

if ($LASTEXITCODE -ne 0) {
    throw "Compilation failed with exit code $LASTEXITCODE."
}

if (Test-Path $outFile) {
    Remove-Item -LiteralPath $outFile -Force
}

Move-Item -LiteralPath $buildFile -Destination $outFile -Force

Write-Host "Built $outFile"
