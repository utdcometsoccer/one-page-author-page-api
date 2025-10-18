# PowerShell script to run different testing scenarios
# Usage: .\RunTests.ps1 -Scenario 1 -DomainName "test.example.com"

param(
    [Parameter(Mandatory=$true)]
    [ValidateSet(1, 2, 3)]
    [int]$Scenario,
    
    [Parameter(Mandatory=$true)]
    [string]$DomainName,
    
    [Parameter(Mandatory=$false)]
    [string]$UserEmail = "test@example.com",
    
    [Parameter(Mandatory=$false)]
    [string]$FunctionAppUrl = "http://localhost:7072",
    
    [Parameter(Mandatory=$false)]
    [string]$FunctionKey = "",
    
    [Parameter(Mandatory=$false)]
    [bool]$ConfirmRealMoney = $false
)

# Function to make HTTP requests to the Function App
function Invoke-FunctionRequest {
    param(
        [string]$Endpoint,
        [object]$Body,
        [string]$Method = "POST"
    )
    
    $headers = @{
        "Content-Type" = "application/json"
    }
    
    if ($FunctionKey) {
        $headers["x-functions-key"] = $FunctionKey
    }
    
    $url = "$FunctionAppUrl$Endpoint"
    if ($FunctionKey -and -not $headers.ContainsKey("x-functions-key")) {
        $url += "?code=$FunctionKey"
    }
    
    try {
        $response = Invoke-RestMethod -Uri $url -Method $Method -Body ($Body | ConvertTo-Json) -Headers $headers -ErrorAction Stop
        return $response
    }
    catch {
        Write-Error "Request failed: $($_.Exception.Message)"
        if ($_.Exception.Response) {
            $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
            $responseBody = $reader.ReadToEnd()
            Write-Error "Response: $responseBody"
        }
        return $null
    }
}

Write-Host "🧪 Starting Testing Scenario $Scenario for domain: $DomainName" -ForegroundColor Cyan
Write-Host "📧 User Email: $UserEmail" -ForegroundColor Gray
Write-Host "🌐 Function App URL: $FunctionAppUrl" -ForegroundColor Gray
Write-Host ""

switch ($Scenario) {
    1 {
        Write-Host "🎯 Scenario 1: Frontend UI Testing (Safe - No Infrastructure Changes)" -ForegroundColor Green
        Write-Host "   This scenario mocks all backend operations for safe UI testing" -ForegroundColor Gray
        Write-Host ""
        
        # Test the end-to-end scenario 1 endpoint
        $body = @{
            domainName = $DomainName
            userEmail = $UserEmail
        }
        
        Write-Host "🚀 Running end-to-end frontend test..." -ForegroundColor Yellow
        $result = Invoke-FunctionRequest -Endpoint "/api/test/scenario1" -Body $body
        
        if ($result) {
            Write-Host "✅ Test completed successfully!" -ForegroundColor Green
            Write-Host "📊 Results:" -ForegroundColor White
            Write-Host "   - Success: $($result.success)" -ForegroundColor $(if($result.success) { "Green" } else { "Red" })
            Write-Host "   - Duration: $($result.duration)" -ForegroundColor White
            Write-Host "   - Total Cost: `$0.00 (mocked)" -ForegroundColor Green
            Write-Host "   - Steps Completed: $($result.steps.Count)" -ForegroundColor White
            
            foreach ($step in $result.steps) {
                $status = if ($step.success) { "✅" } else { "❌" }
                Write-Host "     $status $($step.name): $($step.message)" -ForegroundColor $(if($step.success) { "Green" } else { "Red" })
            }
        }
        else {
            Write-Host "❌ Test failed" -ForegroundColor Red
        }
    }
    
    2 {
        Write-Host "🎯 Scenario 2: Individual Function Testing (Minimal Cost)" -ForegroundColor Yellow
        Write-Host "   This scenario tests each function individually with real Azure APIs" -ForegroundColor Gray
        Write-Host ""
        
        # Test Front Door operations
        Write-Host "🌐 Testing Front Door operations..." -ForegroundColor Yellow
        $tests = @(
            @{ operation = "exists"; description = "Check if domain exists in Front Door" },
            @{ operation = "add"; description = "Add domain to Front Door" },
            @{ operation = "validate"; description = "Validate domain in Front Door" }
        )
        
        foreach ($test in $tests) {
            $body = @{
                domainName = $DomainName
                operation = $test.operation
            }
            
            Write-Host "  📋 $($test.description)..." -ForegroundColor Gray
            $result = Invoke-FunctionRequest -Endpoint "/api/test/frontdoor" -Body $body
            
            if ($result) {
                $status = if ($result.success) { "✅" } else { "❌" }
                $cost = if ($result.estimatedCost -gt 0) { " (`$$($result.estimatedCost))" } else { "" }
                Write-Host "     $status $($result.message)$cost" -ForegroundColor $(if($result.success) { "Green" } else { "Red" })
            }
        }
        
        # Test DNS Zone operations
        Write-Host "🌍 Testing DNS Zone operations..." -ForegroundColor Yellow
        $dnsTests = @(
            @{ operation = "exists"; description = "Check if DNS zone exists" },
            @{ operation = "create"; description = "Create DNS zone" },
            @{ operation = "nameservers"; description = "Get name servers" }
        )
        
        foreach ($test in $dnsTests) {
            $body = @{
                domainName = $DomainName
                operation = $test.operation
            }
            
            Write-Host "  📋 $($test.description)..." -ForegroundColor Gray
            $result = Invoke-FunctionRequest -Endpoint "/api/test/dns" -Body $body
            
            if ($result) {
                $status = if ($result.success) { "✅" } else { "❌" }
                $cost = if ($result.estimatedCost -gt 0) { " (`$$($result.estimatedCost))" } else { "" }
                Write-Host "     $status $($result.message)$cost" -ForegroundColor $(if($result.success) { "Green" } else { "Red" })
                
                if ($test.operation -eq "nameservers" -and $result.data) {
                    Write-Host "     📝 Name servers: $($result.data -join ', ')" -ForegroundColor Gray
                }
            }
        }
        
        # Test Google Domains operations (mocked in scenario 2)
        Write-Host "🔍 Testing Google Domains operations..." -ForegroundColor Yellow
        $googleTests = @(
            @{ operation = "available"; description = "Check domain availability" }
        )
        
        foreach ($test in $googleTests) {
            $body = @{
                domainName = $DomainName
                operation = $test.operation
            }
            
            Write-Host "  📋 $($test.description)..." -ForegroundColor Gray
            $result = Invoke-FunctionRequest -Endpoint "/api/test/googledomains" -Body $body
            
            if ($result) {
                $status = if ($result.success) { "✅" } else { "❌" }
                Write-Host "     $status $($result.message) (mocked)" -ForegroundColor $(if($result.success) { "Green" } else { "Red" })
            }
        }
    }
    
    3 {
        Write-Host "🎯 Scenario 3: Full End-to-End with Real Money (PRODUCTION TEST)" -ForegroundColor Red
        Write-Host "   ⚠️  WARNING: This scenario involves real money and creates actual resources!" -ForegroundColor Red
        Write-Host ""
        
        if (-not $ConfirmRealMoney) {
            Write-Host "❌ Real money test requires explicit confirmation." -ForegroundColor Red
            Write-Host "   Add -ConfirmRealMoney `$true to proceed with production testing." -ForegroundColor Yellow
            return
        }
        
        Write-Host "💰 Proceeding with real money test..." -ForegroundColor Red
        Write-Host "   Domain: $DomainName" -ForegroundColor White
        Write-Host "   Estimated cost: `$12-50 depending on domain TLD" -ForegroundColor Yellow
        Write-Host ""
        
        # Run the full end-to-end test with real money
        $body = @{
            domainName = $DomainName
            userEmail = $UserEmail
            confirmRealMoney = $true
        }
        
        Write-Host "🚀 Running full production test..." -ForegroundColor Red
        $result = Invoke-FunctionRequest -Endpoint "/api/test/scenario3" -Body $body
        
        if ($result) {
            Write-Host "📊 Production Test Results:" -ForegroundColor White
            Write-Host "   - Success: $($result.success)" -ForegroundColor $(if($result.success) { "Green" } else { "Red" })
            Write-Host "   - Duration: $($result.duration)" -ForegroundColor White
            Write-Host "   - Estimated Cost: `$$($result.estimatedCost)" -ForegroundColor Yellow
            Write-Host "   - Actual Cost: `$$($result.totalCost)" -ForegroundColor $(if($result.totalCost -gt 0) { "Red" } else { "Green" })
            Write-Host "   - Steps Completed: $($result.steps.Count)" -ForegroundColor White
            Write-Host ""
            
            foreach ($step in $result.steps) {
                $status = if ($step.success) { "✅" } else { "❌" }
                $cost = if ($step.cost -gt 0) { " (`$$($step.cost))" } else { "" }
                Write-Host "     $status $($step.name): $($step.message)$cost" -ForegroundColor $(if($step.success) { "Green" } else { "Red" })
            }
            
            if ($result.totalCost -gt 0) {
                Write-Host ""
                Write-Host "💳 CHARGES INCURRED: `$$($result.totalCost)" -ForegroundColor Red
                Write-Host "   Check your Azure and Google Cloud billing!" -ForegroundColor Yellow
            }
        }
        else {
            Write-Host "❌ Production test failed" -ForegroundColor Red
        }
    }
}

Write-Host ""
Write-Host "🏁 Testing completed!" -ForegroundColor Cyan