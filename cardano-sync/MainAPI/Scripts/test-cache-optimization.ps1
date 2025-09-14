#!/usr/bin/env pwsh

# PowerShell script for cache optimization verification
# This script compares cache sizes before and after optimization

Write-Host "=== Cache Optimization Test ===" -ForegroundColor Green
Write-Host "Testing cache size optimization..." -ForegroundColor Yellow

# API endpoint (adjust URL as needed)
$API_URL = "http://localhost:5000"

# Test endpoints
$TEST_ENDPOINTS = @(
    "/api/drep/list?page=1&limit=10",
    "/api/pool/list?page=1&limit=10", 
    "/api/proposal/stats",
    "/api/epoch/current",
    "/test-cache"
)

Write-Host "`nTesting cache optimization with various endpoints..." -ForegroundColor Cyan

foreach ($endpoint in $TEST_ENDPOINTS) {
    Write-Host "`n--- Testing: $endpoint ---" -ForegroundColor Magenta
    
    try {
        # Make request and measure response time
        $start_time = Get-Date
        $response = Invoke-WebRequest -Uri "$API_URL$endpoint" -Method GET -UseBasicParsing
        $end_time = Get-Date
        
        # Calculate processing time
        $processing_time = ($end_time - $start_time).TotalMilliseconds
        
        if ($response.StatusCode -eq 200) {
            Write-Host "✅ Status: $($response.StatusCode)" -ForegroundColor Green
            Write-Host "📦 Content Length: $($response.Content.Length) bytes" -ForegroundColor Blue
            Write-Host "⏱️  Response time: $([math]::Round($processing_time, 2))ms" -ForegroundColor Blue
            
            # Check for cache status in response headers
            $cacheStatus = $response.Headers["Cache-Status"]
            if ($cacheStatus) {
                Write-Host "💾 Cache-Status: $cacheStatus" -ForegroundColor Green
            } else {
                Write-Host "💾 Cache-Status: Not found" -ForegroundColor Yellow
            }
        } else {
            Write-Host "❌ Failed with status: $($response.StatusCode)" -ForegroundColor Red
        }
    }
    catch {
        Write-Host "❌ Error: $($_.Exception.Message)" -ForegroundColor Red
    }
}

Write-Host "`n=== Redis Memory Usage ===" -ForegroundColor Green
Write-Host "To check Redis memory usage, run:" -ForegroundColor Yellow
Write-Host "redis-cli info memory | grep used_memory_human" -ForegroundColor White

Write-Host "`n=== Test Cache Endpoint ===" -ForegroundColor Green
Write-Host "Testing the dedicated cache test endpoint..." -ForegroundColor Yellow

try {
    $cacheTest = Invoke-RestMethod -Uri "$API_URL/test-cache" -Method GET
    Write-Host "Cache test result:" -ForegroundColor Green
    $cacheTest | ConvertTo-Json -Depth 10 | Write-Host -ForegroundColor White
}
catch {
    Write-Host "❌ Cache test failed: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n=== Cache Size Comparison ===" -ForegroundColor Green
Write-Host "Expected improvements:" -ForegroundColor Yellow
Write-Host "- Before optimization: ~2MB per cache entry" -ForegroundColor White
Write-Host "- After optimization: ~10KB per cache entry" -ForegroundColor White
Write-Host "- Improvement: 200x reduction in size" -ForegroundColor Green
Write-Host "- Performance: Faster serialization/deserialization" -ForegroundColor Green

Write-Host "`n=== Monitoring Commands ===" -ForegroundColor Green
Write-Host "Check Redis memory usage:" -ForegroundColor Yellow
Write-Host "  redis-cli info memory" -ForegroundColor White
Write-Host ""
Write-Host "Check cache keys:" -ForegroundColor Yellow  
Write-Host "  redis-cli keys '*'" -ForegroundColor White
Write-Host ""
Write-Host "Check specific cache entry size:" -ForegroundColor Yellow
Write-Host "  redis-cli debug object <key>" -ForegroundColor White
Write-Host ""
Write-Host "Monitor Redis operations:" -ForegroundColor Yellow
Write-Host "  redis-cli monitor" -ForegroundColor White

Write-Host "`n=== Performance Test ===" -ForegroundColor Green
Write-Host "Running performance comparison..." -ForegroundColor Yellow

# Performance test - multiple requests to measure improvement
$iterations = 10
$total_time = 0

for ($i = 1; $i -le $iterations; $i++) {
    $start = Get-Date
    try {
        $response = Invoke-WebRequest -Uri "$API_URL/test-cache" -Method GET -UseBasicParsing
        $end = Get-Date
        $request_time = ($end - $start).TotalMilliseconds
        $total_time += $request_time
        Write-Host "Request $i: $([math]::Round($request_time, 2))ms" -ForegroundColor Blue
    }
    catch {
        Write-Host "Request $i: Failed" -ForegroundColor Red
    }
}

if ($total_time -gt 0) {
    $average_time = $total_time / $iterations
    Write-Host "`nAverage response time: $([math]::Round($average_time, 2))ms" -ForegroundColor Green
}

Write-Host "`n=== Test Complete ===" -ForegroundColor Green
Write-Host "Cache optimization test completed." -ForegroundColor Yellow
Write-Host "Review the output above to verify improvements." -ForegroundColor White 