#!/bin/bash
set -e

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

echo -e "${YELLOW}🗑️  Box Tracking - Cleanup${NC}"
echo ""

# Navigate to terraform directory
cd "$(dirname "$0")"

# Check if Terraform is initialized
if [ ! -d ".terraform" ]; then
    echo -e "${RED}❌ Terraform not initialized. Nothing to destroy.${NC}"
    exit 1
fi

# Get resource info before destroying
RESOURCE_GROUP=$(terraform output -raw resource_group_name 2>/dev/null || echo "unknown")

echo -e "${YELLOW}⚠️  WARNING: This will destroy all Azure resources!${NC}"
echo ""
echo "  Resource Group: $RESOURCE_GROUP"
echo ""
echo "  This includes:"
echo "    - All Container Apps"
echo "    - Container Registry (and all images)"
echo "    - Log Analytics Workspace"
echo "    - All logs and monitoring data"
echo ""

read -p "Are you sure you want to proceed? (yes/no): " -r
echo ""

if [[ ! $REPLY =~ ^[Yy][Ee][Ss]$ ]]; then
    echo -e "${GREEN}✅ Aborted. No resources were deleted.${NC}"
    exit 0
fi

echo "🗑️  Destroying Azure resources..."
terraform destroy -auto-approve

echo ""
echo -e "${GREEN}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo -e "${GREEN}✅ Cleanup Complete!${NC}"
echo -e "${GREEN}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo ""
echo "All Azure resources have been deleted."
echo "You will no longer be charged for these resources."
echo ""
