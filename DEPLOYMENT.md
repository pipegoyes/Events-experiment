# Box Tracking - Deployment Guide

## ✅ Current Status

Terraform is now the **single source of truth** for infrastructure.

All Azure resources are fully managed by Terraform with **zero configuration drift**.

## 📦 Resources Managed

- Resource Group: `rg-boxtracking`
- Container Apps Environment: `boxtrack-env`
- Container Apps (5):
  - `boxtrack-api` (External HTTPS)
  - `boxtrack-dashboard` (External HTTPS)
  - `boxtrack-simulator` (External HTTPS)
  - `boxtrack-processor` (Internal, no ingress)
  - `boxtrack-rabbitmq` (Internal ingress)
- Docker Images in ACR: `flighttrackeracr6ebcf3aa`
  - boxtracking-api:latest
  - boxtracking-dashboard:latest
  - boxtracking-processor:latest
  - boxtracking-simulator:latest

## 🚀 Full Deployment (Fresh Start)

### Prerequisites

1. **Azure CLI** - Logged in: `az login`
2. **Docker** - Running and logged into ACR
3. **Terraform** - Installed

### Step 1: Build and Push Docker Images

```bash
# Navigate to project root
cd /home/moltbot/.openclaw/workspace/box-tracking-prototype

# Build all images
docker build -t boxtracking-api:latest -f src/BoxTracking.Api/Dockerfile .
docker build -t boxtracking-processor:latest -f src/BoxTracking.EventProcessor/Dockerfile .
docker build -t boxtracking-dashboard:latest -f src/BoxTracking.Dashboard/Dockerfile .
docker build -t boxtracking-simulator:latest -f src/BoxTracking.EventSimulator/Dockerfile .

# Login to ACR
az acr login --name flighttrackeracr6ebcf3aa

# Tag and push
ACR="flighttrackeracr6ebcf3aa.azurecr.io"
docker tag boxtracking-api:latest $ACR/boxtracking-api:latest
docker push $ACR/boxtracking-api:latest

docker tag boxtracking-processor:latest $ACR/boxtracking-processor:latest
docker push $ACR/boxtracking-processor:latest

docker tag boxtracking-dashboard:latest $ACR/boxtracking-dashboard:latest
docker push $ACR/boxtracking-dashboard:latest

docker tag boxtracking-simulator:latest $ACR/boxtracking-simulator:latest
docker push $ACR/boxtracking-simulator:latest
```

### Step 2: Deploy Infrastructure with Terraform

```bash
cd terraform

# Initialize Terraform
terraform init

# Review deployment plan
terraform plan

# Deploy (creates ALL resources)
terraform apply -auto-approve
```

**Deployment time:** ~5-7 minutes

### Step 3: Verify Deployment

```bash
# Check all apps are running
az containerapp list --resource-group rg-boxtracking --query "[].{Name:name, State:properties.runningStatus}" --output table

# Get URLs
terraform output
```

**Expected URLs:**
- API: https://boxtrack-api.calmsky-0cfe143e.westeurope.azurecontainerapps.io
- Dashboard: https://boxtrack-dashboard.calmsky-0cfe143e.westeurope.azurecontainerapps.io
- Simulator: https://boxtrack-simulator.calmsky-0cfe143e.westeurope.azurecontainerapps.io
- Swagger: https://boxtrack-api.calmsky-0cfe143e.westeurope.azurecontainerapps.io/swagger

## 🗑️ Full Destroy (Cleanup)

### Option 1: Terraform Destroy (Recommended)

```bash
cd terraform
terraform destroy -auto-approve
```

**Destroys:**
- All 5 Container Apps
- Container Apps Environment
- Resource Group

**Keeps:**
- Docker images in ACR (shared with flight-tracker)

### Option 2: Delete Resource Group

```bash
az group delete --name rg-boxtracking --yes --no-wait
```

**Cleanup time:** ~2-3 minutes

## 🔄 Redeploy After Destroy

1. **Images still in ACR** → Skip Step 1, go directly to Step 2 (Terraform)
2. **Fresh rebuild needed** → Follow full deployment (Steps 1-3)

## 📊 Cost Optimization

**Daily Cost:** ~€3.36/day (~€101/month)

To reduce costs:

1. **Stop all apps (scale to 0):**
   ```bash
   az containerapp update --name boxtrack-api --resource-group rg-boxtracking --min-replicas 0
   az containerapp update --name boxtrack-dashboard --resource-group rg-boxtracking --min-replicas 0
   az containerapp update --name boxtrack-processor --resource-group rg-boxtracking --min-replicas 0
   az containerapp update --name boxtrack-rabbitmq --resource-group rg-boxtracking --min-replicas 0
   az containerapp update --name boxtrack-simulator --resource-group rg-boxtracking --min-replicas 0
   ```

2. **Restart when needed (scale back to 1):**
   ```bash
   az containerapp update --name boxtrack-api --resource-group rg-boxtracking --min-replicas 1
   # ... repeat for each app
   ```

3. **Full destroy when not in use** (recommended)

## 🐛 Troubleshooting

### Terraform State Issues

If you get "resource already exists" errors:

```bash
# Import existing resource
terraform import azurerm_container_app.api /subscriptions/SUBSCRIPTION_ID/resourceGroups/rg-boxtracking/providers/Microsoft.App/containerApps/boxtrack-api

# Verify state
terraform plan
```

### Container App Not Starting

Check logs:
```bash
az containerapp logs show --name boxtrack-api --resource-group rg-boxtracking --tail 50 --follow
```

### Image Not Found

Verify images are in ACR:
```bash
az acr repository list --name flighttrackeracr6ebcf3aa --output table
```

## 📝 Notes

1. **Terraform State:** Stored locally in `terraform/terraform.tfstate`
   - **DO NOT** delete this file
   - Commit to Git is optional (contains sensitive data)
   
2. **Sentry Integration:** All apps send errors to Sentry (no Azure Log Analytics)

3. **RabbitMQ:** Internal communication only
   - Processor connects via: `boxtrack-rabbitmq.internal.calmsky-0cfe143e.westeurope.azurecontainerapps.io:5672`

4. **Simulator Port:** Uses port 8080 (not 5001 as originally designed)

## 🎯 Quick Commands

**Status check:**
```bash
az containerapp list --resource-group rg-boxtracking --output table
```

**Get URLs:**
```bash
cd terraform && terraform output
```

**Full rebuild & redeploy:**
```bash
cd /home/moltbot/.openclaw/workspace/box-tracking-prototype
./terraform/deploy.sh  # (if working)
# OR follow Steps 1-3 above
```

---

**Last updated:** 2026-03-14  
**Terraform version:** ~> 3.0  
**Azure Region:** West Europe
