#!/bin/bash
set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo -e "${GREEN}🚀 Box Tracking - Azure Deployment${NC}"
echo ""

# Check prerequisites
echo "🔍 Checking prerequisites..."

if ! command -v az &> /dev/null; then
    echo -e "${RED}❌ Azure CLI not found. Please install: https://docs.microsoft.com/en-us/cli/azure/install-azure-cli${NC}"
    exit 1
fi

if ! command -v terraform &> /dev/null; then
    echo -e "${RED}❌ Terraform not found. Please install: https://www.terraform.io/downloads${NC}"
    exit 1
fi

if ! command -v docker &> /dev/null; then
    echo -e "${RED}❌ Docker not found. Please install: https://docs.docker.com/get-docker/${NC}"
    exit 1
fi

echo -e "${GREEN}✅ All prerequisites installed${NC}"
echo ""

# Check Azure login
echo "🔐 Checking Azure login..."
if ! az account show &> /dev/null; then
    echo -e "${YELLOW}⚠️  Not logged in to Azure. Running 'az login'...${NC}"
    az login
fi

SUBSCRIPTION=$(az account show --query name -o tsv)
echo -e "${GREEN}✅ Logged in to Azure subscription: $SUBSCRIPTION${NC}"
echo ""

# Navigate to project root
cd "$(dirname "$0")/.."
PROJECT_ROOT=$(pwd)

# Build Docker images
echo "📦 Building Docker images..."
echo ""

echo "  Building API..."
docker build -q -t boxtracking-api:latest -f src/BoxTracking.Api/Dockerfile . || {
    echo -e "${RED}❌ Failed to build API image${NC}"
    exit 1
}

echo "  Building Event Processor..."
docker build -q -t boxtracking-processor:latest -f src/BoxTracking.EventProcessor/Dockerfile . || {
    echo -e "${RED}❌ Failed to build Event Processor image${NC}"
    exit 1
}

echo "  Building Dashboard..."
docker build -q -t boxtracking-dashboard:latest -f src/BoxTracking.Dashboard/Dockerfile . || {
    echo -e "${RED}❌ Failed to build Dashboard image${NC}"
    exit 1
}

echo "  Building Event Simulator..."
docker build -q -t boxtracking-simulator:latest -f src/BoxTracking.EventSimulator/Dockerfile . || {
    echo -e "${RED}❌ Failed to build Event Simulator image${NC}"
    exit 1
}

echo -e "${GREEN}✅ All images built successfully${NC}"
echo ""

# Deploy infrastructure with Terraform
echo "☁️  Deploying infrastructure to Azure..."
cd terraform

if [ ! -f "terraform.tfvars" ]; then
    echo -e "${YELLOW}⚠️  terraform.tfvars not found. Copy terraform.tfvars.example and customize it.${NC}"
    echo ""
    echo "Run: cp terraform.tfvars.example terraform.tfvars"
    echo "Then edit terraform.tfvars and change the acr_name to something unique."
    exit 1
fi

echo "  Initializing Terraform..."
terraform init -upgrade > /dev/null

echo "  Applying Terraform configuration..."
terraform apply -auto-approve || {
    echo -e "${RED}❌ Terraform deployment failed${NC}"
    exit 1
}

echo -e "${GREEN}✅ Infrastructure deployed${NC}"
echo ""

# Get ACR details
echo "🔍 Getting Azure Container Registry details..."
ACR_LOGIN_SERVER=$(terraform output -raw container_registry_login_server)
ACR_NAME=$(echo $ACR_LOGIN_SERVER | cut -d'.' -f1)

echo -e "${GREEN}  ACR: $ACR_LOGIN_SERVER${NC}"
echo ""

# Login to ACR
echo "🔐 Logging into Azure Container Registry..."
az acr login --name $ACR_NAME > /dev/null || {
    echo -e "${RED}❌ Failed to login to ACR${NC}"
    exit 1
}

echo -e "${GREEN}✅ Logged in to ACR${NC}"
echo ""

# Push images to ACR
echo "⬆️  Pushing images to Azure Container Registry..."
echo ""

echo "  Pushing API..."
docker tag boxtracking-api:latest $ACR_LOGIN_SERVER/boxtracking-api:latest
docker push $ACR_LOGIN_SERVER/boxtracking-api:latest > /dev/null

echo "  Pushing Event Processor..."
docker tag boxtracking-processor:latest $ACR_LOGIN_SERVER/boxtracking-processor:latest
docker push $ACR_LOGIN_SERVER/boxtracking-processor:latest > /dev/null

echo "  Pushing Dashboard..."
docker tag boxtracking-dashboard:latest $ACR_LOGIN_SERVER/boxtracking-dashboard:latest
docker push $ACR_LOGIN_SERVER/boxtracking-dashboard:latest > /dev/null

echo "  Pushing Event Simulator..."
docker tag boxtracking-simulator:latest $ACR_LOGIN_SERVER/boxtracking-simulator:latest
docker push $ACR_LOGIN_SERVER/boxtracking-simulator:latest > /dev/null

echo -e "${GREEN}✅ All images pushed to ACR${NC}"
echo ""

# Get resource group name
RESOURCE_GROUP=$(terraform output -raw resource_group_name)

# Update Container Apps to pull new images
echo "🔄 Updating Container Apps with new images..."
echo ""

echo "  Updating API..."
az containerapp update --name boxtrack-api --resource-group $RESOURCE_GROUP --output none

echo "  Updating Event Processor..."
az containerapp update --name boxtrack-processor --resource-group $RESOURCE_GROUP --output none

echo "  Updating Dashboard..."
az containerapp update --name boxtrack-dashboard --resource-group $RESOURCE_GROUP --output none

echo "  Updating Event Simulator..."
az containerapp update --name boxtrack-simulator --resource-group $RESOURCE_GROUP --output none

echo -e "${GREEN}✅ All Container Apps updated${NC}"
echo ""

# Get URLs
API_URL=$(terraform output -raw api_url)
DASHBOARD_URL=$(terraform output -raw dashboard_url)
SIMULATOR_URL=$(terraform output -raw simulator_url)
SWAGGER_URL=$(terraform output -raw swagger_url)

# Wait a moment for apps to start
echo "⏳ Waiting 30 seconds for apps to start..."
sleep 30

# Display results
echo ""
echo -e "${GREEN}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo -e "${GREEN}✅ Deployment Complete!${NC}"
echo -e "${GREEN}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo ""
echo -e "${YELLOW}📱 Access Your Applications:${NC}"
echo ""
echo -e "  🎯 Event Simulator: ${GREEN}$SIMULATOR_URL${NC}"
echo -e "  📊 Dashboard:       ${GREEN}$DASHBOARD_URL${NC}"
echo -e "  🔌 API:             ${GREEN}$API_URL${NC}"
echo -e "  📚 Swagger Docs:    ${GREEN}$SWAGGER_URL${NC}"
echo ""
echo -e "${YELLOW}📋 Quick Test:${NC}"
echo ""
echo "  1. Open the Simulator: $SIMULATOR_URL"
echo "  2. Click 'Simulate Lifecycle' to send test events"
echo "  3. Open the Dashboard: $DASHBOARD_URL"
echo "  4. Watch metrics update in real-time"
echo ""
echo -e "${YELLOW}🔍 Monitor Logs:${NC}"
echo ""
echo "  az containerapp logs show --name boxtrack-api --resource-group $RESOURCE_GROUP --follow"
echo ""
echo -e "${YELLOW}🗑️  Cleanup (when done):${NC}"
echo ""
echo "  cd terraform && terraform destroy"
echo ""
echo -e "${GREEN}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
