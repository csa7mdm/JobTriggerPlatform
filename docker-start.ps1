# PowerShell script to start the JobTriggerPlatform with Docker

# Check if Docker is installed
if (-not (Get-Command "docker" -ErrorAction SilentlyContinue)) {
    Write-Error "Docker is not installed. Please install Docker first."
    exit 1
}

# Check if Docker Compose is installed
if (-not (Get-Command "docker-compose" -ErrorAction SilentlyContinue)) {
    Write-Error "Docker Compose is not installed. Please install Docker Compose first."
    exit 1
}

# Create directories if they don't exist
$directories = @(
    ".\plugins",
    ".\docker\postgres\init",
    ".\docker\seq",
    ".\logs"
)

foreach ($dir in $directories) {
    if (-not (Test-Path $dir)) {
        Write-Host "Creating directory: $dir"
        New-Item -ItemType Directory -Path $dir -Force | Out-Null
    }
}

# Check if seq.json exists, create if not
$seqConfigPath = ".\docker\seq\seq.json"
if (-not (Test-Path $seqConfigPath)) {
    Write-Host "Creating default seq.json configuration..."
    $seqConfig = @"
{
  "Seq": {
    "Api": {
      "Keys": {
        "PXzKqXK6i95OeHgE7ZE0": {
          "Title": "JobTriggerPlatform API Key",
          "TokenType": "Ingest",
          "AssignedPermissions": [
            "ingest"
          ]
        }
      }
    }
  }
}
"@
    $seqConfig | Out-File -FilePath $seqConfigPath -Encoding utf8
}

# Build and start the containers
Write-Host "Building and starting containers with docker-compose..."
docker-compose build
docker-compose up -d

# Wait for services to start
Write-Host "Waiting for services to start..."
Start-Sleep -Seconds 10

# Show running containers
Write-Host "Running containers:"
docker-compose ps

# Print access URLs
Write-Host ""
Write-Host "JobTriggerPlatform is now running!" -ForegroundColor Green
Write-Host "Frontend: http://localhost:80"
Write-Host "Backend API: http://localhost:8080"
Write-Host "Seq Log Server: http://localhost:5341"
Write-Host ""
Write-Host "Default users:" -ForegroundColor Yellow
Write-Host "- Admin user: admin@example.com / Password123!"
Write-Host "- Operator user: operator@example.com / Password123!"
Write-Host "- Viewer user: viewer@example.com / Password123!"
