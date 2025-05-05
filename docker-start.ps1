# Docker environment setup and startup script for Deployment Portal
# This script helps with initial setup and starting the application

# Ensure we're in the project directory
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $scriptDir

# Check if .env file exists, if not create it
if (-not (Test-Path ".env")) {
    Write-Host "Creating default .env file..."
    @"
# PostgreSQL settings
POSTGRES_USER=postgres
POSTGRES_PASSWORD=postgres
POSTGRES_DB=jobplatform

# Seq settings
SEQ_API_KEY=PXzKqXK6i95OeHgE7ZE0
SEQ_ADMIN_PASSWORD_HASH=

# Application settings
ASPNETCORE_ENVIRONMENT=Development
"@ | Out-File -FilePath ".env" -Encoding utf8
    Write-Host ".env file created. You can edit it to change default settings."
} else {
    Write-Host ".env file already exists."
}

# Function to check if Docker is running
function Check-Docker {
    try {
        docker info | Out-Null
        return $true
    } catch {
        Write-Host "Error: Docker is not running or not accessible" -ForegroundColor Red
        Write-Host "Please start Docker and try again" -ForegroundColor Red
        return $false
    }
}

# Check Docker is running
if (-not (Check-Docker)) {
    exit 1
}

# Parse command line arguments
$action = "up"
$detached = $false

foreach ($arg in $args) {
    switch ($arg) {
        "--down" { $action = "down"; break }
        "--rebuild" { $action = "rebuild"; break }
        "--restart" { $action = "restart"; break }
        "--logs" { $action = "logs"; break }
        "-d" { $detached = $true; break }
        "--detached" { $detached = $true; break }
        default {
            Write-Host "Unknown option: $arg" -ForegroundColor Red
            Write-Host "Usage: $($MyInvocation.MyCommand.Name) [--down|--rebuild|--restart|--logs] [-d|--detached]"
            exit 1
        }
    }
}

# Execute the requested action
switch ($action) {
    "up" {
        if ($detached) {
            Write-Host "Starting containers in detached mode..."
            docker-compose up -d
        } else {
            Write-Host "Starting containers..."
            docker-compose up
        }
    }
    "down" {
        Write-Host "Stopping containers..."
        docker-compose down
    }
    "rebuild" {
        Write-Host "Rebuilding and starting containers..."
        if ($detached) {
            docker-compose up -d --build
        } else {
            docker-compose up --build
        }
    }
    "restart" {
        Write-Host "Restarting containers..."
        docker-compose restart
    }
    "logs" {
        Write-Host "Showing logs..."
        docker-compose logs -f
    }
}

Write-Host "Done!" -ForegroundColor Green