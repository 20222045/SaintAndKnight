[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)][string]$OutputDirectory,
    [string]$SourceDirectory = (Join-Path $PSScriptRoot 'source'),
    [switch]$Overwrite
)
$ErrorActionPreference = 'Stop'
$projectRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../../..'))
$outputRoot = [System.IO.Path]::GetFullPath($OutputDirectory)
$assetsRoot = Join-Path $projectRoot 'Assets'
if ($outputRoot.Equals($assetsRoot,[System.StringComparison]::OrdinalIgnoreCase) -or
    $outputRoot.StartsWith($assetsRoot+[System.IO.Path]::DirectorySeparatorChar,[System.StringComparison]::OrdinalIgnoreCase)) {
    throw 'Stage outside Assets; applying this art requires a separate reviewed copy.'
}
if ((Test-Path -LiteralPath $outputRoot) -and (Get-ChildItem -LiteralPath $outputRoot -File).Count -and -not $Overwrite) {
    throw 'Output folder contains files. Choose a new staging folder or pass -Overwrite.'
}
New-Item -ItemType Directory -Path $outputRoot -Force | Out-Null
Add-Type -AssemblyName System.Drawing
if (-not ('SaintKnightDedicatedTrapFrames' -as [type])) {
    $refs = @((Get-ChildItem -LiteralPath (Join-Path $PSHOME 'ref') -Filter '*.dll').FullName)
    foreach ($runtimeRef in @([System.Drawing.Bitmap].Assembly.Location,[System.Drawing.Rectangle].Assembly.Location,
        (Join-Path $PSHOME 'System.Private.Windows.GdiPlus.dll'),(Join-Path $PSHOME 'System.Private.Windows.Core.dll'))) {
        if (Test-Path -LiteralPath $runtimeRef) {
            $refs = @($refs | Where-Object { [System.IO.Path]::GetFileName($_) -ne [System.IO.Path]::GetFileName($runtimeRef) })
            $refs += $runtimeRef
        }
    }
    Add-Type -Path (Join-Path $PSScriptRoot 'TrapMotionFrameBuilder.cs') -ReferencedAssemblies $refs
}
$result = [SaintKnightDedicatedTrapFrames]::Build($projectRoot,(Resolve-Path -LiteralPath $SourceDirectory).Path,$outputRoot)
$sourceFiles = @(
    'Assets/SaintKnightDay06/Resources/SaintKnightDay06/cecilia_motion.png',
    'Assets/SaintKnightDay06/Resources/SaintKnightDay06/adela_motion.png',
    'Assets/SaintKnightDay10/Art/flesh_wall_idle_f00.png'
)
$report = [ordered]@{
    operation = 'Native character pixels copied at 1:1 size with integer translation; only trap materials resampled'
    asset_application = 'staged_only'
    source_characters = @($sourceFiles | ForEach-Object { [ordered]@{path=$_; sha256=(Get-FileHash -LiteralPath (Join-Path $projectRoot $_) -Algorithm SHA256).Hash} })
    source_materials = @('slime','tentacle','wall' | ForEach-Object {
        $path=Join-Path $SourceDirectory ($_+'_material.png')
        [ordered]@{path=$path; sha256=(Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash}
    })
    sheets = $result
}
$report | ConvertTo-Json -Depth 14 | Set-Content -LiteralPath (Join-Path $outputRoot 'build-report.json') -Encoding utf8
$result | Select-Object Character,Trap,FrameCount,UniqueFrames,MinimumVisibleBodyPixels,MaximumVisibleBodyPixels
Write-Output "Staged output: $outputRoot"

