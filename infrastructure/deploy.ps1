# Azure LeaseLogic Infrastructure Deployment Script

param(
    [Parameter(Mandatory=$true)]
    [string]$ResourceGroupName,
    
    [Parameter(Mandatory=$false)]
    [string]$Location = "japaneast",
    
    [Parameter(Mandatory=$false)]
    [string]$Environment = "dev",
    
    [Parameter(Mandatory=$false)]
    [string]$ParametersFile = "parameters.json"
)

# Script settings
$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"

Write-Host "🚀 Starting LeaseLogic Infrastructure Deployment" -ForegroundColor Green
Write-Host "Resource Group: $ResourceGroupName" -ForegroundColor Yellow
Write-Host "Location: $Location" -ForegroundColor Yellow
Write-Host "Environment: $Environment" -ForegroundColor Yellow

# Check if logged in to Azure
try {
    $context = Get-AzContext
    if (-not $context) {
        throw "Not logged in"
    }
    Write-Host "✅ Azure login verified: $($context.Account.Id)" -ForegroundColor Green
}
catch {
    Write-Host "❌ Please login to Azure first: Connect-AzAccount" -ForegroundColor Red
    exit 1
}

# Create resource group if it doesn't exist
Write-Host "📦 Checking resource group..." -ForegroundColor Cyan
$rg = Get-AzResourceGroup -Name $ResourceGroupName -ErrorAction SilentlyContinue
if (-not $rg) {
    Write-Host "Creating resource group: $ResourceGroupName" -ForegroundColor Yellow
    New-AzResourceGroup -Name $ResourceGroupName -Location $Location
    Write-Host "✅ Resource group created" -ForegroundColor Green
} else {
    Write-Host "✅ Resource group exists" -ForegroundColor Green
}

# Deploy Bicep template
Write-Host "🏗️  Deploying infrastructure..." -ForegroundColor Cyan
$deploymentName = "leaselogic-deployment-$(Get-Date -Format 'yyyyMMdd-HHmmss')"

try {
    $deployment = New-AzResourceGroupDeployment `
        -ResourceGroupName $ResourceGroupName `
        -Name $deploymentName `
        -TemplateFile "main.bicep" `
        -TemplateParameterFile $ParametersFile `
        -environment $Environment `
        -location $Location `
        -Verbose

    if ($deployment.ProvisioningState -eq "Succeeded") {
        Write-Host "✅ Infrastructure deployment completed successfully!" -ForegroundColor Green
        
        # Display outputs
        Write-Host "`n📋 Deployment Outputs:" -ForegroundColor Cyan
        Write-Host "Function App Name: $($deployment.Outputs.functionAppName.Value)" -ForegroundColor Yellow
        Write-Host "Function App URL: $($deployment.Outputs.functionAppUrl.Value)" -ForegroundColor Yellow
        Write-Host "Storage Account: $($deployment.Outputs.storageAccountName.Value)" -ForegroundColor Yellow
        Write-Host "OpenAI Endpoint: $($deployment.Outputs.openAIEndpoint.Value)" -ForegroundColor Yellow
        Write-Host "Document Intelligence Endpoint: $($deployment.Outputs.documentIntelligenceEndpoint.Value)" -ForegroundColor Yellow
        Write-Host "Key Vault URI: $($deployment.Outputs.keyVaultUri.Value)" -ForegroundColor Yellow
        Write-Host "Application Insights Connection String: $($deployment.Outputs.appInsightsConnectionString.Value)" -ForegroundColor Yellow
        
        # Save outputs to file
        $outputs = @{
            functionAppName = $deployment.Outputs.functionAppName.Value
            functionAppUrl = $deployment.Outputs.functionAppUrl.Value
            storageAccountName = $deployment.Outputs.storageAccountName.Value
            openAIEndpoint = $deployment.Outputs.openAIEndpoint.Value
            documentIntelligenceEndpoint = $deployment.Outputs.documentIntelligenceEndpoint.Value
            keyVaultUri = $deployment.Outputs.keyVaultUri.Value
            appInsightsConnectionString = $deployment.Outputs.appInsightsConnectionString.Value
            resourceGroupName = $deployment.Outputs.resourceGroupName.Value
        }
        
        $outputs | ConvertTo-Json -Depth 10 | Out-File -FilePath "deployment-outputs.json" -Encoding UTF8
        Write-Host "💾 Deployment outputs saved to: deployment-outputs.json" -ForegroundColor Green
        
    } else {
        Write-Host "❌ Deployment failed with state: $($deployment.ProvisioningState)" -ForegroundColor Red
        exit 1
    }
}
catch {
    Write-Host "❌ Deployment failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host "`n🎉 LeaseLogic infrastructure deployment completed!" -ForegroundColor Green
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "1. Deploy the Function App code" -ForegroundColor White
Write-Host "2. Upload accounting standards documents to the 'standards' container" -ForegroundColor White
Write-Host "3. Create and configure OpenAI Assistant with reference documents" -ForegroundColor White