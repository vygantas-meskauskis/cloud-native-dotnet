// Service Bus Standard (needed for duplicate detection). Local (SAS key) auth is disabled:
// only Entra identities with a data role can send or receive.
param location string
param name string
param tags object
param dataPrincipalIds string[]
@description('Identity the KEDA scale rule uses to read the queue length.')
param scalerPrincipalId string

var senderRoleId = '69a216fc-b8fb-44d8-bc22-1f3c2cd27a39'
var receiverRoleId = '4f6d3b9b-027b-4f4c-9142-0e5a2a2247e0'
var dataOwnerRoleId = '090c5cfd-751d-490a-894a-3ce6f1109419'

resource namespace 'Microsoft.ServiceBus/namespaces@2024-01-01' = {
  name: 'sb-${name}'
  location: location
  tags: tags
  sku: { name: 'Standard', tier: 'Standard' }
  properties: {
    disableLocalAuth: true
    minimumTlsVersion: '1.2'
  }
}

resource imports 'Microsoft.ServiceBus/namespaces/queues@2024-01-01' = {
  parent: namespace
  name: 'imports'
  properties: {
    lockDuration: 'PT1M'
    maxDeliveryCount: 10 // after 10 failed attempts the message moves to imports/$deadletterqueue
    requiresDuplicateDetection: true // the same MessageId within the window is dropped
    duplicateDetectionHistoryTimeWindow: 'PT10M'
    deadLetteringOnMessageExpiration: true
  }
}

resource roles 'Microsoft.Authorization/roleAssignments@2022-04-01' = [for item in flatten(map(dataPrincipalIds, p => [
  { principalId: p, roleId: senderRoleId }
  { principalId: p, roleId: receiverRoleId }
])): {
  name: guid(namespace.id, item.principalId, item.roleId)
  scope: namespace
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', item.roleId)
    principalId: item.principalId
    principalType: 'ServicePrincipal'
  }
}]

// 🔧 KEDA reads the queue's message count through the management API, which needs Manage rights.
// Sender and Receiver don't include them, so grant Data Owner on this one queue only, not the namespace.
resource scalerRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(imports.id, scalerPrincipalId, dataOwnerRoleId)
  scope: imports
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', dataOwnerRoleId)
    principalId: scalerPrincipalId
    principalType: 'ServicePrincipal'
  }
}

output name string = namespace.name
output fullyQualifiedNamespace string = '${namespace.name}.servicebus.windows.net'
output queueName string = imports.name
