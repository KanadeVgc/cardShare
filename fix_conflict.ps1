$lines = Get-Content "c:\Users\owner\Documents\GitHub\cardShare\Assets\TextMesh Pro\Resources\Fonts & Materials\LiberationSans SDF - Fallback.asset"
$newLines = @()
$skip = $false
foreach ($line in $lines) {
    if ($line -match "^<<<<<<< Updated upstream") {
        $skip = $true
    } elseif ($line -match "^=======") {
        $skip = $false
    } elseif ($line -match "^>>>>>>> Stashed changes") {
    } else {
        if (-not $skip) {
            $newLines += $line
        }
    }
}
[IO.File]::WriteAllLines("c:\Users\owner\Documents\GitHub\cardShare\Assets\TextMesh Pro\Resources\Fonts & Materials\LiberationSans SDF - Fallback.asset", $newLines)
