# Azure Deployment with Terraform

Deploy the Box Tracking system to Azure using **Azure Container Apps** - Azure's native serverless container platform.

## Architecture

```
Azure Container Apps Environment
├── RabbitMQ (Internal)
├── API (Public HTTPS)
├── Event Processor (Internal)
├── Dashboard (Public HTTPS)
└── Event Simulator (Public HTTPS)
```

All containers run in a shared Container Apps Environment with automatic HTTPS, scaling, and internal networking.

## Prerequisites

1. **Azure CLI** - [Install](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli)
2. **Terraform** - [Install](https://www.terraform.io/downloads)
3. **Docker** - For building images
4. **Azure Subscription** - Active Azure account

## Cost Estimate

**Development environment (~€20-40/month):**
- Container Apps Environment: Free (includes 180,000 vCPU-seconds, 360,000 GiB-seconds)
- Container Registry (Basic): ~€4.50/month
- Log Analytics: ~€2/month (30-day retention)
- Bandwidth: Minimal for testing

**Note:** First 180,000 vCPU-seconds and 360,000 GiB-seconds per month are free across all apps!

## Step 1: Login to Azure

```bash
az login
az account set --subscription "YOUR_SUBSCRIPTION_NAME_OR_ID"
```

## Step 2: Build and Push Docker Images

First, build all Docker images locally:

```bash
# Navigate to project root
cd /home/moltbot/.openclaw/workspace/box-tracking-prototype

# Build API
docker build -t boxtracking-api:latest -f src/BoxTracking.Api/Dockerfile .

# Build Event Processor
docker build -t boxtracking-processor:latest -f src/BoxTracking.EventProcessor/Dockerfile .

# Build Dashboard
docker build -t boxtracking-dashboard:latest -f src/BoxTracking.Dashboard/Dockerfile .

# Build Simulator
docker build -t boxtracking-simulator:latest -f src/BoxTracking.EventSimulator/Dockerfile .
```

**Note:** We'll push these to ACR after creating it with Terraform.

## Step 3: Configure Terraform Variables

Create a `terraform.tfvars` file (optional - defaults are provided):

```hcl
resource_group_name = "rg-boxtracking"
location            = "westeurope"  # or "eastus", "northeurope", etc.
environment         = "Development"
prefix              = "boxtrack"
acr_name            = "boxtrackingacr123"  # Must be globally unique!
```

**Important:** The `acr_name` must be globally unique across all of Azure (5-50 lowercase alphanumeric characters).

## Step 4: Initialize and Deploy

```bash
cd terraform

# Initialize Terraform
terraform init

# Preview the changes
terraform plan

# Deploy to Azure
terraform apply
```

Type `yes` when prompted.

**Deployment takes ~5-10 minutes.**

## Step 5: Push Docker Images to ACR

After Terraform creates the Container Registry:

```bash
# Get ACR credentials (Terraform outputs these)
ACR_NAME=$(terraform output -raw container_registry_login_server | cut -d'.' -f1)
ACR_LOGIN_SERVER=$(terraform output -raw container_registry_login_server)

# Login to ACR
az acr login --name $ACR_NAME

# Tag and push images
docker tag boxtracking-api:latest $ACR_LOGIN_SERVER/boxtracking-api:latest
docker push $ACR_LOGIN_SERVER/boxtracking-api:latest

docker tag boxtracking-processor:latest $ACR_LOGIN_SERVER/boxtracking-processor:latest
docker push $ACR_LOGIN_SERVER/boxtracking-processor:latest

docker tag boxtracking-dashboard:latest $ACR_LOGIN_SERVER/boxtracking-dashboard:latest
docker push $ACR_LOGIN_SERVER/boxtracking-dashboard:latest

docker tag boxtracking-simulator:latest $ACR_LOGIN_SERVER/boxtracking-simulator:latest
docker push $ACR_LOGIN_SERVER/boxtracking-simulator:latest
```

## Step 6: Update Container Apps

After pushing images, update the Container Apps to pull the new images:

```bash
# Trigger new revisions for all apps
az containerapp update --name boxtrack-api --resource-group rg-boxtracking
az containerapp update --name boxtrack-processor --resource-group rg-boxtracking
az containerapp update --name boxtrack-dashboard --resource-group rg-boxtracking
az containerapp update --name boxtrack-simulator --resource-group rg-boxtracking
```

## Step 7: Access Your Deployment

Get the URLs from Terraform outputs:

```bash
terraform output api_url
terraform output dashboard_url
terraform output simulator_url
terraform output swagger_url
```

Example output:
```
api_url = "https://boxtrack-api.greenwater-abc123.westeurope.azurecontainerapps.io"
dashboard_url = "https://boxtrack-dashboard.greenwater-abc123.westeurope.azurecontainerapps.io"
simulator_url = "https://boxtrack-simulator.greenwater-abc123.westeurope.azurecontainerapps.io"
swagger_url = "https://boxtrack-api.greenwater-abc123.westeurope.azurecontainerapps.io/swagger"
```

## Testing the Deployment

1. **Open the Simulator:**
   ```bash
   open $(terraform output -raw simulator_url)
   ```

2. **Send test events** using the UI

3. **View metrics in Dashboard:**
   ```bash
   open $(terraform output -raw dashboard_url)
   ```

4. **Check API health:**
   ```bash
   curl $(terraform output -raw api_url)/health
   ```

## Monitoring and Logs

### View logs in Azure Portal

1. Go to [Azure Portal](https://portal.azure.com)
2. Navigate to Resource Groups → `rg-boxtracking`
3. Select any Container App
4. Go to "Log stream" or "Logs" for detailed diagnostics

### View logs with Azure CLI

```bash
# API logs
az containerapp logs show --name boxtrack-api --resource-group rg-boxtracking --follow

# Event Processor logs
az containerapp logs show --name boxtrack-processor --resource-group rg-boxtracking --follow

# Dashboard logs
az containerapp logs show --name boxtrack-dashboard --resource-group rg-boxtracking --follow
```

## Scaling

Container Apps automatically scale based on HTTP requests (for API, Dashboard, Simulator) and can be configured with custom scaling rules.

**Manual scaling:**

```bash
az containerapp update \
  --name boxtrack-api \
  --resource-group rg-boxtracking \
  --min-replicas 2 \
  --max-replicas 5
```

## Update Deployment

After code changes:

```bash
# 1. Rebuild images
docker build -t boxtracking-api:latest -f src/BoxTracking.Api/Dockerfile .

# 2. Push to ACR
docker tag boxtracking-api:latest $ACR_LOGIN_SERVER/boxtracking-api:latest
docker push $ACR_LOGIN_SERVER/boxtracking-api:latest

# 3. Trigger new revision
az containerapp update --name boxtrack-api --resource-group rg-boxtracking
```

## Cleanup

To destroy all resources and stop charges:

```bash
terraform destroy
```

Type `yes` when prompted.

**This deletes:**
- All Container Apps
- Container Registry (and all images)
- Log Analytics Workspace
- Resource Group

## Troubleshooting

### Container App not starting

```bash
# Check revision status
az containerapp revision list --name boxtrack-api --resource-group rg-boxtracking --output table

# View detailed logs
az containerapp logs show --name boxtrack-api --resource-group rg-boxtracking --tail 100
```

### Image pull errors

```bash
# Verify ACR credentials
az acr credential show --name boxtrackingacr123

# Test ACR login manually
az acr login --name boxtrackingacr123
```

### RabbitMQ connection issues

- Ensure `RabbitMQ__Host` environment variable uses the internal container app name
- Check if RabbitMQ container is running:
  ```bash
  az containerapp show --name boxtrack-rabbitmq --resource-group rg-boxtracking
  ```

## Alternative: One-Command Deployment Script

Create `deploy.sh`:

```bash
#!/bin/bash
set -e

echo "🚀 Deploying Box Tracking to Azure..."

# Build images
echo "📦 Building Docker images..."
docker build -t boxtracking-api:latest -f src/BoxTracking.Api/Dockerfile .
docker build -t boxtracking-processor:latest -f src/BoxTracking.EventProcessor/Dockerfile .
docker build -t boxtracking-dashboard:latest -f src/BoxTracking.Dashboard/Dockerfile .
docker build -t boxtracking-simulator:latest -f src/BoxTracking.EventSimulator/Dockerfile .

# Deploy infrastructure
echo "☁️  Creating Azure resources..."
cd terraform
terraform init
terraform apply -auto-approve

# Get ACR details
ACR_LOGIN_SERVER=$(terraform output -raw container_registry_login_server)
ACR_NAME=$(echo $ACR_LOGIN_SERVER | cut -d'.' -f1)

# Login to ACR
echo "🔐 Logging into ACR..."
az acr login --name $ACR_NAME

# Push images
echo "⬆️  Pushing images to ACR..."
docker tag boxtracking-api:latest $ACR_LOGIN_SERVER/boxtracking-api:latest
docker push $ACR_LOGIN_SERVER/boxtracking-api:latest

docker tag boxtracking-processor:latest $ACR_LOGIN_SERVER/boxtracking-processor:latest
docker push $ACR_LOGIN_SERVER/boxtracking-processor:latest

docker tag boxtracking-dashboard:latest $ACR_LOGIN_SERVER/boxtracking-dashboard:latest
docker push $ACR_LOGIN_SERVER/boxtracking-dashboard:latest

docker tag boxtracking-simulator:latest $ACR_LOGIN_SERVER/boxtracking-simulator:latest
docker push $ACR_LOGIN_SERVER/boxtracking-simulator:latest

# Update container apps
echo "🔄 Updating Container Apps..."
az containerapp update --name boxtrack-api --resource-group rg-boxtracking
az containerapp update --name boxtrack-processor --resource-group rg-boxtracking
az containerapp update --name boxtrack-dashboard --resource-group rg-boxtracking
az containerapp update --name boxtrack-simulator --resource-group rg-boxtracking

# Show URLs
echo ""
echo "✅ Deployment complete!"
echo ""
echo "📊 Dashboard: $(terraform output -raw dashboard_url)"
echo "🎯 Simulator: $(terraform output -raw simulator_url)"
echo "🔌 API: $(terraform output -raw api_url)"
echo "📚 Swagger: $(terraform output -raw swagger_url)"
```

Make it executable and run:

```bash
chmod +x deploy.sh
./deploy.sh
```

## Resources Created

| Resource | Purpose | Public Access |
|----------|---------|---------------|
| Resource Group | Container for all resources | - |
| Container Registry | Stores Docker images | No |
| Log Analytics | Centralized logging | No |
| Container Apps Environment | Shared networking & config | No |
| RabbitMQ Container App | Message queue | No (internal only) |
| API Container App | REST API | Yes (HTTPS) |
| Event Processor Container App | Background worker | No |
| Dashboard Container App | Metrics UI | Yes (HTTPS) |
| Simulator Container App | Test event generator | Yes (HTTPS) |

## Security Considerations

**Current setup (Development):**
- ✅ HTTPS enabled automatically
- ✅ ACR credentials stored as secrets
- ⚠️ No authentication on API endpoints
- ⚠️ RabbitMQ uses default credentials

**Production improvements needed:**
- Add Azure AD authentication
- Use Azure Key Vault for secrets
- Enable CORS restrictions
- Use Azure Service Bus instead of RabbitMQ
- Add Application Gateway / WAF
- Enable Container Apps authentication

## Next Steps

- Add Azure Key Vault integration
- Implement API authentication (Azure AD)
- Add Application Insights for monitoring
- Configure custom domains
- Set up CI/CD with GitHub Actions
- Add database (Azure SQL or Cosmos DB)

---

**Built with:** Terraform, Azure Container Apps, Azure Container Registry
