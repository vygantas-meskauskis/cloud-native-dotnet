// Two user-assigned managed identities. No passwords or connection-string secrets anywhere:
//  - "app": what api, worker and marketdata run as (least privilege: read/write data, send/receive messages).
//  - "migrator": what the migration job runs as. It is the SQL Entra admin, so it can change the schema.
param location string
param name string
param tags object

resource app 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: 'id-${name}-app'
  location: location
  tags: tags
}

resource migrator 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: 'id-${name}-migrator'
  location: location
  tags: tags
}

output appId string = app.id
output appName string = app.name
output appClientId string = app.properties.clientId
output appPrincipalId string = app.properties.principalId
output migratorId string = migrator.id
output migratorName string = migrator.name
output migratorClientId string = migrator.properties.clientId
output migratorPrincipalId string = migrator.properties.principalId
