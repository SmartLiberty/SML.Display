<#
.SYNOPSIS
This script copies .proto files from NuGet packages to a destination folder.

.DESCRIPTION
This script parses a .csproj file to find NuGet package references and copies .proto files from those packages to a specified destination folder. This scripts is usefull to prepare the camouflage project after a dotnet restore.

.PARAMETER csprojFilePath
The path to the .csproj file to analyze.

.PARAMETER nuGetPackageRoot
The root directory of NuGet packages on the local system.

.PARAMETER destinationPath
The destination folder where .proto files will be copied.

.EXAMPLE
.\copy-proto-from-package-references-in-csproj.ps1  -csprojFilePath "C:\Users\USER\sources\csproj-and-workflow\SML.HelpVideos\sources\SML.HelpVideos.Tests.System\SML.HelpVideos.Tests.System.csproj" -nuGetPackageRoot "C:\Users\USER\.nuget\packages" -destinationPath "C:\Users\USER\sources\csproj-and-workflow\SML.HelpVideos\sources\SML.HelpVideos.Tests.System\camouflage.mock.server\camouflage\grpc\protos"
#>

param(
    [Parameter()]
    [string]$csprojFilePath,
    [Parameter()]
    [string]$nuGetPackageRoot,
    [Parameter()]
    [string]$destinationPath
)

$protoRegex = ".*(SML|Smart).*Proto.*"
# Charger le contenu du fichier csproj
[xml]$csprojContent = Get-Content $csprojFilePath

# Sélectionner tous les éléments PackageReference
$packageReferences = $csprojContent.Project.ItemGroup | Where-Object { $_.PackageReference }

Write-Host " Display infos"
Write-Host " csprojFilePath :   $($csprojFilePath)"
Write-Host " nuGetPackageRoot : $($nuGetPackageRoot)"
Write-Host " destinationPath :  $($destinationPath)"


# Parcourir la liste des PackageReference et afficher Include et Version
Write-Host " "
Write-Host " Total $($packageReferences.PackageReference.Count) "
Write-Host " ------"
$packageReferences.PackageReference  | ForEach-Object {
     Write-Host "Include: $($_.Include), Version: $($_.Version)"
}

Write-Host " "
Write-Host "Result $($($packageReferences.PackageReference | Where {$_.Include -Match $protoRegex }).Count) on $($packageReferences.PackageReference.Count)"
Write-Host " ------"
$packageReferences.PackageReference | Where {$_.Include -Match $protoRegex } | ForEach-Object {
     Write-Host "Include: $($_.Include), Version: $($_.Version)"
}

Write-Host " "
Write-Host "Copy recursively all proto file from all package"
Write-Host " ------"
$packageReferences.PackageReference | Where {$_.Include -Match $protoRegex } | ForEach-Object {
    $packagePath = Join-Path $nuGetPackageRoot "$($_.Include.ToLower())/$($_.Version)/content"
    $protoFiles = Get-ChildItem -Path $packagePath -Recurse -Filter "*.proto"
    if ($protoFiles.Count -gt 0) {
        foreach ($file in $protoFiles) {
            Write-Host " "
            $destinationFilePath = Join-Path $destinationPath $file.Name
            if (-not (Test-Path -Path $destinationPath -PathType Container)) {
                Write-Host " Create folder $destinationPath"
                New-Item -Path $destinationPath -ItemType Directory -Force
            } 
            Write-Host " COPY : $($file.FullName) to $destinationFilePath"
            Copy-Item -Path $file.FullName -Destination $destinationFilePath -Force
        }
    }
}
