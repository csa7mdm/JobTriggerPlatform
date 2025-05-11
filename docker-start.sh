#!/bin/bash

# Exit on error
set -e

# Print commands
set -x

# Check if docker is installed
if ! command -v docker &> /dev/null; then
    echo "Docker is not installed. Please install Docker first."
    exit 1
fi

# Check if docker-compose is installed
if ! command -v docker-compose &> /dev/null; then
    echo "Docker Compose is not installed. Please install Docker Compose first."
    exit 1
fi

# Create directories if they don't exist
mkdir -p ./plugins
mkdir -p ./docker/postgres/init
mkdir -p ./docker/seq
mkdir -p ./logs

# Check if seq.json exists, create if not
if [ ! -f ./docker/seq/seq.json ]; then
    echo "Creating default seq.json configuration..."
    cat > ./docker/seq/seq.json << 'EOL'
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
EOL
fi

# Build and start the containers
echo "Building and starting containers with docker-compose..."
docker-compose build
docker-compose up -d

# Wait for services to start
echo "Waiting for services to start..."
sleep 10

# Show running containers
echo "Running containers:"
docker-compose ps

# Print access URLs
echo ""
echo "JobTriggerPlatform is now running!"
echo "Frontend: http://localhost:80"
echo "Backend API: http://localhost:8080"
echo "Seq Log Server: http://localhost:5341"
echo ""
echo "Default users:"
echo "- Admin user: admin@example.com / Password123!"
echo "- Operator user: operator@example.com / Password123!"
echo "- Viewer user: viewer@example.com / Password123!"
