# LeaseLogic Complete Deployment Script (PowerShell)
# This script deploys the entire LeaseLogic system from infrastructure to Function App

param(
    [Parameter(Mandatory=$true, HelpMessage="Resource group name")]
    [string]$ResourceGroupName,
    
    [Parameter(Mandatory=$false)]
    [string]$Location = "japaneast",
    
    [Parameter(Mandatory=$false)]
    [string]$Environment = "dev",
    
    [Parameter(Mandatory=$false)]
    [switch]$SkipInfrastructure,
    
    [Parameter(Mandatory=$false)]
    [switch]$SkipFunctionApp,
    
    [Parameter(Mandatory=$false)]
    [switch]$CleanupOnError,
    
    [Parameter(Mandatory=$false)]
    [switch]$Help
)

# Show help
if ($Help) {
    Write-Host @"
LeaseLogic Complete Deployment Script

Usage: .\deploy-all.ps1 -ResourceGroupName <name> [OPTIONS]

Required:
  -ResourceGroupName     Resource group name

Options:
  -Location             Azure region (default: japaneast)
  -Environment          Environment name (default: dev)
  -SkipInfrastructure   Skip infrastructure deployment
  -SkipFunctionApp      Skip Function App deployment
  -CleanupOnError       Delete resource group if deployment fails
  -Help                 Show this help message

Examples:
  .\deploy-all.ps1 -ResourceGroupName "leaselogic-dev-rg"
  .\deploy-all.ps1 -ResourceGroupName "leaselogic-prod-rg" -Environment "prod" -Location "eastus"
  .\deploy-all.ps1 -ResourceGroupName "leaselogic-test-rg" -SkipInfrastructure
"@
    exit 0
}

# Script settings
$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"

# Color functions
function Write-ColorOutput {
    param(
        [string]$Message,
        [string]$Color = "White"
    )
    Write-Host $Message -ForegroundColor $Color
}

function Write-Status {
    param([string]$Message)
    Write-ColorOutput "[INFO] $Message" "Cyan"
}

function Write-Success {
    param([string]$Message)
    Write-ColorOutput "[SUCCESS] $Message" "Green"
}

function Write-Warning {
    param([string]$Message)
    Write-ColorOutput "[WARNING] $Message" "Yellow"
}

function Write-Error {
    param([string]$Message)
    Write-ColorOutput "[ERROR] $Message" "Red"
}

function Write-Header {
    param([string]$Message)
    Write-Host ""
    Write-ColorOutput "================================================" "Blue"
    Write-ColorOutput " $Message" "Blue"
    Write-ColorOutput "================================================" "Blue"
    Write-Host ""
}

# Cleanup function for error handling
function Cleanup-OnError {
    if ($CleanupOnError) {
        Write-Warning "Cleaning up resources due to deployment failure..."
        try {
            Remove-AzResourceGroup -Name $ResourceGroupName -Force -AsJob
            Write-Status "Resource group deletion initiated"
        }
        catch {
            Write-Warning "Failed to cleanup resource group: $($_.Exception.Message)"
        }
    }
}

# Error handling
trap {
    Write-Error "Deployment failed: $($_.Exception.Message)"
    Cleanup-OnError
    exit 1
}

Write-Header "LeaseLogic Complete Deployment Starting"

Write-Status "Configuration:"
Write-Status "  Resource Group: $ResourceGroupName"
Write-Status "  Location: $Location"
Write-Status "  Environment: $Environment"
Write-Status "  Skip Infrastructure: $SkipInfrastructure"
Write-Status "  Skip Function App: $SkipFunctionApp"

# Step 1: Prerequisites check
Write-Header "Step 1: Prerequisites Check"

Write-Status "Checking Azure PowerShell modules..."
$requiredModules = @('Az.Accounts', 'Az.Resources', 'Az.Storage', 'Az.Websites', 'Az.Functions')

foreach ($module in $requiredModules) {
    if (Get-Module -ListAvailable -Name $module) {
        Write-Success "$module is available"
    } else {
        Write-Error "$module is not installed. Please install it with: Install-Module $module"
        exit 1
    }
}

Write-Status "Checking Azure PowerShell authentication..."
try {
    $context = Get-AzContext
    if (-not $context) {
        throw "Not logged in"
    }
    Write-Success "Logged in as: $($context.Account.Id)"
    Write-Status "Subscription: $($context.Subscription.Name) ($($context.Subscription.Id))"
}
catch {
    Write-Error "Not logged in to Azure. Please run 'Connect-AzAccount' first."
    exit 1
}

Write-Status "Checking .NET SDK..."
try {
    $dotnetVersion = & dotnet --version
    Write-Success ".NET SDK version: $dotnetVersion"
}
catch {
    Write-Error ".NET SDK is not installed. Please install .NET 8 SDK."
    exit 1
}

Write-Status "Checking Azure Functions Core Tools..."
try {
    $funcVersion = & func --version
    Write-Success "Azure Functions Core Tools version: $funcVersion"
}
catch {
    Write-Warning "Azure Functions Core Tools not found. Function App deployment may not work."
    Write-Status "Install with: npm install -g azure-functions-core-tools@4 --unsafe-perm true"
}

# Step 2: Infrastructure Deployment
if (-not $SkipInfrastructure) {
    Write-Header "Step 2: Infrastructure Deployment"
    
    Write-Status "Checking if resource group exists..."
    $rg = Get-AzResourceGroup -Name $ResourceGroupName -ErrorAction SilentlyContinue
    if (-not $rg) {
        Write-Status "Creating resource group: $ResourceGroupName"
        New-AzResourceGroup -Name $ResourceGroupName -Location $Location
        Write-Success "Resource group created"
    } else {
        Write-Success "Resource group already exists"
    }
    
    Write-Status "Deploying infrastructure using Bicep..."
    Set-Location "infrastructure"
    
    $deploymentName = "leaselogic-deployment-$(Get-Date -Format 'yyyyMMdd-HHmmss')"
    
    try {
        $deployment = New-AzResourceGroupDeployment `
            -ResourceGroupName $ResourceGroupName `
            -Name $deploymentName `
            -TemplateFile "main.bicep" `
            -environment $Environment `
            -location $Location `
            -Verbose

        if ($deployment.ProvisioningState -eq "Succeeded") {
            Write-Success "Infrastructure deployment completed successfully!"
            
            # Extract outputs
            $functionAppName = $deployment.Outputs.functionAppName.Value
            $storageAccountName = $deployment.Outputs.storageAccountName.Value
            $openAIEndpoint = $deployment.Outputs.openAIEndpoint.Value
            $docIntelEndpoint = $deployment.Outputs.documentIntelligenceEndpoint.Value
            $keyVaultUri = $deployment.Outputs.keyVaultUri.Value
            
            Write-Status "Deployment outputs:"
            Write-Status "  Function App: $functionAppName"
            Write-Status "  Storage Account: $storageAccountName"
            Write-Status "  OpenAI Endpoint: $openAIEndpoint"
            Write-Status "  Document Intelligence: $docIntelEndpoint"
            Write-Status "  Key Vault: $keyVaultUri"
            
            # Save outputs to file
            $outputs = @{
                functionAppName = $functionAppName
                storageAccountName = $storageAccountName
                openAIEndpoint = $openAIEndpoint
                documentIntelligenceEndpoint = $docIntelEndpoint
                keyVaultUri = $keyVaultUri
                resourceGroupName = $ResourceGroupName
            }
            
            $outputs | ConvertTo-Json -Depth 10 | Out-File -FilePath "deployment-outputs.json" -Encoding UTF8
            Write-Success "Deployment outputs saved to: infrastructure/deployment-outputs.json"
            
        } else {
            Write-Error "Infrastructure deployment failed with state: $($deployment.ProvisioningState)"
            exit 1
        }
    }
    catch {
        Write-Error "Infrastructure deployment failed: $($_.Exception.Message)"
        exit 1
    }
    
    Set-Location ".."
} else {
    Write-Header "Step 2: Infrastructure Deployment (SKIPPED)"
    
    # Try to read existing outputs
    if (Test-Path "infrastructure/deployment-outputs.json") {
        $outputs = Get-Content "infrastructure/deployment-outputs.json" | ConvertFrom-Json
        $functionAppName = $outputs.functionAppName
        Write-Status "Using existing Function App: $functionAppName"
    } else {
        Write-Error "No existing deployment outputs found. Cannot determine Function App name."
        Write-Status "Either run without -SkipInfrastructure or provide deployment-outputs.json"
        exit 1
    }
}

# Step 3: Function App Deployment
if (-not $SkipFunctionApp) {
    Write-Header "Step 3: Function App Deployment"
    
    if (-not $functionAppName) {
        Write-Error "Function App name not available. Cannot deploy."
        exit 1
    }
    
    Write-Status "Building Function App project..."
    Set-Location "src/LeaseLogic.Functions"
    
    # Restore dependencies
    Write-Status "Restoring NuGet packages..."
    & dotnet restore
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Failed to restore NuGet packages"
        exit 1
    }
    
    # Build project
    Write-Status "Building project..."
    & dotnet build --configuration Release --no-restore
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Failed to build project"
        exit 1
    }
    
    # Publish project
    Write-Status "Publishing project..."
    & dotnet publish --configuration Release --no-build --output "./bin/publish"
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Failed to publish project"
        exit 1
    }
    
    # Deploy to Azure
    Write-Status "Deploying to Azure Function App: $functionAppName"
    
    try {
        # Try Azure Functions Core Tools first
        & func azure functionapp publish $functionAppName --force
        if ($LASTEXITCODE -eq 0) {
            Write-Success "Function App deployed using Azure Functions Core Tools"
        } else {
            throw "Azure Functions Core Tools deployment failed"
        }
    }
    catch {
        Write-Status "Falling back to Azure PowerShell deployment..."
        
        # Create deployment package
        Set-Location "./bin/publish"
        Compress-Archive -Path "*" -DestinationPath "../deploy.zip" -Force
        Set-Location ".."
        
        # Deploy using Azure PowerShell
        try {
            Publish-AzWebApp -ResourceGroupName $ResourceGroupName -Name $functionAppName -ArchivePath "deploy.zip" -Force
            Write-Success "Function App deployed using Azure PowerShell"
        }
        catch {
            Write-Error "Failed to deploy Function App: $($_.Exception.Message)"
            exit 1
        }
        finally {
            # Cleanup
            if (Test-Path "deploy.zip") {
                Remove-Item "deploy.zip" -Force
            }
        }
    }
    
    Set-Location "../.."
    
    # Wait for deployment to complete
    Write-Status "Waiting for Function App to start..."
    Start-Sleep -Seconds 30
    
    # Health check
    Write-Status "Performing health check..."
    $functionUrl = "https://$functionAppName.azurewebsites.net"
    
    try {
        $response = Invoke-WebRequest -Uri $functionUrl -TimeoutSec 10 -ErrorAction SilentlyContinue
        $httpStatus = $response.StatusCode
        if ($httpStatus -eq 200 -or $httpStatus -eq 404) {
            Write-Success "Function App is responding (HTTP $httpStatus)"
        } else {
            Write-Warning "Function App health check inconclusive (HTTP $httpStatus)"
        }
    }
    catch {
        Write-Warning "Function App health check inconclusive"
        Write-Status "This may be normal during initial startup"
    }
    
} else {
    Write-Header "Step 3: Function App Deployment (SKIPPED)"
}

# Step 4: Post-deployment Setup
Write-Header "Step 4: Post-deployment Setup"

if ($functionAppName) {
    Write-Status "Function App URL: https://$functionAppName.azurewebsites.net"
    
    Write-Status "Available API endpoints:"
    Write-Status "  POST /api/upload-url    - Generate file upload URL"
    Write-Status "  POST /api/analyze       - Start contract analysis"
    Write-Status "  GET  /api/status/{id}   - Check analysis status"
    Write-Status "  GET  /api/result/{id}   - Get analysis result"
    Write-Status "  DELETE /api/file/{id}   - Delete uploaded file"
    
    # Test basic connectivity
    Write-Status "Testing basic connectivity..."
    try {
        $null = Invoke-WebRequest -Uri "https://$functionAppName.azurewebsites.net" -TimeoutSec 10 -ErrorAction Stop
        Write-Success "Function App is accessible"
    }
    catch {
        Write-Warning "Function App may still be starting up"
    }
}

# Step 5: Next Steps
Write-Header "Step 5: Next Steps"

Write-Success "Deployment completed successfully! 🎉"
Write-Host ""
Write-Status "Next steps:"
Write-Status "1. 📄 Upload accounting standards documents to the 'standards' container"
Write-Status "2. 🤖 Configure OpenAI Assistant with reference documents"
Write-Status "3. 🧪 Test the API endpoints"
Write-Status "4. 📊 Set up monitoring and alerts"
Write-Status "5. 🔒 Configure authentication and CORS settings"

Write-Host ""
Write-Status "Quick API test:"
Write-Host "Invoke-RestMethod -Uri 'https://$functionAppName.azurewebsites.net/api/upload-url' ``"
Write-Host "  -Method Post ``"
Write-Host "  -ContentType 'application/json' ``"
Write-Host "  -Body (ConvertTo-Json @{fileName='test.pdf';fileSize=1000;contentType='application/pdf'})"

$subscriptionId = (Get-AzContext).Subscription.Id
Write-Host ""
Write-Status "Monitoring:"
Write-Status "  Azure Portal: https://portal.azure.com/#@/resource/subscriptions/$subscriptionId/resourceGroups/$ResourceGroupName"
Write-Status "  Function App: https://portal.azure.com/#@/resource/subscriptions/$subscriptionId/resourceGroups/$ResourceGroupName/providers/Microsoft.Web/sites/$functionAppName"

Write-Host ""
Write-Success "🚀 LeaseLogic deployment completed successfully!"

# Save deployment summary
$summary = @{
    deploymentDate = (Get-Date).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")
    resourceGroupName = $ResourceGroupName
    location = $Location
    environment = $Environment
    functionAppName = $functionAppName
    functionAppUrl = "https://$functionAppName.azurewebsites.net"
    status = "completed"
}

$summary | ConvertTo-Json | Out-File -FilePath "deployment-summary.json" -Encoding UTF8
Write-Status "Deployment summary saved to: deployment-summary.json"