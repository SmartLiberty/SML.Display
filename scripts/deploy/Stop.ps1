# ---------------------------------------------------------------------------------|
# STOP                                                                             |
# ---------------------------------------------------------------------------------/

[string] $serviceName = "SML.Display"

# Time
[double] $timeout = 25 * 1000 # [milliseconds]

[bool] $serviceExists = -not [string]::IsNullOrEmpty($(Get-Service -Name "$serviceName" -ErrorAction SilentlyContinue))

# # Import & Loading
# -------------------------------------/

$location = Get-Location
$moduleName = "DeploymentModule.psm1"
Import-Module "$location\$moduleName"

# Main
# -------------------------------------/

try {
    Stop-SMLService `
        -ServiceName $serviceName `
        -Timeout $timeout `
        -ServiceExists $serviceExists
}
catch {
    Write-Error "Unexpected error was caught. Unable to stop the service $serviceName."
    Write-Error "$_"
    exit 1
}
