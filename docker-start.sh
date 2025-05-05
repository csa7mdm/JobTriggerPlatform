#!/bin/bash

# Docker environment setup and startup script for Deployment Portal
# This script helps with initial setup and starting the application

# Ensure we're in the project directory
cd "$(dirname "$0")"

# Check if .env file exists, if not create it
if [ ! -f .env ]; then
  echo "Creating default .env file..."
  cat > .env << EOF
# PostgreSQL settings
POSTGRES_USER=postgres
POSTGRES_PASSWORD=postgres
POSTGRES_DB=jobplatform

# Seq settings
SEQ_API_KEY=PXzKqXK6i95OeHgE7ZE0
SEQ_ADMIN_PASSWORD_HASH=

# Application settings
ASPNETCORE_ENVIRONMENT=Development
EOF
  echo ".env file created. You can edit it to change default settings."
else
  echo ".env file already exists."
fi

# Function to check if Docker is running
check_docker() {
  docker info > /dev/null 2>&1
  if [ $? -ne 0 ]; then
    echo "Error: Docker is not running or not accessible"
    echo "Please start Docker and try again"
    exit 1
  fi
}

# Check Docker is running
check_docker

# Parse command line arguments
ACTION="up"
DETACHED=false

while [[ $# -gt 0 ]]; do
  case $1 in
    --down)
      ACTION="down"
      shift
      ;;
    --rebuild)
      ACTION="rebuild"
      shift
      ;;
    --restart)
      ACTION="restart"
      shift
      ;;
    --logs)
      ACTION="logs"
      shift
      ;;
    -d|--detached)
      DETACHED=true
      shift
      ;;
    *)
      echo "Unknown option: $1"
      echo "Usage: $0 [--down|--rebuild|--restart|--logs] [-d|--detached]"
      exit 1
      ;;
  esac
done

# Execute the requested action
case $ACTION in
  "up")
    if [ "$DETACHED" = true ]; then
      echo "Starting containers in detached mode..."
      docker-compose up -d
    else
      echo "Starting containers..."
      docker-compose up
    fi
    ;;
  "down")
    echo "Stopping containers..."
    docker-compose down
    ;;
  "rebuild")
    echo "Rebuilding and starting containers..."
    if [ "$DETACHED" = true ]; then
      docker-compose up -d --build
    else
      docker-compose up --build
    fi
    ;;
  "restart")
    echo "Restarting containers..."
    docker-compose restart
    ;;
  "logs")
    echo "Showing logs..."
    docker-compose logs -f
    ;;
esac

echo "Done!"