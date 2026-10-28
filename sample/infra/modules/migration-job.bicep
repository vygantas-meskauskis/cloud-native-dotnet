// EF Core migrations as a manual Container Apps job. The pipeline starts it and waits for success
// BEFORE the apps get the new image, so new code never runs against an old schema.
param location string
param tags object
param environmentId string
param registryServer string
param image string
param identityId string
param identityClientId string
param sqlServerFqdn string
param databaseName string
param appUserName string
param appUserClientId string
param appInsightsConnectionString string

resource job 'Microsoft.App/jobs@2025-01-01' = {
  name: 'job-migrations'
  location: location
  tags: tags
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: { '${identityId}': {} }
  }
  properties: {
    environmentId: environmentId
    workloadProfileName: 'Consumption'
    configuration: {
      triggerType: 'Manual'
      replicaTimeout: 600
      replicaRetryLimit: 1
      manualTriggerConfig: { parallelism: 1, replicaCompletionCount: 1 }
      registries: [ { server: registryServer, identity: identityId } ]
    }
    template: {
      containers: [
        {
          name: 'migrations'
          image: image
          resources: { cpu: json('0.5'), memory: '1Gi' }
          env: [
            { name: 'AZURE_CLIENT_ID', value: identityClientId }
            { name: 'OTEL_SERVICE_NAME', value: 'migrations' }
            { name: 'APPLICATIONINSIGHTS_CONNECTION_STRING', value: appInsightsConnectionString }
            {
              name: 'ConnectionStrings__ledgerlydb'
              value: 'Server=tcp:${sqlServerFqdn},1433;Database=${databaseName};Authentication=Active Directory Managed Identity;User Id=${identityClientId};Encrypt=True;Connect Timeout=60'
            }
            { name: 'Database__AppUser__Name', value: appUserName }
            { name: 'Database__AppUser__ClientId', value: appUserClientId }
          ]
        }
      ]
    }
  }
}

output name string = job.name

