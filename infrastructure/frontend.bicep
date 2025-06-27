@description('Environment name (dev, staging, prod)')
param environment string = 'dev'

@description('Location for all resources')
param location string = resourceGroup().location

@description('Unique suffix for resource names')
param uniqueSuffix string = uniqueString(resourceGroup().id)

@description('Function App name for CORS configuration')
param functionAppName string

// Variables
var projectName = 'leaselogic'
var namePrefix = '${projectName}-${environment}-${uniqueSuffix}'

// Resource names
var appServicePlanName = '${namePrefix}-frontend-plan'
var webAppName = '${namePrefix}-frontend'

// App Service Plan for Frontend (Basic tier for cost efficiency)
resource frontendAppServicePlan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: appServicePlanName
  location: location
  sku: {
    name: 'B1'
    tier: 'Basic'
  }
  properties: {
    reserved: false // Windows
  }
}

// Web App for Frontend
resource frontendWebApp 'Microsoft.Web/sites@2023-12-01' = {
  name: webAppName
  location: location
  properties: {
    serverFarmId: frontendAppServicePlan.id
    httpsOnly: true
    siteConfig: {
      nodeVersion: '~18'
      appSettings: [
        {
          name: 'WEBSITE_NODE_DEFAULT_VERSION'
          value: '~18'
        }
        {
          name: 'SCM_DO_BUILD_DURING_DEPLOYMENT'
          value: 'true'
        }
        {
          name: 'REACT_APP_API_BASE_URL'
          value: 'https://${functionAppName}.azurewebsites.net'
        }
        {
          name: 'NODE_ENV'
          value: environment == 'prod' ? 'production' : 'development'
        }
      ]
      cors: {
        allowedOrigins: [
          'https://${functionAppName}.azurewebsites.net'
        ]
        supportCredentials: false
      }
      defaultDocuments: [
        'index.html'
      ]
      httpLoggingEnabled: true
      logsDirectorySizeLimit: 35
      detailedErrorLoggingEnabled: true
      minTlsVersion: '1.2'
      ftpsState: 'Disabled'
    }
  }
}

// Configure Function App CORS to allow frontend
resource functionApp 'Microsoft.Web/sites@2023-12-01' existing = {
  name: functionAppName
}

resource functionAppCors 'Microsoft.Web/sites/config@2023-12-01' = {
  parent: functionApp
  name: 'web'
  properties: {
    cors: {
      allowedOrigins: [
        'https://${webAppName}.azurewebsites.net'
        'http://localhost:3000' // For local development
      ]
      supportCredentials: false
    }
  }
}

// Outputs
output frontendAppServicePlanName string = frontendAppServicePlan.name
output frontendWebAppName string = frontendWebApp.name
output frontendWebAppUrl string = 'https://${frontendWebApp.properties.defaultHostName}'
output frontendWebAppHostName string = frontendWebApp.properties.defaultHostName