# ---------------------------------------------------------------------------------|
# RESTART                                                                          |
# ---------------------------------------------------------------------------------/

[string] $serviceName = "SML.ExampleGrpc"

# Time
[double] $timeout = 25 * 1000 # [milliseconds]

[bool] $serviceExists = -not [string]::IsNullOrEmpty($(Get-Service -Name "$serviceName" -ErrorAction SilentlyContinue))

# Import
# -------------------------------------/

$location = Get-Location
$moduleName = "DeploymentModule.psm1"
Import-Module "$location\$moduleName"

# Main
# -------------------------------------/

try {
    Restart-SMLService `
        -ServiceName $serviceName `
        -Timeout $timeout `
        -ServiceExists $serviceExists
}
catch {
    Write-Error "Unexpected error was caught. Unable to restart the service $serviceName."
    Write-Error "$_"
    exit 1
}
