# Script to generate a script from migrations
# Usage example ./migrations-script.ps1
#Requires -PSEdition Core
# Call get-paths.ps1 to get folder paths
$paths = ./get-paths.ps1;
Invoke-Expression "dotnet ef migrations script $migrationName --project $($paths.DataProject) --startup-project $($paths.StartupProject)"