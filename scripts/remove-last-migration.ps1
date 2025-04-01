# Script to remove last migration
# Usage example ./remove-last-migration.ps1
#Requires -PSEdition Core
# Call get-paths.ps1 to get folder paths
$paths = ./get-paths.ps1;
Invoke-Expression "dotnet ef migrations remove --project $($paths.DataProject) --startup-project $($paths.StartupProject)"