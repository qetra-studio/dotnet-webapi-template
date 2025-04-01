# Script to squash migrations
# Usage: ./squash-migrations.ps1
#Requires -PSEdition Core
####################################################################################################
# Script-wide constants ############################################################################
####################################################################################################
$solutionFileName       = "EHRv2.Backend.sln"
$dataProjectFileName    = "EHRv2.Database.Migrations.csproj"
$startupProjectFileName = "EHRv2.Backend.csproj"
$migrationsFolderName   = "Migrations"
$rootFolder             = $null
$migrationsFolder       = $null
####################################################################################################
# Finding root directory - $rootFolder #############################################################
####################################################################################################
$currentDir = Split-Path -Path $MyInvocation.MyCommand.Definition -Parent;
do {
	$solutionFile = Get-ChildItem -Path $currentDir -Recurse -File | Where-Object { $_.Name -eq $solutionFileName }
	if ($null -ne $solutionFile) {
		$rootFolder = Split-Path -Path $solutionFile.FullName -Parent;
		Write-Host "Found solution file in $rootFolder" -ForegroundColor Green;
		break;
	}
	$currentDir = Split-Path -Path $currentDir -Parent;
} while ($true);
####################################################################################################
# Looking for migrations folder - @migrationsFolder ################################################
####################################################################################################
$migrationsFolder = Get-ChildItem -Path $rootFolder -Recurse -Directory | Where-Object { $_.Name -eq $migrationsFolderName }
if ($null -ne $migrationsFolder) {
	Write-Host "Found $migrationsFolderName folder in $($migrationsFolder.FullName)" -ForegroundColor Green;
} else {
	Write-Host "Could not find $migrationsFolderName folder. Exiting ..." -ForegroundColor Red;
	exit;
}
Write-Host "Here is the list of migrations to be squashed:" -ForegroundColor Yellow;
$files = Get-ChildItem -Path $migrationsFolder.FullName -File;
$firstMigrationName = $files[0].Name.Split(".")[0];
if ($files.Count -lt 10) {
	Write-Host "Files in $migrationsFolderName folder:" -ForegroundColor Yellow;
	$files | ForEach-Object { Write-Host $_.Name -ForegroundColor Yellow; };
} else {
	Write-Host "Files in $migrationsFolderName folder:" -ForegroundColor Yellow;
	$files[0].Name;
	$files[1].Name;
	$files[2].Name;
	Write-Host "..." -ForegroundColor Yellow;
	$files[-3].Name;
	$files[-2].Name;
	$files[-1].Name;
}
####################################################################################################
# Delete migration files ##########################################################################
####################################################################################################
try { Remove-Item -Path $migrationsFolder -Recurse -Force; } 
catch { Write-Host "Error: $($_.Exception.Message)"; }
Write-Host "Deleted $migrationsFolderName folder" -ForegroundColor Green;
####################################################################################################
# Generate new initial migration and rename it to the first migration ##############################
####################################################################################################
$dataProjectPath = Get-ChildItem -Path $rootFolder -Recurse -File | Where-Object { $_.Name -eq $dataProjectFileName  }
$startupProjectPath = Get-ChildItem -Path $rootFolder -Recurse -File | Where-Object { $_.Name -eq $startupProjectFileName  }
$newMigrationName = "Initial";
Invoke-Expression "dotnet ef migrations add $newMigrationName --project $dataProjectPath --startup-project $startupProjectPath";
Write-Host "Migration '$newMigrationName' created" -ForegroundColor Green;
$generatedMigrationFiles = Get-ChildItem -Path $migrationsFolder -Filter "*$newMigrationName*.cs";
foreach ($file in $generatedMigrationFiles) {
	$fileNamePartToChange = $file.Name.Split(".")[0];
	$oldClassName = $fileNamePartToChange.Split("_")[1];
	$newFileName = $file.Name.Replace($fileNamePartToChange, $firstMigrationName);
	Rename-Item -Path $file.FullName -NewName $newFileName;
	$fileContent = Get-Content -Path "$migrationsFolder\$newFileName" -Raw;
	if ($newFileName.Contains("Designer")) {
		$fileContent = $fileContent.Replace($fileNamePartToChange, $firstMigrationName);
	}
	$fileContent = $fileContent.Replace($oldClassName, $firstMigrationName.Split("_")[1]);
	Set-Content -Path "$migrationsFolder\$newFileName" -Value $fileContent;
	Write-Host "Renamed $file to $newFileName" -ForegroundColor Green;
}
####################################################################################################
# Generate squashed migration and clean up migrations history ######################################
####################################################################################################
$squashedMigrationName = "Squashed";
Invoke-Expression "dotnet ef migrations add $squashedMigrationName --project $dataProjectPath --startup-project $startupProjectPath";
Write-Host "Migration '$squashedMigrationName' created" -ForegroundColor Green;
$generatedMigrationFile = Get-ChildItem -Path $migrationsFolder -Filter "*$squashedMigrationName.cs";
$fileContent = Get-Content -Path $generatedMigrationFile;
$methodIndex = $fileContent | Select-String -Pattern 'protected override void Up' | Select-Object LineNumber;
if ($methodIndex) {
	$implementation = "migrationBuilder.Sql(`"DELETE FROM [__EFMigrationsHistory] WHERE MigrationId <> '$firstMigrationName'`");";
	$fileContent[$methodIndex.LineNumber + 1] = "`t`t`t$implementation";
	Set-Content -Path $generatedMigrationFile -Value $fileContent;
} else {
	Write-Error "Method 'Up' not found in the file.";
}
####################################################################################################
# Finishing up #####################################################################################
####################################################################################################
Write-Host "All done! Now start the solution to apply migrations." -ForegroundColor Green;