#!/bin/bash

# LeaseLogic Complete Deployment Script
# This script deploys the entire LeaseLogic system from infrastructure to Function App

set -e

# Color codes for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

# Default values
RESOURCE_GROUP_NAME=""
LOCATION="japaneast"
ENVIRONMENT="dev"
SKIP_INFRASTRUCTURE=false
SKIP_FUNCTION_APP=false
CLEANUP_ON_ERROR=false

# Function to print colored output
print_status() {
    echo -e "${CYAN}[INFO]${NC} $1"
}

print_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

print_header() {
    echo ""
    echo -e "${BLUE}================================================${NC}"
    echo -e "${BLUE} $1${NC}"
    echo -e "${BLUE}================================================${NC}"
    echo ""
}

# Function to show usage
show_usage() {
    cat << EOF
Usage: $0 -g <resource-group-name> [OPTIONS]

Required:
  -g, --resource-group    Resource group name

Options:
  -l, --location         Azure region (default: japaneast)
  -e, --environment      Environment name (default: dev)
  --skip-infrastructure  Skip infrastructure deployment
  --skip-function-app    Skip Function App deployment
  --cleanup-on-error     Delete resource group if deployment fails
  -h, --help            Show this help message

Examples:
  $0 -g "leaselogic-dev-rg"
  $0 -g "leaselogic-prod-rg" -e "prod" -l "eastus"
  $0 -g "leaselogic-test-rg" --skip-infrastructure
EOF
}

# Parse command line arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        -g|--resource-group)
            RESOURCE_GROUP_NAME="$2"
            shift 2
            ;;
        -l|--location)
            LOCATION="$2"
            shift 2
            ;;
        -e|--environment)
            ENVIRONMENT="$2"
            shift 2
            ;;
        --skip-infrastructure)
            SKIP_INFRASTRUCTURE=true
            shift
            ;;
        --skip-function-app)
            SKIP_FUNCTION_APP=true
            shift
            ;;
        --cleanup-on-error)
            CLEANUP_ON_ERROR=true
            shift
            ;;
        -h|--help)
            show_usage
            exit 0
            ;;
        *)
            print_error "Unknown option $1"
            show_usage
            exit 1
            ;;
    esac
done

# Validate required parameters
if [ -z "$RESOURCE_GROUP_NAME" ]; then
    print_error "Resource group name is required. Use -g option."
    show_usage
    exit 1
fi

# Cleanup function for error handling
cleanup_on_error() {
    if [ "$CLEANUP_ON_ERROR" = true ]; then
        print_warning "Cleaning up resources due to deployment failure..."
        az group delete --name "$RESOURCE_GROUP_NAME" --yes --no-wait
        print_status "Resource group deletion initiated"
    fi
}

# Set error trap
trap 'cleanup_on_error' ERR

print_header "LeaseLogic Complete Deployment Starting"

print_status "Configuration:"
print_status "  Resource Group: $RESOURCE_GROUP_NAME"
print_status "  Location: $LOCATION"
print_status "  Environment: $ENVIRONMENT"
print_status "  Skip Infrastructure: $SKIP_INFRASTRUCTURE"
print_status "  Skip Function App: $SKIP_FUNCTION_APP"

# Step 1: Prerequisites check
print_header "Step 1: Prerequisites Check"

print_status "Checking Azure CLI installation..."
if ! command -v az &> /dev/null; then
    print_error "Azure CLI is not installed. Please install it first."
    exit 1
fi
print_success "Azure CLI is installed"

print_status "Checking Azure CLI authentication..."
if ! az account show > /dev/null 2>&1; then
    print_error "Not logged in to Azure. Please run 'az login' first."
    exit 1
fi

ACCOUNT_INFO=$(az account show --query "{subscriptionId:id, tenantId:tenantId, user:user.name}" -o json)
SUBSCRIPTION_ID=$(echo $ACCOUNT_INFO | jq -r '.subscriptionId')
USER_NAME=$(echo $ACCOUNT_INFO | jq -r '.user')
print_success "Logged in as: $USER_NAME"
print_status "Subscription: $SUBSCRIPTION_ID"

print_status "Checking .NET SDK..."
if ! command -v dotnet &> /dev/null; then
    print_error ".NET SDK is not installed. Please install .NET 8 SDK."
    exit 1
fi

DOTNET_VERSION=$(dotnet --version)
print_success ".NET SDK version: $DOTNET_VERSION"

print_status "Checking Azure Functions Core Tools..."
if ! command -v func &> /dev/null; then
    print_warning "Azure Functions Core Tools not found. Function App deployment may not work."
    print_status "Install with: npm install -g azure-functions-core-tools@4 --unsafe-perm true"
else
    FUNC_VERSION=$(func --version)
    print_success "Azure Functions Core Tools version: $FUNC_VERSION"
fi

# Step 2: Infrastructure Deployment
if [ "$SKIP_INFRASTRUCTURE" = false ]; then
    print_header "Step 2: Infrastructure Deployment"
    
    print_status "Checking if resource group exists..."
    if ! az group show --name "$RESOURCE_GROUP_NAME" > /dev/null 2>&1; then
        print_status "Creating resource group: $RESOURCE_GROUP_NAME"
        az group create --name "$RESOURCE_GROUP_NAME" --location "$LOCATION"
        print_success "Resource group created"
    else
        print_success "Resource group already exists"
    fi
    
    print_status "Deploying infrastructure using Bicep..."
    cd infrastructure
    
    DEPLOYMENT_NAME="leaselogic-deployment-$(date +%Y%m%d-%H%M%S)"
    
    DEPLOYMENT_RESULT=$(az deployment group create \
        --resource-group "$RESOURCE_GROUP_NAME" \
        --name "$DEPLOYMENT_NAME" \
        --template-file "main.bicep" \
        --parameters environment="$ENVIRONMENT" location="$LOCATION" \
        --query "{provisioningState:properties.provisioningState, outputs:properties.outputs}" \
        --output json)
    
    PROVISIONING_STATE=$(echo $DEPLOYMENT_RESULT | jq -r '.provisioningState')
    
    if [ "$PROVISIONING_STATE" = "Succeeded" ]; then
        print_success "Infrastructure deployment completed successfully!"
        
        # Extract outputs
        FUNCTION_APP_NAME=$(echo $DEPLOYMENT_RESULT | jq -r '.outputs.functionAppName.value')
        STORAGE_ACCOUNT_NAME=$(echo $DEPLOYMENT_RESULT | jq -r '.outputs.storageAccountName.value')
        OPENAI_ENDPOINT=$(echo $DEPLOYMENT_RESULT | jq -r '.outputs.openAIEndpoint.value')
        DOC_INTEL_ENDPOINT=$(echo $DEPLOYMENT_RESULT | jq -r '.outputs.documentIntelligenceEndpoint.value')
        KEY_VAULT_URI=$(echo $DEPLOYMENT_RESULT | jq -r '.outputs.keyVaultUri.value')
        
        print_status "Deployment outputs:"
        print_status "  Function App: $FUNCTION_APP_NAME"
        print_status "  Storage Account: $STORAGE_ACCOUNT_NAME"
        print_status "  OpenAI Endpoint: $OPENAI_ENDPOINT"
        print_status "  Document Intelligence: $DOC_INTEL_ENDPOINT"
        print_status "  Key Vault: $KEY_VAULT_URI"
        
        # Save outputs to file
        cat > deployment-outputs.json << EOF
{
    "functionAppName": "$FUNCTION_APP_NAME",
    "storageAccountName": "$STORAGE_ACCOUNT_NAME",
    "openAIEndpoint": "$OPENAI_ENDPOINT",
    "documentIntelligenceEndpoint": "$DOC_INTEL_ENDPOINT",
    "keyVaultUri": "$KEY_VAULT_URI",
    "resourceGroupName": "$RESOURCE_GROUP_NAME"
}
EOF
        print_success "Deployment outputs saved to: infrastructure/deployment-outputs.json"
        
    else
        print_error "Infrastructure deployment failed with state: $PROVISIONING_STATE"
        exit 1
    fi
    
    cd ..
else
    print_header "Step 2: Infrastructure Deployment (SKIPPED)"
    
    # Try to read existing outputs
    if [ -f "infrastructure/deployment-outputs.json" ]; then
        FUNCTION_APP_NAME=$(jq -r '.functionAppName' infrastructure/deployment-outputs.json)
        print_status "Using existing Function App: $FUNCTION_APP_NAME"
    else
        print_error "No existing deployment outputs found. Cannot determine Function App name."
        print_status "Either run without --skip-infrastructure or provide deployment-outputs.json"
        exit 1
    fi
fi

# Step 3: Function App Deployment
if [ "$SKIP_FUNCTION_APP" = false ]; then
    print_header "Step 3: Function App Deployment"
    
    if [ -z "$FUNCTION_APP_NAME" ]; then
        print_error "Function App name not available. Cannot deploy."
        exit 1
    fi
    
    print_status "Building Function App project..."
    cd src/LeaseLogic.Functions
    
    # Restore dependencies
    print_status "Restoring NuGet packages..."
    dotnet restore
    
    # Build project
    print_status "Building project..."
    dotnet build --configuration Release --no-restore
    
    # Publish project
    print_status "Publishing project..."
    dotnet publish --configuration Release --no-build --output ./bin/publish
    
    # Deploy to Azure
    print_status "Deploying to Azure Function App: $FUNCTION_APP_NAME"
    
    if command -v func &> /dev/null; then
        # Use Azure Functions Core Tools
        func azure functionapp publish "$FUNCTION_APP_NAME" --force
        print_success "Function App deployed using Azure Functions Core Tools"
    else
        # Use az cli as fallback
        print_status "Using Azure CLI for deployment (zip deployment)..."
        
        # Create deployment package
        cd ./bin/publish
        zip -r ../deploy.zip . > /dev/null
        cd ..
        
        # Deploy using az cli
        az functionapp deployment source config-zip \
            --resource-group "$RESOURCE_GROUP_NAME" \
            --name "$FUNCTION_APP_NAME" \
            --src deploy.zip
        
        print_success "Function App deployed using Azure CLI"
        
        # Cleanup
        rm -f deploy.zip
    fi
    
    cd ../..
    
    # Wait for deployment to complete
    print_status "Waiting for Function App to start..."
    sleep 30
    
    # Health check
    print_status "Performing health check..."
    FUNCTION_URL="https://$FUNCTION_APP_NAME.azurewebsites.net"
    
    HTTP_STATUS=$(curl -s -o /dev/null -w "%{http_code}" "$FUNCTION_URL" || echo "000")
    if [ "$HTTP_STATUS" -eq "200" ] || [ "$HTTP_STATUS" -eq "404" ]; then
        print_success "Function App is responding (HTTP $HTTP_STATUS)"
    else
        print_warning "Function App health check inconclusive (HTTP $HTTP_STATUS)"
        print_status "This may be normal during initial startup"
    fi
    
else
    print_header "Step 3: Function App Deployment (SKIPPED)"
fi

# Step 4: Post-deployment Setup
print_header "Step 4: Post-deployment Setup"

if [ -n "$FUNCTION_APP_NAME" ]; then
    print_status "Function App URL: https://$FUNCTION_APP_NAME.azurewebsites.net"
    
    print_status "Available API endpoints:"
    print_status "  POST /api/upload-url    - Generate file upload URL"
    print_status "  POST /api/analyze       - Start contract analysis"
    print_status "  GET  /api/status/{id}   - Check analysis status"
    print_status "  GET  /api/result/{id}   - Get analysis result"
    print_status "  DELETE /api/file/{id}   - Delete uploaded file"
    
    # Test basic connectivity
    print_status "Testing basic connectivity..."
    if curl -s --max-time 10 "https://$FUNCTION_APP_NAME.azurewebsites.net" > /dev/null; then
        print_success "Function App is accessible"
    else
        print_warning "Function App may still be starting up"
    fi
fi

# Step 5: Next Steps
print_header "Step 5: Next Steps"

print_status "Deployment completed successfully! 🎉"
echo ""
print_status "Next steps:"
print_status "1. 📄 Upload accounting standards documents to the 'standards' container"
print_status "2. 🤖 Configure OpenAI Assistant with reference documents"
print_status "3. 🧪 Test the API endpoints"
print_status "4. 📊 Set up monitoring and alerts"
print_status "5. 🔒 Configure authentication and CORS settings"

echo ""
print_status "Quick API test:"
echo "curl -X POST \"https://$FUNCTION_APP_NAME.azurewebsites.net/api/upload-url\" \\"
echo "  -H \"Content-Type: application/json\" \\"
echo "  -d '{\"fileName\":\"test.pdf\",\"fileSize\":1000,\"contentType\":\"application/pdf\"}'"

echo ""
print_status "Monitoring:"
print_status "  Azure Portal: https://portal.azure.com/#@/resource/subscriptions/$SUBSCRIPTION_ID/resourceGroups/$RESOURCE_GROUP_NAME"
print_status "  Function App: https://portal.azure.com/#@/resource/subscriptions/$SUBSCRIPTION_ID/resourceGroups/$RESOURCE_GROUP_NAME/providers/Microsoft.Web/sites/$FUNCTION_APP_NAME"

echo ""
print_success "🚀 LeaseLogic deployment completed successfully!"

# Save deployment summary
cat > deployment-summary.json << EOF
{
    "deploymentDate": "$(date -u +%Y-%m-%dT%H:%M:%SZ)",
    "resourceGroupName": "$RESOURCE_GROUP_NAME",
    "location": "$LOCATION",
    "environment": "$ENVIRONMENT",
    "functionAppName": "$FUNCTION_APP_NAME",
    "functionAppUrl": "https://$FUNCTION_APP_NAME.azurewebsites.net",
    "status": "completed"
}
EOF

print_status "Deployment summary saved to: deployment-summary.json"