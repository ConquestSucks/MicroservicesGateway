param(
    [string]$BaseUrl = "http://localhost:5000",
    [switch]$SkipDockerUp
)

Write-Host "=== STEP 0: Start infrastructure (docker-compose) ===" -ForegroundColor Cyan

if (-not $SkipDockerUp) {
    try {
        Write-Host "Running: docker-compose up -d ..." -ForegroundColor Yellow
        docker-compose up -d | Out-Null
        Write-Host "Containers started. Waiting 10 seconds for services to be ready..." -ForegroundColor Yellow
        Start-Sleep -Seconds 10
    } catch {
        Write-Host "Failed to run docker-compose: $($_.Exception.Message)" -ForegroundColor Red
        Write-Host "Make sure Docker Desktop is running and try again." -ForegroundColor Red
        exit 1
    }
} else {
    Write-Host "Skipping docker-compose up (flag -SkipDockerUp is set)." -ForegroundColor Yellow
}

Write-Host "`n=== STEP 1: Get JWT token ===" -ForegroundColor Cyan

try {
    $loginResponse = Invoke-RestMethod -Uri "$BaseUrl/api/auth/login?username=testuser" -Method POST -ErrorAction Stop
    $token = $loginResponse.token
    if (-not $token) {
        Write-Host "Token is empty." -ForegroundColor Red
        exit 1
    }
    Write-Host "Token received." -ForegroundColor Green
} catch {
    Write-Host "Error while getting token: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

$headers = @{ Authorization = "Bearer $token" }

Write-Host "`n=== STEP 2: Aggregated user profile (/api/profile/1) ===" -ForegroundColor Cyan

try {
    $profile = Invoke-RestMethod -Uri "$BaseUrl/api/profile/1" -Headers $headers -ErrorAction Stop
    $profile | ConvertTo-Json -Depth 10
    Write-Host "Aggregated profile received successfully." -ForegroundColor Green
} catch {
    Write-Host "Error while requesting profile: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n=== STEP 3: Caching (two sequential requests to /api/profile/1) ===" -ForegroundColor Cyan

try {
    $sw1 = [System.Diagnostics.Stopwatch]::StartNew()
    Invoke-RestMethod -Uri "$BaseUrl/api/profile/1" -Headers $headers -ErrorAction Stop | Out-Null
    $sw1.Stop()

    $sw2 = [System.Diagnostics.Stopwatch]::StartNew()
    Invoke-RestMethod -Uri "$BaseUrl/api/profile/1" -Headers $headers -ErrorAction Stop | Out-Null
    $sw2.Stop()

    Write-Host ("First request : {0} ms" -f $sw1.ElapsedMilliseconds)
    Write-Host ("Second (cache): {0} ms" -f $sw2.ElapsedMilliseconds)
} catch {
    Write-Host "Error while testing cache: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n=== STEP 4: Fallback / resilience (stop OrderService) ===" -ForegroundColor Cyan

try {
    Write-Host "Stopping orderservice via docker-compose..." -ForegroundColor Yellow
    docker-compose stop orderservice | Out-Null
    Start-Sleep -Seconds 3

    Write-Host "Requesting /api/profile/1 with OrderService stopped:" -ForegroundColor Yellow
    $profileFallback = Invoke-RestMethod -Uri "$BaseUrl/api/profile/1" -Headers $headers -ErrorAction Stop
    $profileFallback | ConvertTo-Json -Depth 10
    Write-Host "Fallback worked (response received even with stopped service)." -ForegroundColor Green
} catch {
    Write-Host "Error while testing fallback: $($_.Exception.Message)" -ForegroundColor Red
} finally {
    Write-Host "Starting orderservice again..." -ForegroundColor Yellow
    docker-compose start orderservice | Out-Null
    Start-Sleep -Seconds 3
}

Write-Host "`n=== STEP 5: Rate limiting (many requests to /api/profile/1) ===" -ForegroundColor Cyan

try {
    Write-Host "Sending a burst of requests to /api/profile/1 (expecting 429 after limit)..." -ForegroundColor Yellow
    for ($i = 1; $i -le 110; $i++) {
        try {
            Invoke-RestMethod -Uri "$BaseUrl/api/profile/1" -Headers $headers -TimeoutSec 5 -ErrorAction Stop | Out-Null
        } catch {
        }
    }

    try {
        Invoke-RestMethod -Uri "$BaseUrl/api/profile/1" -Headers $headers -TimeoutSec 5 -ErrorAction Stop | Out-Null
        Write-Host "Rate limit did NOT trigger (no 429)." -ForegroundColor Yellow
    } catch [System.Net.WebException] {
        $resp = $_.Exception.Response
        if ($resp -and $resp.StatusCode.value__ -eq 429) {
            Write-Host "Got expected 429 Too Many Requests." -ForegroundColor Green
        } else {
            Write-Host "Unexpected error while testing rate limiting: $($_.Exception.Message)" -ForegroundColor Red
        }
    }
} catch {
    Write-Host "General error while testing rate limiting: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n=== STEP 6: Prometheus metrics (/metrics on ApiGateway) ===" -ForegroundColor Cyan

try {
    $metrics = Invoke-RestMethod -Uri "$BaseUrl/metrics" -TimeoutSec 5 -ErrorAction Stop
    $metrics -split "`n" | Where-Object { $_ -match "http_requests_total" } | Select-Object -First 5
    Write-Host "ApiGateway metrics retrieved successfully." -ForegroundColor Green
} catch {
    Write-Host "Error while requesting ApiGateway metrics: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n=== STEP 7: Microservices metrics ===" -ForegroundColor Cyan

try {
    Write-Host "`nUserService /metrics:" -ForegroundColor Yellow
    Invoke-RestMethod -Uri "http://localhost:5001/metrics" -TimeoutSec 5 -ErrorAction Stop `
        | Select-String "http_requests_total" -Context 0,1 | Select-Object -First 3

    Write-Host "`nOrderService /metrics:" -ForegroundColor Yellow
    Invoke-RestMethod -Uri "http://localhost:5002/metrics" -TimeoutSec 5 -ErrorAction Stop `
        | Select-String "http_requests_total" -Context 0,1 | Select-Object -First 3

    Write-Host "`nProductService /metrics:" -ForegroundColor Yellow
    Invoke-RestMethod -Uri "http://localhost:5003/metrics" -TimeoutSec 5 -ErrorAction Stop `
        | Select-String "http_requests_total" -Context 0,1 | Select-Object -First 3

    Write-Host "Microservices metrics retrieved successfully." -ForegroundColor Green
} catch {
    Write-Host "Error while requesting microservices metrics: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n=== ALL TESTS COMPLETED ===" -ForegroundColor Cyan
