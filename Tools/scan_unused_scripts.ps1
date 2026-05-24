$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$scriptDir = Join-Path $root 'Assets\Scripts'
$assetRoot = Join-Path $root 'Assets'

$csFiles = Get-ChildItem -Path $scriptDir -Recurse -Filter *.cs | Select-Object -ExpandProperty FullName
$assetFiles = @()
$assetFiles += Get-ChildItem -Path $assetRoot -Recurse -Include *.unity,*.prefab | Select-Object -ExpandProperty FullName

$unused = New-Object System.Collections.Generic.List[string]

foreach ($cs in $csFiles)
{
    $meta = $cs + '.meta'
    if (!(Test-Path -LiteralPath $meta)) { continue }

    $guidLine = Select-String -LiteralPath $meta -Pattern '^guid:\s*([0-9a-f]{32})\s*$' | Select-Object -First 1
    if ($null -eq $guidLine) { continue }
    $guid = $guidLine.Matches[0].Groups[1].Value

    $hit = Select-String -Path $assetFiles -SimpleMatch $guid -List -ErrorAction SilentlyContinue
    if ($null -eq $hit)
    {
        $unused.Add($cs) | Out-Null
    }
}

Write-Output ('UNUSED_COUNT ' + $unused.Count)
$unused | Sort-Object | ForEach-Object { Write-Output $_ }
