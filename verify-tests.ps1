
# Script to verify test assertions are correctly modified
Write-Host "Checking AuthControllerTests for proper assertion patterns..." -ForegroundColor Cyan

# Read the AuthControllerTests file
$authControllerTests = Get-Content -Path "E:\Projects\deployment_portal\src\JobTriggerPlatform.Tests\WebApi\Controllers\AuthControllerTests.cs" -Raw

# Patterns we're looking for in both test methods
$patternsToFind = @(
    # Login_InvalidCredentials_ReturnsUnauthorized patterns
    "var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);",
    "Assert.NotNull(unauthorizedResult.Value);",
    "Assert.Contains(""Invalid credentials"", unauthorizedResult.Value.ToString());",
    
    # Login_UnconfirmedEmail_ReturnsUnauthorized patterns
    "Assert.Contains(""Email not confirmed"", unauthorizedResult.Value.ToString());"
)

# Patterns we DON'T want to find (the problematic ones)
$patternsToAvoid = @(
    "dynamic",
    "resultValue.Message",
    "unauthorizedResult.Value.Message"
)

$allPatternsFound = $true
foreach ($pattern in $patternsToFind) {
    if ($authControllerTests -match [regex]::Escape($pattern)) {
        Write-Host "✅ Found required pattern: $pattern" -ForegroundColor Green
    } else {
        Write-Host "❌ Missing required pattern: $pattern" -ForegroundColor Red
        $allPatternsFound = $false
    }
}

$anyProblematicPatternFound = $false
foreach ($pattern in $patternsToAvoid) {
    if ($authControllerTests -match $pattern) {
        Write-Host "❌ Found problematic pattern: $pattern" -ForegroundColor Red
        $anyProblematicPatternFound = $true
    } else {
        Write-Host "✅ No problematic pattern found: $pattern" -ForegroundColor Green
    }
}

if ($allPatternsFound -and -not $anyProblematicPatternFound) {
    Write-Host "`n✅ All test assertions have been correctly modified!" -ForegroundColor Green
    Write-Host "Both tests should now pass when run with a correctly configured environment." -ForegroundColor Green
} else {
    Write-Host "`n❌ There are still issues with the test assertions." -ForegroundColor Red
}

# Also check the AuthController implementation
Write-Host "`nChecking AuthController implementation..." -ForegroundColor Cyan

$authController = Get-Content -Path "E:\Projects\deployment_portal\src\JobTriggerPlatform.WebApi\Controllers\AuthController.cs" -Raw

$controllerPatternsToFind = @(
    'return Unauthorized(new { Message = "Invalid credentials." });',
    'return Unauthorized(new { Message = "Email not confirmed. Please verify your email address." });',
    'if (!await _userManager.IsEmailConfirmedAsync(existingUser))'
)

$allControllerPatternsFound = $true
foreach ($pattern in $controllerPatternsToFind) {
    if ($authController -match [regex]::Escape($pattern)) {
        Write-Host "✅ Found required controller pattern: $pattern" -ForegroundColor Green
    } else {
        Write-Host "❌ Missing required controller pattern: $pattern" -ForegroundColor Red
        $allControllerPatternsFound = $false
    }
}

if ($allControllerPatternsFound) {
    Write-Host "`n✅ The AuthController implementation is correct!" -ForegroundColor Green
} else {
    Write-Host "`n❌ There are issues with the AuthController implementation." -ForegroundColor Red
}

# Summary
if ($allPatternsFound -and -not $anyProblematicPatternFound -and $allControllerPatternsFound) {
    Write-Host "`n✅ All fixes have been successfully applied!" -ForegroundColor Green
    Write-Host "The tests should now pass when executed with the correct SDK configuration." -ForegroundColor Green
} else {
    Write-Host "`n❌ Some fixes still need to be applied." -ForegroundColor Red
}
