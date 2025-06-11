# PowerShell script to delete all resources created by the Bicep template in the 'Learning' resource group

$resourceGroup = "Learning"
$webAppName = "b2capprole"
$appServicePlanName = "b2capprole-plan"
$functionAppName = "B2CappRole"
$functionPlanName = "B2CappRole-flex-plan"
$keyVaultName = "kv-entra-dev-eastus-01"

# Delete Web App
az webapp delete --name $webAppName --resource-group $resourceGroup

# Delete App Service Plan
az appservice plan delete --name $appServicePlanName --resource-group $resourceGroup --yes

# Delete Function App
az functionapp delete --name $functionAppName --resource-group $resourceGroup

# Delete Function Plan (Consumption plan is an App Service Plan)
az appservice plan delete --name $functionPlanName --resource-group $resourceGroup --yes

# Delete Key Vault
az keyvault delete --name $keyVaultName --resource-group $resourceGroup