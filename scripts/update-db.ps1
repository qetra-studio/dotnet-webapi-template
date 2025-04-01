# Script to update database to the migration
# Usage example ./update-db.ps1 "MigrationName"
# Usage example ./update-db.ps1 0
#Requires -PSEdition Core
param(
    [Parameter()]
    [string]$migration
)
# Call get-paths.ps1 to get folder paths
$paths = ./get-paths.ps1;
Invoke-Expression "dotnet ef database update $migration --project $($paths.DataProject) --startup-project $($paths.StartupProject)"