# Frees dev ports 5136/5137 (Kestrel) then builds the API. Use when MSB3021/MSB3027 locks occur.
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
Set-Location $repoRoot

foreach ($port in @(5136, 5137)) {
    Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue | ForEach-Object {
        Stop-Process -Id $_.OwningProcess -Force -ErrorAction SilentlyContinue
    }
}
Stop-Process -Name 'IIS.Api' -Force -ErrorAction SilentlyContinue
Start-Sleep -Milliseconds 400

dotnet build @args 'src/IIS.Api/IIS.Api.csproj'
