// Values for the "production" environment. Secrets never go here: there are none to pass,
// because every service authenticates with a managed identity.
using 'main.bicep'

param appName = 'ledgerly'
param jwtAuthority = 'https://login.microsoftonline.com/<your-tenant-id>/v2.0'
param jwtAudience = 'api://ledgerly-api'
param corsOrigins = []
param enableRedis = false
param apiMinReplicas = 0

// imageTag and deployApps are passed by the pipeline on the command line.
