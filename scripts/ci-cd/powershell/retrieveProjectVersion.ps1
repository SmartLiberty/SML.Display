# This is a minimal script used to retrieve the project version from csproj file.
param(
	#Csproj file path you want to retrieve the project version.
    [Parameter()]
    [string]$FilePath,
	
	#Balise you want to retrieve into Poject Property Group segment.
	#Default value : Version
	[Parameter()]
    [string]$BaliseName = "Version"
	
)
Write-Host "Get the content file : $($FilePath)" -ForegroundColor "White"
Write-Host "Retrieve the project property group version into the balise : $($BaliseName)" -ForegroundColor "White"
$xml = [Xml] (Get-Content $FilePath)
$version = $xml.Project.PropertyGroup.$BaliseName
Write-Host "Version retrieaved : $($version)" -ForegroundColor "White"
echo $version
echo "##vso[task.setvariable variable=version]$version"