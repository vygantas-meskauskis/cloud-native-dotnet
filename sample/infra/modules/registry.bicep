// Private container registry. No admin user: Container Apps pull with a managed identity (AcrPull).
param location string
@minLength(5)
param name string
param tags object
param pullPrincipalIds string[]

var acrPullRoleId = '7f951dda-4ed3-4680-a7ca-43fe172d538d'

resource registry 'Microsoft.ContainerRegistry/registries@2023-07-01' = {
  #disable-next-line BCP334 // name is "ledgerly-xxxxxx", so the result is long enough; Bicep can't infer it through replace()
  name: 'cr${replace(name, '-', '')}'
  location: location
  tags: tags
  sku: { name: 'Basic' }
  properties: {
    adminUserEnabled: false
  }
}

resource acrPull 'Microsoft.Authorization/roleAssignments@2022-04-01' = [for principalId in pullPrincipalIds: {
  name: guid(registry.id, principalId, acrPullRoleId)
  scope: registry
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', acrPullRoleId)
    principalId: principalId
    principalType: 'ServicePrincipal'
  }
}]

output name string = registry.name
output loginServer string = registry.properties.loginServer

