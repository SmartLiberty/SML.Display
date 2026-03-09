param (
     [string]$FeedName,
     [string]$PackageName,
     [string]$PackageVersion ,
     [string]$PAT 
)

if ($FeedName -eq $null) {
    Write-Error "FeedName it's null !"
    exit -1
}

if ($PackageName -eq $null) {
    Write-Error "PackageName it's null !"
    exit -1
}

if ($PackageVersion -eq $null) {
    Write-Error "PackageVersion it's null !"
    exit -1
}

if ($PAT -eq $null) {
    Write-Error "PAT it's null !"
    exit -1
}

$base64AuthInfo= [System.Convert]::ToBase64String([System.Text.Encoding]::ASCII.GetBytes(":$($PAT)"))

$urlPackage= "https://feeds.dev.azure.com/SmartDevQA/SmartDevOps/_apis/packaging/Feeds/$($FeedName)/packages?packageNameQuery=$($PackageName)&getTopPackageVersions=$($PackageVersion)&api-version=7.0"


$PackagesInfo = Invoke-RestMethod -Uri $urlPackage -Headers @{Authorization = ("Basic {0}" -f $base64AuthInfo)} -Method get

$package
foreach ($p in $PackagesInfo.value)
{
  if($p.normalizedName -eq $PackageName){
    $package = $p
  }
}

if ($package.normalizedName -eq $null) {
    Write-Host "Package $($PackageName) not found in feed $($FeedName)!"
    Write-Host "The $($PackageName) $($PackageVersion) dosen't exist in Feed $($FeedName) ! it seems like ok."
    exit 0
}else{
    Write-Host "Package $($PackageName) found in Feed $($FeedName)!"
}


Write-Host "Check if $($PackageVersion) version exist."
$urlVersion= "https://feeds.dev.azure.com/SmartDevQA/SmartDevOps/_apis/packaging/Feeds/$($FeedName)/packages/$($package.id)/versions/$($PackageVersion)?includeDeleted=true&isListed=null&api-version=7.0"

try{
    $VerInfo = Invoke-RestMethod -Uri $urlVersion -Headers @{Authorization = ("Basic {0}" -f $base64AuthInfo)} -Method get
}catch{
    if ($_.innerException -eq $null) {
            Write-Host "The $($PackageName) $($PackageVersion) dosen't exist in Feed $($FeedName) ! it seems like ok."
            exit 0;
    }
    elseif ($_.innerException -ne $null) {
            Error-Error "An error with exception ocured"
            Write-Error "$($_.innerException)"
            Write-Error "$($_.message)"
            exit -1;
    }
}
if($VerInfo.normalizedVersion -ne $null ){
    Write-Host $VerInfo.normalizedVersion
    Write-Error "The $($PackageName) $($PackageVersion) already exist in Feed $($FeedName)!"
    exit -1;
}



