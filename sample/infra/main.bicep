// Ledgerly on Azure. Resource-group scope; the deploy workflow runs it in three phases:
//   1. imageTag = ''                       -> platform only (registry, data, messaging, environment)
//   2. imageTag = <sha>, deployApps = false -> + the migration job with the new image (pipeline runs it)
//   3. imageTag = <sha>, deployApps = true  -> + api, worker, marketdata on the new image
// Deployments are incremental and idempotent: re-running a phase changes only what differs.
targetScope = 'resourceGroup'

@description('Short name used in every resource name.')
@minLength(3)
@maxLength(12)
param appName string = 'ledgerly'

param location string = resourceGroup().location

@description('Container image tag (the git commit SHA). Empty = deploy the platform only.')
param imageTag string = ''

@description('Deploy the long-running apps. The pipeline sets this after the migration job succeeded.')
param deployApps bool = false

@description('Also create Azure Managed Redis. Without it the API cache runs in memory (HybridCache L1 only).')
param enableRedis bool = false

@description('Use the Azure SQL free offer (one per database, up to 10 per subscription).')
param sqlUseFreeLimit bool = true

@description('JWT issuer authority, e.g. https://login.microsoftonline.com/<tenant-id>/v2.0')
param jwtAuthority string

@description('Expected JWT audience, e.g. api://ledgerly-api')
param jwtAudience string

@description('Browser origins allowed to call the API (the future React app).')
param corsOrigins string[] = []

@description('0 = scale to zero (cheapest), 1 = always warm (no cold start).')
@minValue(0)
param apiMinReplicas int = 0

var name = '${appName}-${take(uniqueString(resourceGroup().id), 6)}'
var tags = {
  app: appName
  'managed-by': 'bicep'
}

module monitoring 'modules/monitoring.bicep' = {
  name: 'monitoring'
  params: { location: location, name: name, tags: tags }
}

module identities 'modules/identity.bicep' = {
  name: 'identities'
  params: { location: location, name: name, tags: tags }
}

module registry 'modules/registry.bicep' = {
  name: 'registry'
  params: {
    location: location
    name: name
    tags: tags
    pullPrincipalIds: [identities.outputs.appPrincipalId, identities.outputs.migratorPrincipalId]
  }
}

module keyVault 'modules/keyvault.bicep' = {
  name: 'keyvault'
  params: {
    location: location
    name: name
    tags: tags
    secretReaderPrincipalIds: [identities.outputs.appPrincipalId]
  }
}

module serviceBus 'modules/servicebus.bicep' = {
  name: 'servicebus'
  params: {
    location: location
    name: name
    tags: tags
    dataPrincipalIds: [identities.outputs.appPrincipalId]
    scalerPrincipalId: identities.outputs.appPrincipalId
  }
}

module sql 'modules/sql.bicep' = {
  name: 'sql'
  params: {
    location: location
    name: name
    tags: tags
    adminIdentityName: identities.outputs.migratorName
    adminIdentityClientId: identities.outputs.migratorClientId
    useFreeLimit: sqlUseFreeLimit
  }
}

module redis 'modules/redis.bicep' = if (enableRedis) {
  name: 'redis'
  params: {
    location: location
    name: name
    tags: tags
    keyVaultName: keyVault.outputs.name
  }
}

module environment 'modules/environment.bicep' = {
  name: 'environment'
  params: {
    location: location
    name: name
    tags: tags
    logAnalyticsWorkspaceName: monitoring.outputs.workspaceName
  }
}

module migrationJob 'modules/migration-job.bicep' = if (!empty(imageTag)) {
  name: 'migration-job'
  params: {
    location: location
    tags: tags
    environmentId: environment.outputs.id
    registryServer: registry.outputs.loginServer
    image: '${registry.outputs.loginServer}/ledgerly-migrations:${imageTag}'
    identityId: identities.outputs.migratorId
    identityClientId: identities.outputs.migratorClientId
    sqlServerFqdn: sql.outputs.serverFqdn
    databaseName: sql.outputs.databaseName
    appUserName: identities.outputs.appName
    appUserClientId: identities.outputs.appClientId
    appInsightsConnectionString: monitoring.outputs.appInsightsConnectionString
  }
}

module apps 'modules/apps.bicep' = if (deployApps && !empty(imageTag)) {
  name: 'apps'
  params: {
    location: location
    tags: tags
    environmentId: environment.outputs.id
    registryServer: registry.outputs.loginServer
    imageTag: imageTag
    identityId: identities.outputs.appId
    identityClientId: identities.outputs.appClientId
    sqlServerFqdn: sql.outputs.serverFqdn
    databaseName: sql.outputs.databaseName
    serviceBusNamespace: serviceBus.outputs.name
    serviceBusFqns: serviceBus.outputs.fullyQualifiedNamespace
    appInsightsConnectionString: monitoring.outputs.appInsightsConnectionString
    cacheSecretUri: enableRedis ? redis!.outputs.secretUri : ''
    jwtAuthority: jwtAuthority
    jwtAudience: jwtAudience
    corsOrigins: corsOrigins
    apiMinReplicas: apiMinReplicas
  }
}

output registryName string = registry.outputs.name
output registryLoginServer string = registry.outputs.loginServer
output migrationJobName string = 'job-migrations'
output apiUrl string = (deployApps && !empty(imageTag)) ? apps!.outputs.apiUrl : ''

