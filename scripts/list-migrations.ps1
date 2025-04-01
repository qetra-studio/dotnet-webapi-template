# script to list existing migrations
# Usage: ./list-migrations.ps1
#Requires -PSEdition Core
# Call get-paths.ps1 to get folder paths
$paths = ./get-paths.ps1;
# Start a job for Invoke-Expression
$job = Start-Job -ScriptBlock {
    Invoke-Expression "dotnet ef migrations list --project $($args[0].DataProject) --startup-project $($args[0].StartupProject)"
} -ArgumentList $paths
# Display a progress bar while the job is running
while ($job.State -eq 'Running') {
    Write-Progress -Activity "Listing migrations" -Status "Please wait..." -PercentComplete -1
    Start-Sleep -Seconds 1
}
# Remove the progress bar
Write-Progress -Activity "Listing migrations" -Completed
# Print each migration on a new line
$cleaned = Receive-Job -Job $job | Select-String -Pattern '^[0-9A-Fa-f]{8}'
foreach ($migration in $cleaned) { Write-Host $migration }