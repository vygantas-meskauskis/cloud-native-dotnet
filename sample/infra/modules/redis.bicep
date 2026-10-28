// Optional: Azure Managed Redis (the successor of Azure Cache for Redis, which is retiring).
// The access-key connection string goes straight into Key Vault and is never output.
// Entra auth is the stronger option, see chapter 13.
param location string
param name string
param tags object
param keyVaultName string

resource redis 'Microsoft.Cache/redisEnterprise@2025-07-01' = {
  name: 'redis-${name}'
  location: location
  tags: tags
  sku: { name: 'Balanced_B0' } // 💰 smallest size, still a paid resource
  properties: {
    minimumTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled' // production: 'Disabled' + a private endpoint
  }
}

resource database 'Microsoft.Cache/redisEnterprise/databases@2025-07-01' = {
  parent: redis
  name: 'default'
  properties: {
    clientProtocol: 'Encrypted'
    port: 10000
    clusteringPolicy: 'OSSCluster'
    evictionPolicy: 'AllKeysLRU'
    accessKeysAuthentication: 'Enabled'
  }
}

resource vault 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
  name: keyVaultName
}

resource secret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: vault
  name: 'cache-connection'
  properties: {
    value: '${redis.properties.hostName}:10000,password=${database.listKeys().primaryKey},ssl=True,abortConnect=False'
  }
}

output secretUri string = secret.properties.secretUri

