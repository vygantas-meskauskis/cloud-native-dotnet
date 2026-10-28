// The three long-running services. Each gets probes mapped to the ASP.NET Core health endpoints:
//   startup + liveness -> /alive  (is the process responsive?)
//   readiness          -> /health (are dependencies reachable? if not, take it out of rotation)
param location string
param tags object
param environmentId string
param registryServer string
param imageTag string
param identityId string
param identityClientId string
param sqlServerFqdn string
param databaseName string
param serviceBusNamespace string
param serviceBusFqns string
param appInsightsConnectionString string
param cacheSecretUri string
param jwtAuthority string
param jwtAudience string
param corsOrigins string[]
param apiMinReplicas int

var sqlConnection = 'Server=tcp:${sqlServerFqdn},1433;Database=${databaseName};Authentication=Active Directory Managed Identity;User Id=${identityClientId};Encrypt=True;Connect Timeout=60'

var commonEnv = [
  { name: 'AZURE_CLIENT_ID', value: identityClientId } // makes Azure SDK credentials pick the user-assigned identity
  { name: 'APPLICATIONINSIGHTS_CONNECTION_STRING', value: appInsightsConnectionString }
]

var probes = [
  {
    type: 'Startup'
    httpGet: { path: '/alive', port: 8080 }
    initialDelaySeconds: 2
    periodSeconds: 3
    failureThreshold: 20
  }
  {
    type: 'Liveness'
    httpGet: { path: '/alive', port: 8080 }
    periodSeconds: 10
    failureThreshold: 3
  }
  {
    type: 'Readiness'
    httpGet: { path: '/health', port: 8080 }
    periodSeconds: 10
    failureThreshold: 3
  }
]

var identity = {
  type: 'UserAssigned'
  userAssignedIdentities: { '${identityId}': {} }
}

var registries = [ { server: registryServer, identity: identityId } ]

var corsEnv = [for (origin, i) in corsOrigins: { name: 'Cors__AllowedOrigins__${i}', value: origin }]

resource marketdata 'Microsoft.App/containerApps@2025-01-01' = {
  name: 'marketdata'
  location: location
  tags: tags
  identity: identity
  properties: {
    environmentId: environmentId
    workloadProfileName: 'Consumption'
    configuration: {
      activeRevisionsMode: 'Single'
      registries: registries
      ingress: { external: false, targetPort: 8080, transport: 'auto' } // reachable only inside the environment
    }
    template: {
      containers: [
        {
          name: 'marketdata'
          image: '${registryServer}/ledgerly-marketdata:${imageTag}'
          resources: { cpu: json('0.25'), memory: '0.5Gi' }
          env: concat(commonEnv, [ { name: 'OTEL_SERVICE_NAME', value: 'marketdata' } ])
          probes: probes
        }
      ]
      scale: { minReplicas: 0, maxReplicas: 3 }
    }
  }
}

var marketDataUrl = 'https://${marketdata.properties.configuration.ingress.fqdn}'

resource api 'Microsoft.App/containerApps@2025-01-01' = {
  name: 'api'
  location: location
  tags: tags
  identity: identity
  properties: {
    environmentId: environmentId
    workloadProfileName: 'Consumption'
    configuration: {
      activeRevisionsMode: 'Single'
      registries: registries
      ingress: {
        external: true
        targetPort: 8080
        transport: 'auto'
        allowInsecure: false
      }
      // A Key Vault reference: Container Apps fetches the value with the managed identity.
      secrets: empty(cacheSecretUri) ? [] : [
        { name: 'cache-connection', keyVaultUrl: cacheSecretUri, identity: identityId }
      ]
    }
    template: {
      containers: [
        {
          name: 'api'
          image: '${registryServer}/ledgerly-api:${imageTag}'
          resources: { cpu: json('0.5'), memory: '1Gi' }
          env: concat(commonEnv, [
            { name: 'OTEL_SERVICE_NAME', value: 'api' }
            { name: 'ConnectionStrings__ledgerlydb', value: sqlConnection }
            { name: 'MarketData__BaseUrl', value: marketDataUrl }
            { name: 'Authentication__Schemes__Bearer__Authority', value: jwtAuthority }
            { name: 'Authentication__Schemes__Bearer__ValidAudiences__0', value: jwtAudience }
          ], empty(cacheSecretUri) ? [] : [
            { name: 'ConnectionStrings__cache', secretRef: 'cache-connection' }
          ], corsEnv)
          probes: probes
        }
      ]
      scale: {
        minReplicas: apiMinReplicas // 0 = no cost when idle but a cold start; 1 = always warm
        maxReplicas: 10
        rules: [
          { name: 'http', http: { metadata: { concurrentRequests: '50' } } }
        ]
      }
    }
  }
}

resource worker 'Microsoft.App/containerApps@2025-01-01' = {
  name: 'worker'
  location: location
  tags: tags
  identity: identity
  properties: {
    environmentId: environmentId
    workloadProfileName: 'Consumption'
    configuration: {
      activeRevisionsMode: 'Single'
      registries: registries
      // No ingress: nothing calls the worker. Probes still reach port 8080 inside the replica.
    }
    template: {
      containers: [
        {
          name: 'worker'
          image: '${registryServer}/ledgerly-worker:${imageTag}'
          resources: { cpu: json('0.5'), memory: '1Gi' }
          env: concat(commonEnv, [
            { name: 'OTEL_SERVICE_NAME', value: 'worker' }
            { name: 'ConnectionStrings__ledgerlydb', value: sqlConnection }
            { name: 'ConnectionStrings__messaging', value: serviceBusFqns } // a namespace, not a key: Entra auth
            { name: 'MarketData__BaseUrl', value: marketDataUrl }
          ])
          probes: probes
        }
      ]
      scale: {
        // 1, not 0: the outbox relay and price alerts poll on a timer, so one replica must always run.
        minReplicas: 1
        maxReplicas: 5
        rules: [
          {
            // KEDA watches the queue length and adds replicas when imports pile up.
            name: 'imports-queue'
            custom: {
              type: 'azure-servicebus'
              metadata: {
                namespace: serviceBusNamespace
                queueName: 'imports'
                messageCount: '20'
              }
              identity: identityId
            }
          }
        ]
      }
    }
  }
}

output apiUrl string = 'https://${api.properties.configuration.ingress.fqdn}'
output latestApiRevision string = api.properties.latestRevisionName

