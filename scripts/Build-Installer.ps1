param(
    [ValidatePattern('^\d+\.\d+\.\d+$')]
    [string]$Version = '1.0.1',
    [string]$IsccPath,
    [switch]$SkipPublish
)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
if (-not $IsccPath) {
    $candidates = @(
        (Join-Path $projectRoot 'artifacts/build-tools/inno/ISCC.exe'),
        "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
        "$env:ProgramFiles\Inno Setup 7\ISCC.exe"
    )
    $IsccPath = $candidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
    if (-not $IsccPath) {
        $command = Get-Command ISCC.exe -ErrorAction SilentlyContinue
        if ($command) { $IsccPath = $command.Source }
    }
}
if (-not $IsccPath -or -not (Test-Path -LiteralPath $IsccPath)) {
    throw 'Install Inno Setup from https://jrsoftware.org/isdl.php or pass -IsccPath.'
}
if (-not $SkipPublish) {
    & dotnet publish (Join-Path $projectRoot 'src/PomodoroTaskbar.Windows') -c Release -r win-x64 --self-contained true "-p:Version=$Version" -o (Join-Path $projectRoot 'artifacts/nekodoro-portable')
    if ($LASTEXITCODE -ne 0) { throw 'Publish failed.' }
}
if (-not (Test-Path (Join-Path $projectRoot 'artifacts/nekodoro-portable/coreclr.dll')) -or
    -not (Test-Path (Join-Path $projectRoot 'artifacts/nekodoro-portable/Nekodoro.exe'))) {
    throw 'A self-contained publish is required in artifacts/nekodoro-portable.'
}
& $IsccPath "/DAppVersion=$Version" (Join-Path $PSScriptRoot 'Nekodoro.iss')
if ($LASTEXITCODE -ne 0) { throw 'Installer compilation failed.' }
Get-Item (Join-Path $projectRoot "artifacts/Nekodoro-Setup-$Version-win-x64.exe")
