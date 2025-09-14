# Test Scheduled Warmup Service
# Script này giúp test và monitor scheduled warmup service

param(
    [string]$BaseUrl = "https://localhost:7001",
    [switch]$TriggerManual,
    [switch]$CheckHealth,
    [switch]$MonitorLogs
)

Write-Host "=== Cardano MainAPI Scheduled Warmup Test ===" -ForegroundColor Green
Write-Host "Base URL: $BaseUrl" -ForegroundColor Yellow
Write-Host ""

# Function to make HTTP requests
function Invoke-ApiRequest {
    param(
        [string]$Url,
        [string]$Method = "GET"
    )
    
    try {
        $response = Invoke-RestMethod -Uri $Url -Method $Method -TimeoutSec 30
        return @{
            Success = $true
            Data = $response
        }
    }
    catch {
        return @{
            Success = $false
            Error = $_.Exception.Message
        }
    }
}

# Function to check service health
function Test-ServiceHealth {
    Write-Host "Testing service health..." -ForegroundColor Cyan
    
    $healthUrl = "$BaseUrl/api/warmup/health"
    $result = Invoke-ApiRequest -Url $healthUrl
    
    if ($result.Success) {
        Write-Host "✓ Service health check passed" -ForegroundColor Green
        $result.Data | ConvertTo-Json -Depth 3
    }
    else {
        Write-Host "✗ Service health check failed: $($result.Error)" -ForegroundColor Red
    }
    Write-Host ""
}

# Function to trigger manual warmup
function Start-ManualWarmup {
    Write-Host "Triggering manual warmup..." -ForegroundColor Cyan
    
    $warmupUrl = "$BaseUrl/api/warmup/trigger"
    $result = Invoke-ApiRequest -Url $warmupUrl -Method "POST"
    
    if ($result.Success) {
        Write-Host "✓ Manual warmup triggered successfully" -ForegroundColor Green
        
        $data = $result.Data
        Write-Host "Warmup Time: $($data.data.warmupTime)" -ForegroundColor Yellow
        Write-Host "Total Endpoints: $($data.data.totalEndpoints)" -ForegroundColor Yellow
        Write-Host "Successful: $($data.data.successfulEndpoints)" -ForegroundColor Green
        Write-Host "Failed: $($data.data.failedEndpoints)" -ForegroundColor Red
        Write-Host "Status: $($data.data.status)" -ForegroundColor Yellow
        
        if ($data.data.failedEndpoints -gt 0) {
            Write-Host "`nFailed endpoints:" -ForegroundColor Red
            $data.data.results | Where-Object { $_ -like "*FAILED*" } | ForEach-Object {
                Write-Host "  $_" -ForegroundColor Red
            }
        }
    }
    else {
        Write-Host "✗ Manual warmup failed: $($result.Error)" -ForegroundColor Red
    }
    Write-Host ""
}

# Function to monitor logs
function Watch-Logs {
    Write-Host "Monitoring logs for warmup activities..." -ForegroundColor Cyan
    Write-Host "Press Ctrl+C to stop monitoring" -ForegroundColor Yellow
    Write-Host ""
    
    $logFile = "Logs/log-$(Get-Date -Format 'yyyy-MM-dd').txt"
    
    if (Test-Path $logFile) {
        Get-Content $logFile -Wait -Tail 10 | ForEach-Object {
            if ($_ -like "*warmup*" -or $_ -like "*Warmup*") {
                Write-Host $_ -ForegroundColor Magenta
            }
        }
    }
    else {
        Write-Host "Log file not found: $logFile" -ForegroundColor Red
    }
}

# Function to check configuration
function Test-Configuration {
    Write-Host "Checking scheduled warmup configuration..." -ForegroundColor Cyan
    
    $configUrl = "$BaseUrl/api/warmup"
    $result = Invoke-ApiRequest -Url $configUrl
    
    if ($result.Success) {
        Write-Host "✓ Configuration check passed" -ForegroundColor Green
        Write-Host "Service Status: $($result.Data.data.status)" -ForegroundColor Yellow
    }
    else {
        Write-Host "✗ Configuration check failed: $($result.Error)" -ForegroundColor Red
    }
    Write-Host ""
}

# Function to show next scheduled run
function Show-NextScheduledRun {
    Write-Host "Calculating next scheduled warmup..." -ForegroundColor Cyan
    
    $now = Get-Date -AsUTC
    $nextRun = $now.Date.AddDays(1).AddMinutes(1) # 00:01 UTC tomorrow
    
    if ($now.TimeOfDay -gt [TimeSpan]::FromMinutes(1)) {
        $nextRun = $now.Date.AddDays(1).AddMinutes(1)
    }
    else {
        $nextRun = $now.Date.AddMinutes(1)
    }
    
    $timeUntilNext = $nextRun - $now
    
    Write-Host "Current UTC Time: $($now.ToString('yyyy-MM-dd HH:mm:ss'))" -ForegroundColor Yellow
    Write-Host "Next Scheduled Run: $($nextRun.ToString('yyyy-MM-dd HH:mm:ss'))" -ForegroundColor Green
    Write-Host "Time Until Next Run: $($timeUntilNext.ToString('hh\:mm\:ss'))" -ForegroundColor Cyan
    Write-Host ""
}

# Main execution
try {
    # Show next scheduled run
    Show-NextScheduledRun
    
    # Check configuration
    Test-Configuration
    
    # Check health if requested
    if ($CheckHealth) {
        Test-ServiceHealth
    }
    
    # Trigger manual warmup if requested
    if ($TriggerManual) {
        Start-ManualWarmup
    }
    
    # Monitor logs if requested
    if ($MonitorLogs) {
        Watch-Logs
    }
    
    # If no specific action requested, show menu
    if (-not $TriggerManual -and -not $CheckHealth -and -not $MonitorLogs) {
        Write-Host "Available actions:" -ForegroundColor Cyan
        Write-Host "  -TriggerManual : Trigger manual warmup" -ForegroundColor White
        Write-Host "  -CheckHealth   : Check service health" -ForegroundColor White
        Write-Host "  -MonitorLogs   : Monitor warmup logs" -ForegroundColor White
        Write-Host ""
        Write-Host "Example: .\test-scheduled-warmup.ps1 -TriggerManual -CheckHealth" -ForegroundColor Yellow
    }
}
catch {
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
}
finally {
    Write-Host "=== Test completed ===" -ForegroundColor Green
} 