Param(
    [Parameter(Mandatory=$true)]
    [String]
    $ProjectName
)
if ($ProjectName -eq "") {
    throw 'The Project Name cannot be empty!!!'
}

#the required name to the new project
$newProjectFullName = $ProjectName

#the templateName
$templateName = "SML.ExampleGrpc"

# replace all references of "ExampleGrpc" in all files
# there are some permission issues in "Get-Content", ignore the errors as the files with errors are renamed aftwards
Get-ChildItem -recurse pipelines,sources,scripts,docker | 
	Select-Object -expand fullname |
		ForEach-Object {
			$file = $_
            $fileContent = (Get-Content $file -erroraction 'silentlycontinue')
			if(-Not [string]::IsNullOrEmpty($fileContent)){
				$fileContent = $fileContent.Replace($templateName.ToLower(),$newProjectFullName.ToLower(),$false,"en-US").Replace($templateName.ToLower().Replace(".","-"),$newProjectFullName.ToLower().Replace(".","-"),$false,"en-US" ).Replace($templateName,$newProjectFullName,$false,"en-US")
			}
			$fileContent | Set-Content $file 
		}
			
Push-Location "sources"

# rename all files in all child folders
Get-ChildItem -File -Recurse | % { Rename-Item -Path $_.PSPath -NewName $_.Name.replace($templateName,$newProjectFullName)}

# rename all files in current directory
Get-ChildItem | rename-item -NewName {$_.name -replace $templateName,$newProjectFullName}

Pop-Location

# remove README file, we don't want the template readme in our project
Remove-Item README.md

# remove rename scripts has we wont need it anymore
Remove-Item RenameTemplateFiles.ps1