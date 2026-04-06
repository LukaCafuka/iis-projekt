# Exit 1 if Kestrel dev ports are in use (API still running). Used by IIS.Api pre-build.
$ErrorActionPreference = 'Stop'
foreach ($port in @(5136, 5137)) {
    if (Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue) {
        Write-Host "Build stopped: port $port is in use (usually IIS.Api). Stop the API with Ctrl+C in that terminal, then build again. Or skip this check: dotnet build -p:SkipApiRunningCheck=true" -ForegroundColor Yellow
        exit 1
    }
}
exit 0
