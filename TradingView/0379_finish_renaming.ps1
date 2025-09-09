# PowerShell script to finish renaming the remaining files
$workDir = "C:\StockSharp\AlgoTrading\TradingView"
Set-Location $workDir

Write-Host "Finishing the renaming process..."

# Get all .txt files without 4-digit prefix, sorted alphabetically
$filesToRename = Get-ChildItem -File -Filter "*.txt" | 
    Where-Object { $_.Name -notmatch "^[0-9]{4}_" } | 
    Sort-Object Name

Write-Host "Found $($filesToRename.Count) files to rename starting from 2888"

$counter = 2888
$success = 0
$errors = 0

foreach ($file in $filesToRename) {
    $oldName = $file.Name
    $newName = "{0:D4}_{1}" -f $counter, $oldName
    
    Write-Host "[$counter] $oldName"
    
    try {
        Rename-Item -Path $file.FullName -NewName $newName -ErrorAction Stop
        $success++
        $counter++
    }
    catch {
        Write-Host "ERROR: $($_.Exception.Message)" -ForegroundColor Red
        $errors++
        $counter++  # Still increment counter to maintain sequence
    }
}

Write-Host "`nFinal renaming completed!"
Write-Host "Successful renames: $success"
Write-Host "Errors: $errors"
Write-Host "Final counter: $counter"

# Final count check
$withPrefix = (Get-ChildItem -File -Filter "*.txt" | Where-Object { $_.Name -match "^[0-9]{4}_" }).Count
$withoutPrefix = (Get-ChildItem -File -Filter "*.txt" | Where-Object { $_.Name -notmatch "^[0-9]{4}_" }).Count
$totalTxt = (Get-ChildItem -File -Filter "*.txt").Count

Write-Host "`nFinal counts:"
Write-Host "Files with 4-digit prefix: $withPrefix"
Write-Host "Files without prefix: $withoutPrefix"
Write-Host "Total .txt files: $totalTxt"

if ($withoutPrefix -eq 0) {
    Write-Host "SUCCESS: All .txt files have been renamed with 4-digit prefixes!" -ForegroundColor Green
} else {
    Write-Host "WARNING: $withoutPrefix .txt files still need renaming" -ForegroundColor Yellow
}