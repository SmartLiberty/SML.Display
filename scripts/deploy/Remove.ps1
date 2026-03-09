# ---------------------------------------------------------------------------------|
# REMOVE                                                                           |
# ---------------------------------------------------------------------------------/

[string] $serviceName = "SML.ExampleGrpc"
[string] $databaseName = $serviceName

# Time
[double] $timeout = 25 * 1000 # [milliseconds]
[double] $waitTime = 1.0

# ---------------------------------------------------------------------------------|
# Dynamic variables (have to change between scripts by reloading the module)
# ---------------------------------------------------------------------------------/

# DB
[string] $databaseUrl = $env:SML_DATABASE__URL ? $env:SML_DATABASE__URL : "localhost";
[string] $databasePort = $env:SML_DATABASE__PORT ? $env:SML_DATABASE__PORT : "5432";
[string] $databaseUser = $env:SML_DATABASE__USER ? $env:SML_DATABASE__USER : "postgres";
[string] $databasePassword = $env:SML_DATABASE__PASSWORD ? $env:SML_DATABASE__PASSWORD : "postgresPWD";

[string] $dataPath = "$Env:SML_INSTALL__DATA"
[string] $binPath = "$Env:SML_INSTALL__BIN"
[string] $installationPath = "$binPath/$serviceName"
[string] $exePath = $installationPath + "/$serviceName.exe"

[bool] $serviceExists = -not [string]::IsNullOrEmpty($(Get-Service -Name "$serviceName" -ErrorAction SilentlyContinue))

[string] $sqlConnectionString = "host=$databaseUrl port=$databasePort dbname=postgres user=$databaseUser password=$databasePassword"
[string] $dbExists = psql -tAc "SELECT 1 FROM pg_database WHERE datname='$databaseName'" "$sqlConnectionString"
[string] $closeSqlQuery = "SELECT pg_terminate_backend(pg_stat_activity.pid) FROM pg_stat_activity WHERE pg_stat_activity.datname = '$databaseName'AND pid <> pg_backend_pid();"

# Import
# -------------------------------------/

$location = Get-Location
$moduleName = "DeploymentModule.psm1"
Import-Module "$location\$moduleName"

# Main
# -------------------------------------/

try {
    Remove-SMLService `
        -ServiceName $serviceName `
        -DatabaseName $databaseName `
        -Timeout $timeout `
        -WaitTime $waitTime `
        -InstallationPath $installationPath `
        -DataPath $dataPath `
        -BinPath $binPath `
        -ExePath $exePath `
        -DbExists $dbExists `
        -ServiceExists $serviceExists `
        -RemoveDataPath $false `
        -RemoveDatabase $true `
        -SqlConnectionString $sqlConnectionString `
        -CloseSqlQuery $closeSqlQuery
}
catch {
    Write-Error "Unexpected error was caught. Unable to remove the service $serviceName."
    Write-Error "$_"
    exit 1
}
