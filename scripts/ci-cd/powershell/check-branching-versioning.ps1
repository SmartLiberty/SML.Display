<#
.SYNOPSIS
Validates the version against a branch of your project using semantic versioning and branching strategies.

.DESCRIPTION
This script validates that the version of your project is following the branching strategies, depending on the branch it is on.

.PARAMETER Version
The version of your project.

.PARAMETER Branch
The git branch of your project is following.

.EXAMPLE
.\check-branching-versioning -Version "1.2.3-alpha.1" -Branch "refs/heads/develop"
Validates that the version of the project is following the branching strategies for the develop branch.
#>

param(
    # The version of your Project.
    [Parameter()]
    [string]$Version,

    # The git branch.
    [Parameter()]
    [string]$Branch
)

Write-Host "Validating your project version with a semantic versioning and branching strategies conditions." -ForegroundColor "Blue"
Write-Host " - Version : $($Version)" -ForegroundColor "White"
Write-Host " - Branch  : $($Branch)" -ForegroundColor "White"

# Define regex patterns for validation
$semverMainPattern = '^(\d+)\.(\d+)\.(\d+)$'                          # x.y.z
$semverBranchPattern = '^(\d+)\.(\d+)\.(\d+)-(alpha|beta)\.(\d+)$'    # x.y.z-suffix.n

# Validate version format and ensure components are not zero-padded
if ($Version -match $semverMainPattern -or $Version -match $semverBranchPattern) {
    # Check if any of the components (major, minor, patch) are zero-padded
    if ($matches[1] -match '^0\d+' -or $matches[2] -match '^0\d+' -or $matches[3] -match '^0\d+') {
        Write-Host "Version components (major, minor, patch) must not be zero-padded." -ForegroundColor "Red"
        Exit 1
    }

    # Extract version components as integers
    $major = [int]$matches[1]
    $minor = [int]$matches[2]
    $patch = [int]$matches[3]

    # Check if the sum of the components is greater than zero
    if (($major + $minor + $patch) -le 0) {
        Write-Host "The sum of version components (major, minor, patch) must be greater than 0 (e.g., not 0.0.0)." -ForegroundColor "Red"
        Exit 1
    }

    # If the version matches the branch pattern, validate the build number
    if ($Version -match $semverBranchPattern) {
        $buildNumber = [int]$matches[5]  # Extract the build number (suffix.n -> n)

        # Check if the build number is zero-padded
        if ($matches[5] -match '^0\d+') {
            Write-Host "Build number must not be zero-padded." -ForegroundColor "Red"
            Exit 1
        }

        # Check if the build number is greater than zero
        if ($buildNumber -le 0) {
            Write-Host "Build number must be greater than zero." -ForegroundColor "Red"
            Exit 1
        }
    }

    # Success message if all checks pass
    Write-Host "Version is valid: $Version" -ForegroundColor "Green"
} else {
    Write-Host "Invalid version format. Ensure it matches the expected patterns and components are not zero-padded." -ForegroundColor "Red"
    Exit 1
}



# Perform validation based on the branch
if ($Branch -eq 'refs/heads/main') {
    if (-not ($Version -match $semverMainPattern)) {
        Write-Host "Invalid version format for main branch. Expected format: x.y.z" -ForegroundColor "Red"
        Exit 1
    }
} elseif ($Branch -eq 'refs/heads/develop') {
    if (-not ($Version -match $semverBranchPattern -and $Version -like '*-alpha.*')) {
        Write-Host "Invalid version format for develop branch. Expected format: x.y.z-alpha.n" -ForegroundColor "Red"
        Exit 1
    }
} elseif ($Branch -eq 'refs/heads/qa') {
    if (-not ($Version -match $semverBranchPattern -and $Version -like '*-beta.*')) {
        Write-Host "Invalid version format for QA branch. Expected format: x.y.z-beta.n" -ForegroundColor "Red"
        Exit 1
    }
} else {
    Write-Host "Unknown branch. Cannot validate version." -ForegroundColor "Red"
    Exit 1
}

# If all checks pass
Write-Host "The version : $($Version) on your branch: $($Branch) is OK." -ForegroundColor "Green"
Exit 0
