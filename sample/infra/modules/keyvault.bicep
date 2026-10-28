// Key Vault in RBAC mode (access policies are legacy). Apps read secrets via Key Vault references
// in Container Apps, so the secret value never appears in the pipeline or in app settings.
param location string
param name string
param tags object
param secretReaderPrincipalIds string[]

var secretsUserRoleId = '4633458b-17de-408a-b874-0445c86b69e6'

resource vault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: 'kv-${take(name, 15)}-${take(uniqueString(resourceGroup().id), 4)}'
  location: location
  tags: tags
  properties: {
    tenantId: subscription().tenantId
    sku: { family: 'A', name: 'standard' }
    enableRbacAuthorization: true
    enableSoftDelete: true
    softDeleteRetentionInDays: 7
  }
}

resource secretsUser 'Microsoft.Authorization/roleAssignments@2022-04-01' = [for principalId in secretReaderPrincipalIds: {
  name: guid(vault.id, principalId, secretsUserRoleId)
  scope: vault
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', secretsUserRoleId)
    principalId: principalId
    principalType: 'ServicePrincipal'
  }
}]

output name string = vault.name
output uri string = vault.properties.vaultUri
