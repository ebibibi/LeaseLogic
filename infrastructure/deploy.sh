#!/bin/bash

# Azure LeaseLogic Infrastructure Deployment Script (Bash)

set -e

# Default values
LOCATION="japaneast"
ENVIRONMENT="dev"
PARAMETERS_FILE="parameters.json"

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
        -p|--parameters-file)
            PARAMETERS_FILE="$2"
            shift 2
            ;;
        -h|--help)
            echo "Usage: $0 -g <resource-group-name> [-l <location>] [-e <environment>] [-p <parameters-file>]"
            echo ""
            echo "Options:"
            echo "  -g, --resource-group    Resource group name (required)"
            echo "  -l, --location         Azure region (default: japaneast)"
            echo "  -e, --environment      Environment name (default: dev)"
            echo "  -p, --parameters-file  Parameters file (default: parameters.json)"
            echo "  -h, --help            Show this help message"
            exit 0
            ;;
        *)
            echo "Unknown option $1"
            exit 1
            ;;
    esac
done

# Check required parameters
if [ -z "$RESOURCE_GROUP_NAME" ]; then
    echo "❌ Error: Resource group name is required. Use -g option."
    echo "Use $0 --help for usage information."
    exit 1
fi

echo "🚀 Starting LeaseLogic Infrastructure Deployment"
echo "Resource Group: $RESOURCE_GROUP_NAME"
echo "Location: $LOCATION"
echo "Environment: $ENVIRONMENT"

# Check if logged in to Azure
echo "📋 Checking Azure CLI login..."
if ! az account show > /dev/null 2>&1; then
    echo "❌ Please login to Azure first: az login"
    exit 1
fi

ACCOUNT_INFO=$(az account show --query "{subscriptionId:id, tenantId:tenantId, user:user.name}" -o json)
echo "✅ Azure login verified: $(echo $ACCOUNT_INFO | jq -r '.user')"
echo "   Subscription: $(echo $ACCOUNT_INFO | jq -r '.subscriptionId')"

# Create resource group if it doesn't exist
echo "📦 Checking resource group..."
if ! az group show --name "$RESOURCE_GROUP_NAME" > /dev/null 2>&1; then
    echo "Creating resource group: $RESOURCE_GROUP_NAME"
    az group create --name "$RESOURCE_GROUP_NAME" --location "$LOCATION"
    echo "✅ Resource group created"
else
    echo "✅ Resource group exists"
fi

# Deploy Bicep template
echo "🏗️  Deploying infrastructure..."
DEPLOYMENT_NAME="leaselogic-deployment-$(date +%Y%m%d-%H%M%S)"

echo "Starting deployment: $DEPLOYMENT_NAME"

DEPLOYMENT_RESULT=$(az deployment group create \
    --resource-group "$RESOURCE_GROUP_NAME" \
    --name "$DEPLOYMENT_NAME" \
    --template-file "main.bicep" \
    --parameters "@$PARAMETERS_FILE" \
    --parameters environment="$ENVIRONMENT" location="$LOCATION" \
    --query "{provisioningState:properties.provisioningState, outputs:properties.outputs}" \
    --output json)

PROVISIONING_STATE=$(echo $DEPLOYMENT_RESULT | jq -r '.provisioningState')

if [ "$PROVISIONING_STATE" = "Succeeded" ]; then
    echo "✅ Infrastructure deployment completed successfully!"
    
    # Extract and display outputs
    echo ""
    echo "📋 Deployment Outputs:"
    
    FUNCTION_APP_NAME=$(echo $DEPLOYMENT_RESULT | jq -r '.outputs.functionAppName.value')
    FUNCTION_APP_URL=$(echo $DEPLOYMENT_RESULT | jq -r '.outputs.functionAppUrl.value')
    STORAGE_ACCOUNT_NAME=$(echo $DEPLOYMENT_RESULT | jq -r '.outputs.storageAccountName.value')
    OPENAI_ENDPOINT=$(echo $DEPLOYMENT_RESULT | jq -r '.outputs.openAIEndpoint.value')
    DOC_INTEL_ENDPOINT=$(echo $DEPLOYMENT_RESULT | jq -r '.outputs.documentIntelligenceEndpoint.value')
    KEY_VAULT_URI=$(echo $DEPLOYMENT_RESULT | jq -r '.outputs.keyVaultUri.value')
    APP_INSIGHTS_CONNECTION=$(echo $DEPLOYMENT_RESULT | jq -r '.outputs.appInsightsConnectionString.value')
    RESOURCE_GROUP_OUTPUT=$(echo $DEPLOYMENT_RESULT | jq -r '.outputs.resourceGroupName.value')
    
    echo "Function App Name: $FUNCTION_APP_NAME"
    echo "Function App URL: $FUNCTION_APP_URL"
    echo "Storage Account: $STORAGE_ACCOUNT_NAME"
    echo "OpenAI Endpoint: $OPENAI_ENDPOINT"
    echo "Document Intelligence Endpoint: $DOC_INTEL_ENDPOINT"
    echo "Key Vault URI: $KEY_VAULT_URI"
    echo "Application Insights Connection String: $APP_INSIGHTS_CONNECTION"
    
    # Save outputs to file
    cat > deployment-outputs.json << EOF
{
    "functionAppName": "$FUNCTION_APP_NAME",
    "functionAppUrl": "$FUNCTION_APP_URL",
    "storageAccountName": "$STORAGE_ACCOUNT_NAME",
    "openAIEndpoint": "$OPENAI_ENDPOINT",
    "documentIntelligenceEndpoint": "$DOC_INTEL_ENDPOINT",
    "keyVaultUri": "$KEY_VAULT_URI",
    "appInsightsConnectionString": "$APP_INSIGHTS_CONNECTION",
    "resourceGroupName": "$RESOURCE_GROUP_OUTPUT"
}
EOF
    
    echo "💾 Deployment outputs saved to: deployment-outputs.json"
    
else
    echo "❌ Deployment failed with state: $PROVISIONING_STATE"
    exit 1
fi

echo ""
echo "🎉 LeaseLogic infrastructure deployment completed!"
echo ""
echo "Next steps:"
echo "1. Deploy the Function App code"
echo "2. Upload accounting standards documents to the 'standards' container"
echo "3. Create and configure OpenAI Assistant with reference documents"