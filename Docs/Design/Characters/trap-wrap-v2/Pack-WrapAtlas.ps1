[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$OutputDirectory,
    [string]$SourceSheet = (Join-Path $PSScriptRoot 'full_body_wrap_concept.png'),
    [string]$TentacleSheet = '',
    [string]$AtlasDirectory = '',
    [switch]$Overwrite
)

$ErrorActionPreference = 'Stop'
$projectRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../../..'))
$assetsRoot = [System.IO.Path]::GetFullPath((Join-Path $projectRoot 'Assets'))
$outputRoot = [System.IO.Path]::GetFullPath($OutputDirectory)
if ($outputRoot.Equals($assetsRoot, [System.StringComparison]::OrdinalIgnoreCase) -or
    $outputRoot.StartsWith($assetsRoot + [System.IO.Path]::DirectorySeparatorChar, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw 'OutputDirectory must be a staging directory outside Assets. This packer does not apply assets.'
}
if (-not $AtlasDirectory) {
    $AtlasDirectory = Join-Path $assetsRoot 'SaintKnightDay06/Resources/SaintKnightDay06'
}
$atlasRoot = [System.IO.Path]::GetFullPath($AtlasDirectory)
$sourcePath = (Resolve-Path -LiteralPath $SourceSheet).Path
$tentaclePath = if ($TentacleSheet) { (Resolve-Path -LiteralPath $TentacleSheet).Path } else { '' }
$ceciliaAtlas = (Resolve-Path -LiteralPath (Join-Path $atlasRoot 'cecilia_motion.png')).Path
$adelaAtlas = (Resolve-Path -LiteralPath (Join-Path $atlasRoot 'adela_motion.png')).Path
$outputs = @('cecilia_motion.png', 'adela_motion.png', 'trap_reactions_preview.png', 'packing-report.json')
foreach ($name in $outputs) {
    $target = Join-Path $outputRoot $name
    foreach ($inputPath in @($sourcePath, $tentaclePath, $ceciliaAtlas, $adelaAtlas)) {
        if ($inputPath -and $target.Equals($inputPath, [System.StringComparison]::OrdinalIgnoreCase)) {
            throw 'Output path must not replace an input file.'
        }
    }
    if ((Test-Path -LiteralPath $target) -and -not $Overwrite) {
        throw "Output already exists: $target. Use a new staging folder or explicitly pass -Overwrite."
    }
}
New-Item -ItemType Directory -Path $outputRoot -Force | Out-Null
Add-Type -AssemblyName System.Drawing
if (-not ('SaintKnightTrapWrapPackerV2' -as [type])) {
    $referencePaths = @((Get-ChildItem -LiteralPath (Join-Path $PSHOME 'ref') -Filter '*.dll').FullName)
    $drawingPaths = @(
        [System.Drawing.Bitmap].Assembly.Location,
        [System.Drawing.Rectangle].Assembly.Location,
        (Join-Path $PSHOME 'System.Private.Windows.GdiPlus.dll'),
        (Join-Path $PSHOME 'System.Private.Windows.Core.dll')
    )
    foreach ($drawingPath in $drawingPaths) {
        if (Test-Path -LiteralPath $drawingPath) {
            $referencePaths = @($referencePaths | Where-Object { [System.IO.Path]::GetFileName($_) -ne [System.IO.Path]::GetFileName($drawingPath) })
            $referencePaths += $drawingPath
        }
    }
    Add-Type -Path (Join-Path $PSScriptRoot 'WrapAtlasPacking.cs') -ReferencedAssemblies $referencePaths
}
$result = [SaintKnightTrapWrapPackerV2]::Pack($sourcePath, $tentaclePath, $ceciliaAtlas, $adelaAtlas, $outputRoot)
$report = [ordered]@{
    operation = 'Nearest-neighbor resampling and cell packing; no painted or generated pixels'
    source_sheet = $sourcePath
    source_sha256 = (Get-FileHash -LiteralPath $sourcePath -Algorithm SHA256).Hash
    tentacle_sheet = if ($tentaclePath) { $tentaclePath } else { $null }
    tentacle_sha256 = if ($tentaclePath) { (Get-FileHash -LiteralPath $tentaclePath -Algorithm SHA256).Hash } else { $null }
    tentacle_fallback = -not [bool]$tentaclePath
    normalization = 'One common scale per character per source sheet; pose proportions within a sheet stay unchanged'
    input_atlases = @($ceciliaAtlas, $adelaAtlas | ForEach-Object {
        [ordered]@{ path = $_; sha256 = (Get-FileHash -LiteralPath $_ -Algorithm SHA256).Hash }
    })
    alpha_threshold = 128
    ground_last_pixel_y = 93
    atlas_metadata_files_modified = $false
    characters = $result
    files = @($outputs | Where-Object { $_ -ne 'packing-report.json' } | ForEach-Object {
        $filePath = Join-Path $outputRoot $_
        [ordered]@{ name = $_; sha256 = (Get-FileHash -LiteralPath $filePath -Algorithm SHA256).Hash }
    })
}
$report | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath (Join-Path $outputRoot 'packing-report.json') -Encoding utf8
$result | Select-Object Character, SlimeScale, TentacleScale, PreservedPixelsVerified, ChangedCells, SemiTransparentReplacementPixels
Write-Output "Staged output: $outputRoot"

