# Script to add a new migration
# Usage example ./add-migration.ps1 "NewStuff"
#Requires -PSEdition Core
param(
    [Parameter(Mandatory=$true)]
    [string]$migrationName
)
# Call get-paths.ps1 to get folder paths
$paths = ./get-paths.ps1;
Invoke-Expression "dotnet ef migrations add $migrationName --project $($paths.DataProject) --startup-project $($paths.StartupProject)"