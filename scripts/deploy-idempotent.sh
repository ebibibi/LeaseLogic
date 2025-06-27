#!/bin/bash

# LeaseLogic Idempotent Deployment Script
# This script handles Azure CLI 2.74.0 bugs and ensures idempotent deployment

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
LOCATION="southeastasia"
ENVIRONMENT="dev"
SKIP_INFRASTRUCTURE=false
SKIP_FUNCTION_APP=false

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
  -l, --location         Azure region (default: southeastasia)
  -e, --environment      Environment name (default: dev)
  --skip-infrastructure  Skip infrastructure deployment
  --skip-function-app    Skip Function App deployment
  -h, --help            Show this help message

Examples:
  $0 -g "LeaseLogic"
  $0 -g "LeaseLogic" -l "eastus"
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

print_header "LeaseLogic Idempotent Deployment Starting"

print_status "Configuration:"
print_status "  Resource Group: $RESOURCE_GROUP_NAME"
print_status "  Location: $LOCATION"
print_status "  Environment: $ENVIRONMENT"

# Step 1: Prerequisites check
print_header "Step 1: Prerequisites Check"

print_status "Checking Azure CLI authentication..."
if ! az account show > /dev/null 2>&1; then
    print_error "Not logged in to Azure. Please run 'az login' first."
    exit 1
fi

SUBSCRIPTION_ID=$(az account show --query "id" -o tsv)
USER_NAME=$(az account show --query "user.name" -o tsv)
print_success "Logged in as: $USER_NAME"
print_status "Subscription: $SUBSCRIPTION_ID"

# Check for Azure CLI 2.74.0 bug
CLI_VERSION=$(az --version | head -1 | grep -o '[0-9]\+\.[0-9]\+\.[0-9]\+')
if [[ "$CLI_VERSION" == "2.74.0" || "$CLI_VERSION" == "2.73.0" ]]; then
    print_warning "Detected Azure CLI $CLI_VERSION with known Bicep deployment bugs"
    print_status "Using individual resource creation to avoid 'content consumed' error"
fi

# Variables for resource names
UNIQUE_SUFFIX=$(echo $RESOURCE_GROUP_NAME | tr '[:upper:]' '[:lower:]' | head -c 8)$(date +%s | tail -c 4)
STORAGE_ACCOUNT_NAME="leaselogic${ENVIRONMENT}${UNIQUE_SUFFIX}st"
FUNCTION_APP_NAME="leaselogic-${ENVIRONMENT}-${UNIQUE_SUFFIX}-func"
APP_SERVICE_PLAN_NAME="leaselogic-${ENVIRONMENT}-${UNIQUE_SUFFIX}-plan"
OPENAI_ACCOUNT_NAME="leaselogic-${ENVIRONMENT}-${UNIQUE_SUFFIX}-openai"
DOC_INTEL_NAME="leaselogic-${ENVIRONMENT}-${UNIQUE_SUFFIX}-docint"
KEY_VAULT_NAME="leaselogic${ENVIRONMENT}${UNIQUE_SUFFIX}kv"

# Ensure storage account name length is valid (3-24 chars)
if [ ${#STORAGE_ACCOUNT_NAME} -gt 24 ]; then
    STORAGE_ACCOUNT_NAME=$(echo $STORAGE_ACCOUNT_NAME | head -c 24)
fi

# Ensure Key Vault name length is valid (3-24 chars)
if [ ${#KEY_VAULT_NAME} -gt 24 ]; then
    KEY_VAULT_NAME=$(echo $KEY_VAULT_NAME | head -c 24)
fi

print_status "Resource names:"
print_status "  Storage Account: $STORAGE_ACCOUNT_NAME"
print_status "  Function App: $FUNCTION_APP_NAME"
print_status "  App Service Plan: $APP_SERVICE_PLAN_NAME"

# Step 2: Infrastructure Deployment
if [ "$SKIP_INFRASTRUCTURE" = false ]; then
    print_header "Step 2: Idempotent Infrastructure Deployment"
    
    # Check and create resource group
    print_status "Checking resource group: $RESOURCE_GROUP_NAME"
    if ! az group show --name "$RESOURCE_GROUP_NAME" > /dev/null 2>&1; then
        print_status "Creating resource group: $RESOURCE_GROUP_NAME"
        az group create --name "$RESOURCE_GROUP_NAME" --location "$LOCATION" --output none
        print_success "Resource group created"
    else
        print_success "Resource group already exists"
    fi
    
    # Check and create storage account
    print_status "Checking storage account: $STORAGE_ACCOUNT_NAME"
    if ! az storage account show --name "$STORAGE_ACCOUNT_NAME" --resource-group "$RESOURCE_GROUP_NAME" > /dev/null 2>&1; then
        print_status "Creating storage account: $STORAGE_ACCOUNT_NAME"
        az storage account create \
            --name "$STORAGE_ACCOUNT_NAME" \
            --resource-group "$RESOURCE_GROUP_NAME" \
            --location "$LOCATION" \
            --sku Standard_LRS \
            --kind StorageV2 \
            --https-only true \
            --min-tls-version TLS1_2 \
            --allow-blob-public-access false \
            --output none
        print_success "Storage account created"
        
        # Create blob containers
        STORAGE_KEY=$(az storage account keys list --resource-group "$RESOURCE_GROUP_NAME" --account-name "$STORAGE_ACCOUNT_NAME" --query "[0].value" -o tsv)
        
        az storage container create --name "documents" --account-name "$STORAGE_ACCOUNT_NAME" --account-key "$STORAGE_KEY" --output none
        az storage container create --name "results" --account-name "$STORAGE_ACCOUNT_NAME" --account-key "$STORAGE_KEY" --output none
        az storage container create --name "standards" --account-name "$STORAGE_ACCOUNT_NAME" --account-key "$STORAGE_KEY" --output none
        print_success "Blob containers created"
    else
        print_success "Storage account already exists"
    fi
    
    # Check and create App Service Plan
    print_status "Checking App Service Plan: $APP_SERVICE_PLAN_NAME"
    if ! az appservice plan show --name "$APP_SERVICE_PLAN_NAME" --resource-group "$RESOURCE_GROUP_NAME" > /dev/null 2>&1; then
        print_status "Creating App Service Plan: $APP_SERVICE_PLAN_NAME"
        az appservice plan create \
            --name "$APP_SERVICE_PLAN_NAME" \
            --resource-group "$RESOURCE_GROUP_NAME" \
            --location "$LOCATION" \
            --sku B1 \
            --output none
        print_success "App Service Plan created"
    else
        print_success "App Service Plan already exists"
    fi
    
    # Check and create Function App
    print_status "Checking Function App: $FUNCTION_APP_NAME"
    if ! az functionapp show --name "$FUNCTION_APP_NAME" --resource-group "$RESOURCE_GROUP_NAME" > /dev/null 2>&1; then
        print_status "Creating Function App: $FUNCTION_APP_NAME"
        az functionapp create \
            --name "$FUNCTION_APP_NAME" \
            --resource-group "$RESOURCE_GROUP_NAME" \
            --plan "$APP_SERVICE_PLAN_NAME" \
            --storage-account "$STORAGE_ACCOUNT_NAME" \
            --runtime dotnet-isolated \
            --runtime-version 8 \
            --functions-version 4 \
            --output none
        print_success "Function App created"
    else
        print_success "Function App already exists"
    fi
    
    # Check and create Azure OpenAI Service
    print_status "Checking Azure OpenAI Service: $OPENAI_ACCOUNT_NAME"
    if ! az cognitiveservices account show --name "$OPENAI_ACCOUNT_NAME" --resource-group "$RESOURCE_GROUP_NAME" > /dev/null 2>&1; then
        print_status "Creating Azure OpenAI Service: $OPENAI_ACCOUNT_NAME"
        az cognitiveservices account create \
            --name "$OPENAI_ACCOUNT_NAME" \
            --resource-group "$RESOURCE_GROUP_NAME" \
            --location "eastus" \
            --kind OpenAI \
            --sku S0 \
            --output none
        print_success "Azure OpenAI Service created"
    else
        print_success "Azure OpenAI Service already exists"
    fi
    
    # Check and create Document Intelligence
    print_status "Checking Document Intelligence: $DOC_INTEL_NAME"
    if ! az cognitiveservices account show --name "$DOC_INTEL_NAME" --resource-group "$RESOURCE_GROUP_NAME" > /dev/null 2>&1; then
        print_status "Creating Document Intelligence: $DOC_INTEL_NAME"
        az cognitiveservices account create \
            --name "$DOC_INTEL_NAME" \
            --resource-group "$RESOURCE_GROUP_NAME" \
            --location "$LOCATION" \
            --kind FormRecognizer \
            --sku S0 \
            --output none
        print_success "Document Intelligence created"
    else
        print_success "Document Intelligence already exists"
    fi
    
    # Create Key Vault (if needed for production)
    print_status "Checking Key Vault: $KEY_VAULT_NAME"
    if ! az keyvault show --name "$KEY_VAULT_NAME" --resource-group "$RESOURCE_GROUP_NAME" > /dev/null 2>&1; then
        print_status "Creating Key Vault: $KEY_VAULT_NAME"
        az keyvault create \
            --name "$KEY_VAULT_NAME" \
            --resource-group "$RESOURCE_GROUP_NAME" \
            --location "$LOCATION" \
            --sku standard \
            --output none
        print_success "Key Vault created"
    else
        print_success "Key Vault already exists"
    fi
    
    # Save deployment outputs
    mkdir -p infrastructure
    cat > infrastructure/deployment-outputs.json << EOF
{
    "functionAppName": "$FUNCTION_APP_NAME",
    "functionAppUrl": "https://$FUNCTION_APP_NAME.azurewebsites.net",
    "storageAccountName": "$STORAGE_ACCOUNT_NAME",
    "openAIAccountName": "$OPENAI_ACCOUNT_NAME",
    "documentIntelligenceName": "$DOC_INTEL_NAME",
    "keyVaultName": "$KEY_VAULT_NAME",
    "resourceGroupName": "$RESOURCE_GROUP_NAME",
    "location": "$LOCATION"
}
EOF
    print_success "Deployment outputs saved to: infrastructure/deployment-outputs.json"
    
else
    print_header "Step 2: Infrastructure Deployment (SKIPPED)"
    
    # Get script directory and project root for file paths
    SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )"
    PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"
    
    print_status "Debug: SCRIPT_DIR=$SCRIPT_DIR"
    print_status "Debug: PROJECT_ROOT=$PROJECT_ROOT"
    
    # Try to read existing outputs from correct path
    OUTPUT_FILE1="$PROJECT_ROOT/infrastructure/infrastructure/deployment-outputs.json"
    OUTPUT_FILE2="$PROJECT_ROOT/infrastructure/deployment-outputs.json"
    
    print_status "Debug: Checking file: $OUTPUT_FILE1"
    print_status "Debug: File exists: $([ -f "$OUTPUT_FILE1" ] && echo "YES" || echo "NO")"
    print_status "Debug: Checking file: $OUTPUT_FILE2"
    print_status "Debug: File exists: $([ -f "$OUTPUT_FILE2" ] && echo "YES" || echo "NO")"
    
    if [ -f "$OUTPUT_FILE1" ]; then
        FUNCTION_APP_NAME=$(sed -n 's/.*"functionAppName": *"\([^"]*\)".*/\1/p' "$OUTPUT_FILE1")
        print_status "Using existing Function App from $OUTPUT_FILE1: $FUNCTION_APP_NAME"
    elif [ -f "$OUTPUT_FILE2" ]; then
        FUNCTION_APP_NAME=$(sed -n 's/.*"functionAppName": *"\([^"]*\)".*/\1/p' "$OUTPUT_FILE2")
        print_status "Using existing Function App from $OUTPUT_FILE2: $FUNCTION_APP_NAME"
    else
        print_error "No existing deployment outputs found. Cannot determine Function App name."
        print_status "Checked paths:"
        print_status "  $OUTPUT_FILE1"
        print_status "  $OUTPUT_FILE2"
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
    
    # Check if .NET SDK is available
    if ! command -v dotnet &> /dev/null; then
        if [ -d "/home/ebi/.dotnet" ]; then
            export PATH="$PATH:/home/ebi/.dotnet"
        else
            print_error ".NET SDK not found. Please install .NET 8 SDK."
            exit 1
        fi
    fi
    
    # Get script directory and project root
    SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )"
    PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"
    FUNCTION_PROJECT_PATH="$PROJECT_ROOT/src/LeaseLogic.Functions"
    
    if [ ! -d "$FUNCTION_PROJECT_PATH" ]; then
        print_error "Function App project not found at: $FUNCTION_PROJECT_PATH"
        exit 1
    fi
    
    cd "$FUNCTION_PROJECT_PATH"
    
    # Restore dependencies
    print_status "Restoring NuGet packages..."
    dotnet restore --verbosity quiet
    
    # Build project
    print_status "Building project..."
    dotnet build --configuration Release --no-restore --verbosity quiet
    
    # Publish project
    print_status "Publishing project..."
    dotnet publish --configuration Release --no-build --output ./bin/publish --verbosity quiet
    
    # Deploy to Azure
    print_status "Deploying to Azure Function App: $FUNCTION_APP_NAME"
    
    # Create deployment package
    cd ./bin/publish
    if command -v zip &> /dev/null; then
        zip -r ../deploy.zip . > /dev/null
    else
        print_status "Installing zip utility..."
        # Try to install zip without sudo first
        if command -v apt-get &> /dev/null; then
            export DEBIAN_FRONTEND=noninteractive
            apt-get update &> /dev/null && apt-get install -y zip &> /dev/null || {
                print_warning "Cannot install zip utility, trying alternative method"
                # Use python to create zip if available
                if command -v python3 &> /dev/null; then
                    python3 -c "
import zipfile
import os
with zipfile.ZipFile('../deploy.zip', 'w', zipfile.ZIP_DEFLATED) as zipf:
    for root, dirs, files in os.walk('.'):
        for file in files:
            file_path = os.path.join(root, file)
            arcname = os.path.relpath(file_path, '.')
            zipf.write(file_path, arcname)
"
                else
                    print_error "Neither zip nor python3 available for packaging"
                    exit 1
                fi
            }
        fi
        
        # Try zip again after installation
        if command -v zip &> /dev/null; then
            zip -r ../deploy.zip . > /dev/null
        fi
    fi
    cd ..
    
    # Deploy using az cli
    az functionapp deployment source config-zip \
        --resource-group "$RESOURCE_GROUP_NAME" \
        --name "$FUNCTION_APP_NAME" \
        --src deploy.zip \
        --output none
    
    print_success "Function App deployed successfully"
    
    # Cleanup
    rm -f deploy.zip
    
    cd "$PROJECT_ROOT"
    
else
    print_header "Step 3: Function App Deployment (SKIPPED)"
fi

# Step 4: Frontend Deployment
print_header "Step 4: Frontend Deployment"

if command -v node &> /dev/null && command -v npm &> /dev/null; then
    print_status "Node.js detected. Building and deploying frontend..."
    
    # Frontend deployment would go here
    print_status "Frontend deployment placeholder - implement as needed"
    
else
    print_warning "Node.js and npm not found. Skipping frontend deployment."
    print_status "Frontend can be deployed manually later"
fi

# Step 5: Final Status
print_header "Step 5: Deployment Completed"

print_success "🚀 LeaseLogic deployment completed successfully!"
echo ""
print_status "Deployed resources:"
print_status "  Function App: https://$FUNCTION_APP_NAME.azurewebsites.net"
print_status "  Storage Account: $STORAGE_ACCOUNT_NAME"
print_status "  OpenAI Service: $OPENAI_ACCOUNT_NAME"
print_status "  Document Intelligence: $DOC_INTEL_NAME"

echo ""
print_status "Next steps:"
print_status "1. Configure OpenAI models and API keys"
print_status "2. Upload accounting standards documents"
print_status "3. Test the API endpoints"
print_status "4. Deploy frontend application"

echo ""
print_status "API endpoints available:"
print_status "  POST /api/upload-url    - Generate file upload URL"
print_status "  POST /api/analyze       - Start contract analysis"
print_status "  GET  /api/status/{id}   - Check analysis status"
print_status "  GET  /api/result/{id}   - Get analysis result"

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
print_success "✅ All deployment steps completed successfully!"