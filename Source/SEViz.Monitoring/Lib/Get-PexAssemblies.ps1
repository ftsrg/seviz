<#
.SYNOPSIS
  The script copies the assemblies required by SEViz from the Visual Studio Enterprise folder.
.DESCRIPTION
  SEViz is a tool for visualizing symbolic execution and monitors the execution of Pex test generator tool.
.INPUTS
  None
.OUTPUTS
  Three dlls and one exe is copied to the calling folder of the script.
.NOTES
  Version:        0.1
  Author:         Dávid Honfi
  Creation Date:  2015.11.05.
  Purpose/Change: Alleviation of build
  
.EXAMPLE
  Call the script without any parameters.
#>

# Getting the value of the environment variable
$vsDir = [environment]::GetEnvironmentVariable("VS140COMNTOOLS").TrimEnd("\Tools")

# Checking if the VS14 installation exists
if($vsDir -ne $null) {

	# Clearing $Error variable
	$Error.Clear()

	# Copying the files
	Copy-Item "$vsDir\IDE\Extensions\Microsoft\Pex\Microsoft.ExtendedReflection.dll" ./ -errorAction SilentlyContinue
	Copy-Item "$vsDir\IDE\Extensions\Microsoft\Pex\Microsoft.ExtendedReflection.Reasoning.dll" ./ -errorAction SilentlyContinue
	Copy-Item "$vsDir\IDE\Extensions\Microsoft\Pex\Microsoft.Pex.exe" ./ -errorAction SilentlyContinue
	Copy-Item "$vsDir\IDE\Extensions\Microsoft\Pex\Microsoft.Pex.Framework.dll" ./ -errorAction SilentlyContinue

	# Checking for errors occured during the copy
	$isErroneous = $false
	foreach($err in $error)
    {
		if($err.Exception -ne $null) {
			$isErroneous = $true
			Write-Warning $err.Exception.Message
		}
	}

	# Writing out if error occured
	if($isErroneous -eq $true) {
		Write-Error "Problem occured during copying a file. (Do you have Enterprise version of VS2015?)"
	}
} else {
	Write-Error "VS140COMNTOOLS environment variable is not found. (Do you have an installation of VS2015?)"
}