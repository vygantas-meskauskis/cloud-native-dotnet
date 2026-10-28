// Azure SQL with Microsoft Entra-only authentication (no SQL logins, no passwords) and a
// serverless General Purpose database that uses the free offer and auto-pauses when idle.
param location string
param name string
param tags object
param adminIdentityName string
param adminIdentityClientId string
param useFreeLimit bool = true

resource server 'Microsoft.Sql/servers@2023-08-01' = {
  name: 'sql-${name}'
  location: location
  tags: tags
  properties: {
    minimalTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
    administrators: {
      administratorType: 'ActiveDirectory'
      principalType: 'Application'
      login: adminIdentityName
      sid: adminIdentityClientId // 🔧 for a managed identity, the Learn Bicep sample uses the client id
      tenantId: subscription().tenantId
      azureADOnlyAuthentication: true
    }
  }
}

// "Allow Azure services" (0.0.0.0). Simple for a first deployment; production uses a VNet + private endpoint.
resource allowAzure 'Microsoft.Sql/servers/firewallRules@2023-08-01' = {
  parent: server
  name: 'AllowAllWindowsAzureIps'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

resource database 'Microsoft.Sql/servers/databases@2023-08-01' = {
  parent: server
  name: 'ledgerly'
  location: location
  tags: tags
  sku: {
    name: 'GP_S_Gen5_2'
    tier: 'GeneralPurpose'
    family: 'Gen5'
    capacity: 2
  }
  properties: {
    autoPauseDelay: 60
    minCapacity: json('0.5')
    useFreeLimit: useFreeLimit // 💰 100,000 vCore-seconds per month, then pause until next month
    freeLimitExhaustionBehavior: useFreeLimit ? 'AutoPause' : null
  }
}

output serverFqdn string = server.properties.fullyQualifiedDomainName
output databaseName string = database.name
