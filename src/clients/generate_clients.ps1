$ErrorActionPreference = 'Continue'
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
Set-Location $ScriptDir

Clear-Host
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host "                NSwag Client Code Generation                  " -ForegroundColor Cyan
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "[*] Executing: " -ForegroundColor DarkGray -NoNewline
Write-Host "nswag run nswag.json" -ForegroundColor White
Write-Host ""

try {
    # Run the command and capture both stdout and stderr
    $output = nswag run nswag.json 2>&1

    # Check if the command was successful
    if ($LASTEXITCODE -eq 0) {
        foreach ($line in $output) {
            Write-Host "  $line" -ForegroundColor DarkGreen
        }
        Write-Host ""
        Write-Host "============================================================" -ForegroundColor Cyan
        Write-Host "[ SUCCESS ] Clients generated successfully!" -ForegroundColor Green
    } else {
        foreach ($line in $output) {
            Write-Host "  $line" -ForegroundColor Red
        }
        Write-Host ""
        Write-Host "============================================================" -ForegroundColor Cyan
        Write-Host "[ FAILED ] Error generating clients." -ForegroundColor Red
        Write-Host "Make sure the NSwag CLI tool is installed globally." -ForegroundColor Yellow
    }
} catch {
    Write-Host "============================================================" -ForegroundColor Cyan
    Write-Host "[ FAILED ] Could not execute nswag command." -ForegroundColor Red
    Write-Host ""
    Write-Host $_ -ForegroundColor Red
    Write-Host ""
    Write-Host "Make sure the NSwag CLI tool is installed globally." -ForegroundColor Yellow
    Write-Host "(e.g., npm install -g nswag or dotnet tool install -g NSwag.ConsoleCore)" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Press any key to exit..." -ForegroundColor DarkGray
$null = $Host.UI.RawUI.ReadKey('NoEcho,IncludeKeyDown')
