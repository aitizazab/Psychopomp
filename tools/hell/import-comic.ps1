# Imports the comic flip-through's pictures (docs/HELL-COMIC.md) into Resources/HellComic as 1920x1080 JPGs.
#   powershell -File tools/hell/import-comic.ps1                 # the endings (hell-end-*), the default
#   powershell -File tools/hell/import-comic.ps1 -Opening        # also the opening's six premise panels
#   powershell -File tools/hell/import-comic.ps1 -Inset 0.03     # trims 3% off every edge first (notebook/paper edges)
#   powershell -File tools/hell/import-comic.ps1 -DryRun         # only lists what it would copy
# Endings: <Incoming>\hell-end-<art>-<page>-<panel>[-takeN].jpg and hell-end-<art>-title[-takeN].jpg, where <art> is kill,
# spare-kept, spare-cost, damned or lucifer; page 1 has panels 1 and 2, page 2 panel 1 (see HellComics.Ending). With several
# takes the highest take wins (a file without -takeN beats them all). Output: Resources/HellComic/endings/<art>-<page>-<panel>.jpg.
# Unity makes the .meta files on its next import (never hand-write them). Needs ffmpeg on PATH. Safe to re-run.
param(
    [string]$Incoming = (Join-Path $PSScriptRoot '..\..\..\FableAnimationTests\art\incoming'),
    [switch]$Opening,
    [double]$Inset = 0,
    [switch]$DryRun
)
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$res = Join-Path $root 'Assets\JamKit\Templates\SkylineRepo\Resources\HellComic'
if (-not (Test-Path $Incoming)) { Write-Host "No incoming folder: $Incoming"; exit 1 }
$Incoming = (Resolve-Path $Incoming).Path

function Convert-Panel([string]$src, [string]$dst, [int[]]$crop) {
    $dir = Split-Path $dst -Parent
    if (-not (Test-Path $dir)) { New-Item -ItemType Directory -Force $dir | Out-Null }
    if ($crop) { $vf = "crop=$($crop[2]):$($crop[3]):$($crop[0]):$($crop[1]),scale=1920:1080:flags=lanczos" }
    else {
        $k = 1 - 2 * $Inset
        $vf = "crop=iw*${k}:ih*${k},crop='min(iw,ih*16/9)':'min(ih,iw*9/16)',scale=1920:1080:flags=lanczos"
    }
    Write-Host ("  " + (Split-Path $src -Leaf) + "  ->  " + $dst.Substring($root.Length + 1))
    if ($DryRun) { return }
    $tmp = "$dst.tmp.jpg"
    & ffmpeg -hide_banner -loglevel error -y -i $src -vf $vf -q:v 3 $tmp
    if ($LASTEXITCODE -ne 0) { if (Test-Path $tmp) { Remove-Item $tmp -Force }; throw "ffmpeg failed on $src" }
    Move-Item -Force $tmp $dst
}

if ($Opening) {
    Write-Host 'The opening:'
    # the premise art the creator picked, cropped as the trailer crops it (notebook edges off; video/psychopomp-trailer/edl)
    $open = @(
        @('hell-sister-hand-take2.jpg', '1-1', @(90, 55, 2500, 1406)),
        @('hell-sister-doorway-take1.jpg', '1-2', @(136, 60, 2480, 1395)),
        @('hell-r2-baja-take2.jpg', '2-1', @(40, 22, 2672, 1503)),
        @('hell-r2-deal-take1.jpg', '3-1', @(197, 210, 2357, 1326)),
        @('hell-gate-descent-take1.jpg', '4-1', @(26, 14, 2700, 1519)),
        @('hell-gate-fall-take2.jpg', '4-2', @(192, 300, 2197, 1236))
    )
    foreach ($o in $open) {
        $src = Join-Path $Incoming $o[0]
        if (-not (Test-Path $src)) { Write-Host "  missing $($o[0])"; continue }
        Convert-Panel $src (Join-Path $res "opening\$($o[1]).jpg") $o[2]
    }
}

Write-Host 'The endings:'
$pattern = '^hell-end-(kill|spare-kept|spare-cost|damned|lucifer)-(\d-\d|title)(?:-take(\d+))?\.(jpg|jpeg|png)$'
$best = @{}
foreach ($f in Get-ChildItem $Incoming -File) {
    $m = [regex]::Match($f.Name.ToLowerInvariant(), $pattern)
    if (-not $m.Success) { continue }
    $key = "$($m.Groups[1].Value)-$($m.Groups[2].Value)"
    $take = if ($m.Groups[3].Success) { [int]$m.Groups[3].Value } else { [int]::MaxValue }
    if (-not $best.ContainsKey($key) -or $best[$key].take -lt $take) { $best[$key] = @{ take = $take; path = $f.FullName } }
}
if ($best.Count -eq 0) { Write-Host '  none yet (hell-end-<art>-<page>-<panel>[-takeN].jpg)' }
foreach ($key in ($best.Keys | Sort-Object)) { Convert-Panel $best[$key].path (Join-Path $res "endings\$key.jpg") $null }
$skipped = Get-ChildItem $Incoming -File -Filter 'hell-end-*' | Where-Object { -not [regex]::IsMatch($_.Name.ToLowerInvariant(), $pattern) }
foreach ($s in $skipped) { Write-Host "  not matched (rename it): $($s.Name)" }
Write-Host 'Done. Unity imports the pictures (and makes their metas) when it next opens or refreshes.'
