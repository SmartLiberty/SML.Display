# ---------------------------------------------------------------------------------|
# INSTALL                                                                          |
# ---------------------------------------------------------------------------------/

# Names
[string] $serviceName = "SML.ExampleGrpc"

# Time
[double] $timeout = 25 * 1000 # [milliseconds]
[double] $waitTime = 1.0

# Env
[string] $dataPath = "$Env:SML_INSTALL__DATA"
[string] $binPath = "$Env:SML_INSTALL__BIN"
[string] $installationPath = "$binPath/$serviceName"
[string] $exePath = $installationPath + "/$serviceName.exe"
[bool] $serviceExists = -not [string]::IsNullOrEmpty($(Get-Service -Name "$serviceName" -ErrorAction SilentlyContinue))

# DB
[bool] $installDb = $true

# Import & Loading
# -------------------------------------/

$location = Get-Location
$moduleName = "DeploymentModule.psm1"
Import-Module "$location\$moduleName"

# Main
# -------------------------------------/

try {  
    Install-SMLService `
        -ServiceName $serviceName `
        -Timeout $timeout `
        -WaitTime $waitTime `
        -InstallationPath $installationPath `
        -DataPath $dataPath `
        -BinPath $binPath `
        -ExePath $exePath `
        -InstallDb $installDb `
        -ServiceExists $serviceExists `
        -CreateService $true
}
catch {
    Write-Error "Unexpected error was caught."
    Write-Error "$_"
    exit 1
}
