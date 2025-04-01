# Script to optimize the model
# Usage example ./optimize.ps1
#Requires -PSEdition Core
# Call get-paths.ps1 to get folder paths
$paths = ./get-paths.ps1;
$parentFolder = Split-Path -Path $paths.StartupProject -Parent
Invoke-Expression "dotnet ef dbcontext optimize --msbuildprojectextensionspath '$parentFolder\obj' --project $($paths.DataProject) --startup-project $($paths.StartupProject)"