# RemoveAppData.ps1
$AppDataFolder = Join-Path $env:LOCALAPPDATA "TimerWithKeyBindings"

if (Test-Path $AppDataFolder) {
    Remove-Item -Recurse -Force $AppDataFolder
}