#!/bin/bash
set -e

echo "🧪 Running Box Tracking Integration Tests..."
echo ""

# Build test image
echo "📦 Building test image..."
docker build -f tests/BoxTracking.IntegrationTests/Dockerfile.test -t box-tracking-tests .

# Run tests with Docker socket mounted (for Testcontainers)
echo ""
echo "🚀 Running tests..."
docker run --rm \
  -v /var/run/docker.sock:/var/run/docker.sock \
  box-tracking-tests

echo ""
echo "✅ Tests completed!"
