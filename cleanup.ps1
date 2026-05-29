$inner = "c:\Users\USER\Desktop\IT_Gied\IT_Gied"
$outer = "c:\Users\USER\Desktop\IT_Gied"

Write-Host "Copying unique files from inner to outer..."
Get-ChildItem -Path $inner -Recurse -File | ForEach-Object {
    $rel = $_.FullName.Substring($inner.Length + 1)
    $outPath = Join-Path $outer $rel
    if (-not (Test-Path $outPath)) {
        $dir = Split-Path $outPath
        if (-not (Test-Path $dir)) { New-Item -ItemType Directory -Force -Path $dir | Out-Null }
        Copy-Item -Path $_.FullName -Destination $outPath
        Write-Host "Preserved unique file: $rel"
    }
}

Write-Host "Emptying inner folder to eliminate duplicates..."
Get-ChildItem -Path $inner -Recurse -File | Remove-Item -Force
Get-ChildItem -Path $inner -Mindepth 1 | Where-Object { $_.PSIsContainer } | Remove-Item -Recurse -Force

Write-Host "Cleanup complete."
