param($exeBackupZipFile = "", $dbBackupFile="")

$serviceName = "SML.ExampleGrpc"

# Check that PowerShell 64-bits is used
$intSize = [intptr]::size
if($intSize -eq 4)
{
    Write-Host " Script is currently running in PowerShell 32-bits, but needs 64-bits to run properly. Exiting..."
    Exit
}

$dataPath = $Env:SML_INSTALL__DATA
if([string]::IsNullOrEmpty($dataPath) -Or -Not (Test-Path $dataPath))
{
	Write-Host "Error, missing data path. Exiting..."
	Exit
	
}
$binaryPath = "$Env:SML_INSTALL__BIN/$serviceName"
if(-Not (Test-Path $binaryPath))
{
	Write-Host "Error, missing binary path ($binaryPath). Exiting..."
	Exit
}

if((-Not [string]::IsNullOrEmpty($dbBackupFile)) -And (-Not (Test-Path $dbBackupFile)))
{
    Write-Host "Error, file $dbBackupFile doesn't exists. Exiting..."
    Exit
}
Write-Host "Found database backup file : $dbBackupFile"

if(-Not (Test-Path $exeBackupZipFile))
{
	Write-Output "Error, executables backup file $exeBackupZipFile doesn't exists. Exiting..."
	Exit
}
Write-Host "Found executables backup file : $exeBackupZipFile"

Write-Host "Stopping PG Admin..."
Stop-Process -name pgadmin3 -ErrorAction SilentlyContinue
Stop-Process -name pgAdmin4 -ErrorAction SilentlyContinue

Get-Service -Name $serviceName | Stop-Service -ErrorAction SilentlyContinue

$proc = Get-Process $serviceName -ErrorAction SilentlyContinue
if($proc){
	Write-Host "Killing process $proc.Name"
	$proc.kill()
	Write-Host "Waiting 2 seconds"
	sleep 2
}

Write-Host "Remove current software"

Get-ChildItem -Path "$($binaryPath)/" | Remove-Item -Recurse -Force 
if(-Not [string]::IsNullOrEmpty($dbBackupFile))
{
	Write-Host "Remove current database"
	dropdb --if-exists --username=$env:SML_DATABASE__USER -w $serviceName  
}

Write-Host "Restore executables"
7z x """$exeBackupZipFile""" -aoa -o"""$Env:SML_INSTALL__BIN""" | Out-Null

if(-Not [string]::IsNullOrEmpty($dbBackupFile))
{
	Write-Host "Restore database $serviceName"
	createdb --username=$env:SML_DATABASE__USER -w -E UTF8 $serviceName
	pg_restore --username=$env:SML_DATABASE__USER -w -d $serviceName $dbBackupFile
}
Get-Service -Name $serviceName | Start-Service 

Write-Host "End of rollback"