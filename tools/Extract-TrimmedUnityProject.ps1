param(
    [string]$SourceRoot = (Join-Path $PSScriptRoot "..\\game"),
    [string]$OutputRoot = (Join-Path $PSScriptRoot "..\\trimmed_projects\\stage01_build_export")
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Get-NormalizedPath {
    param([Parameter(Mandatory = $true)][string]$Path)

    return [System.IO.Path]::GetFullPath($Path)
}

function Test-StartsWithPath {
    param(
        [Parameter(Mandatory = $true)][string]$Candidate,
        [Parameter(Mandatory = $true)][string]$Prefix
    )

    return $Candidate.StartsWith($Prefix, [System.StringComparison]::OrdinalIgnoreCase)
}

function Copy-ItemWithMeta {
    param(
        [Parameter(Mandatory = $true)][string]$SourcePath,
        [Parameter(Mandatory = $true)][string]$DestinationPath
    )

    $destinationDirectory = Split-Path -Path $DestinationPath -Parent
    if (-not (Test-Path -LiteralPath $destinationDirectory)) {
        New-Item -ItemType Directory -Path $destinationDirectory -Force | Out-Null
    }

    Copy-Item -LiteralPath $SourcePath -Destination $DestinationPath -Force

    $metaPath = "$SourcePath.meta"
    if (Test-Path -LiteralPath $metaPath) {
        Copy-Item -LiteralPath $metaPath -Destination "$DestinationPath.meta" -Force
    }
}

function Get-GuidMatches {
    param([Parameter(Mandatory = $true)][string]$FilePath)

    $extension = [System.IO.Path]::GetExtension($FilePath)
    if ($script:TextAssetExtensions -notcontains $extension) {
        return @()
    }

    $content = Get-Content -LiteralPath $FilePath -Raw
    if ([string]::IsNullOrWhiteSpace($content)) {
        return @()
    }

    return [System.Text.RegularExpressions.Regex]::Matches($content, "guid:\s*([0-9a-f]{32})") |
        ForEach-Object { $_.Groups[1].Value } |
        Sort-Object -Unique
}

function Add-AssetToCopySet {
    param([Parameter(Mandatory = $true)][string]$RelativePath)

    if ([string]::IsNullOrWhiteSpace($RelativePath)) {
        return
    }

    if ($script:CopiedAssetPaths.Contains($RelativePath)) {
        return
    }

    $sourcePath = Join-Path $script:SourceRoot $RelativePath
    if (-not (Test-Path -LiteralPath $sourcePath)) {
        return
    }

    $destinationPath = Join-Path $script:OutputRoot $RelativePath
    Copy-ItemWithMeta -SourcePath $sourcePath -DestinationPath $destinationPath

    $null = $script:CopiedAssetPaths.Add($RelativePath)

    if ($script:TextAssetExtensions -contains [System.IO.Path]::GetExtension($RelativePath)) {
        $script:ParseQueue.Enqueue($RelativePath)
    }
}

function Add-DirectorySeed {
    param([Parameter(Mandatory = $true)][string]$RelativeDirectory)

    $absoluteDirectory = Join-Path $script:SourceRoot $RelativeDirectory
    if (-not (Test-Path -LiteralPath $absoluteDirectory)) {
        return
    }

    Get-ChildItem -LiteralPath $absoluteDirectory -Recurse -File | ForEach-Object {
        $relativePath = $_.FullName.Substring($script:SourceRoot.Length + 1)
        Add-AssetToCopySet -RelativePath $relativePath
    }
}

$repoRoot = Get-NormalizedPath -Path (Join-Path $PSScriptRoot "..")
$script:SourceRoot = Get-NormalizedPath -Path $SourceRoot
$script:OutputRoot = Get-NormalizedPath -Path $OutputRoot
$allowedOutputRoot = Get-NormalizedPath -Path (Join-Path $repoRoot "trimmed_projects")

if (-not (Test-Path -LiteralPath (Join-Path $script:SourceRoot "Assets"))) {
    throw "SourceRoot does not look like a Unity project: $script:SourceRoot"
}

if (-not (Test-StartsWithPath -Candidate $script:OutputRoot -Prefix $allowedOutputRoot)) {
    throw "OutputRoot must stay under $allowedOutputRoot"
}

if (Test-Path -LiteralPath $script:OutputRoot) {
    try {
        Remove-Item -LiteralPath $script:OutputRoot -Recurse -Force
    }
    catch {
        $fallbackOutputRoot = "{0}_{1}" -f $script:OutputRoot, (Get-Date -Format "yyyyMMdd_HHmmss")
        Write-Warning "Could not replace existing output folder, likely because Unity is holding it open. Writing to $fallbackOutputRoot instead."
        $script:OutputRoot = $fallbackOutputRoot
    }
}

New-Item -ItemType Directory -Path $script:OutputRoot -Force | Out-Null

Copy-Item -LiteralPath (Join-Path $script:SourceRoot "Packages") -Destination (Join-Path $script:OutputRoot "Packages") -Recurse -Force
Copy-Item -LiteralPath (Join-Path $script:SourceRoot "ProjectSettings") -Destination (Join-Path $script:OutputRoot "ProjectSettings") -Recurse -Force

$graphicsSettingsPath = Join-Path $script:OutputRoot "ProjectSettings\\GraphicsSettings.asset"
if (Test-Path -LiteralPath $graphicsSettingsPath) {
    $graphicsSettingsContent = Get-Content -LiteralPath $graphicsSettingsPath -Raw
    $graphicsSettingsContent = $graphicsSettingsContent -replace "(?ms)^  m_RenderPipelineGlobalSettingsMap:\r?\n(?:    .*\r?\n)+", "  m_RenderPipelineGlobalSettingsMap: {}`r`n"
    Set-Content -LiteralPath $graphicsSettingsPath -Value $graphicsSettingsContent
}

$script:TextAssetExtensions = @(
    ".anim",
    ".asmdef",
    ".asmref",
    ".asset",
    ".brush",
    ".controller",
    ".flare",
    ".fontsettings",
    ".guiskin",
    ".lighting",
    ".mask",
    ".mat",
    ".overrideController",
    ".physicsMaterial",
    ".physicsMaterial2D",
    ".playable",
    ".preset",
    ".prefab",
    ".renderTexture",
    ".shadergraph",
    ".shadersubgraph",
    ".spriteatlas",
    ".spriteatlasv2",
    ".timeline",
    ".unity",
    ".vfx"
)

$script:CopiedAssetPaths = New-Object System.Collections.Generic.HashSet[string]([System.StringComparer]::OrdinalIgnoreCase)
$script:ParseQueue = New-Object System.Collections.Generic.Queue[string]

$guidToPath = @{}
Get-ChildItem -LiteralPath (Join-Path $script:SourceRoot "Assets") -Recurse -File -Filter *.meta | ForEach-Object {
    $guidLine = Select-String -LiteralPath $_.FullName -Pattern "^guid:\s*([0-9a-f]{32})$" -CaseSensitive | Select-Object -First 1
    if ($null -eq $guidLine) {
        return
    }

    $guid = $guidLine.Matches[0].Groups[1].Value
    $assetPath = $_.FullName.Substring($script:SourceRoot.Length + 1)
    if ($assetPath.EndsWith(".meta", [System.StringComparison]::OrdinalIgnoreCase)) {
        $assetPath = $assetPath.Substring(0, $assetPath.Length - 5)
    }

    if (-not $guidToPath.ContainsKey($guid)) {
        $guidToPath[$guid] = $assetPath
    }
}

$seedDirectories = @(
    "Assets\\Scripts",
    "Assets\\Scenes",
    "Assets\\Data",
    "Assets\\Prefabs",
    "Assets\\Resources\\Battle",
    "Assets\\Resources\\Stage01Demo",
    "Assets\\Resources\\UI\\BattleHud",
    "Assets\\Plugins\\Demigiant\\DemiLib",
    "Assets\\Plugins\\Demigiant\\DOTween",
    "Assets\\HeroEditor4D\\Common\\Scripts\\Data",
    "Assets\\HeroEditor4D\\Common\\Scripts\\Enums",
    "Assets\\HeroEditor4D\\Common\\SimpleColorPicker",
    "Assets\\HeroEditor4D\\InventorySystem",
    "Assets\\FantasyMonsters\\Common\\Scripts"
)

foreach ($seedDirectory in $seedDirectories) {
    Add-DirectorySeed -RelativeDirectory $seedDirectory
}

$seedFiles = @(
    "Assets\\Resources\\DOTweenSettings.asset"
)

foreach ($seedFile in $seedFiles) {
    Add-AssetToCopySet -RelativePath $seedFile
}

Get-ChildItem -LiteralPath (Join-Path $script:SourceRoot "ProjectSettings") -Recurse -File |
    Where-Object { $script:TextAssetExtensions -contains [System.IO.Path]::GetExtension($_.FullName) } |
    ForEach-Object {
        foreach ($guid in Get-GuidMatches -FilePath $_.FullName) {
            if ($guidToPath.ContainsKey($guid)) {
                Add-AssetToCopySet -RelativePath $guidToPath[$guid]
            }
        }
    }

while ($script:ParseQueue.Count -gt 0) {
    $relativePath = $script:ParseQueue.Dequeue()
    $absolutePath = Join-Path $script:SourceRoot $relativePath
    if (-not (Test-Path -LiteralPath $absolutePath)) {
        continue
    }

    foreach ($guid in Get-GuidMatches -FilePath $absolutePath) {
        if (-not $guidToPath.ContainsKey($guid)) {
            continue
        }

        Add-AssetToCopySet -RelativePath $guidToPath[$guid]
    }
}

$excludedAssetPaths = @(
    "Assets\\DefaultVolumeProfile.asset",
    "Assets\\UniversalRenderPipelineGlobalSettings.asset"
)

foreach ($excludedAssetPath in $excludedAssetPaths) {
    $outputAssetPath = Join-Path $script:OutputRoot $excludedAssetPath
    if (Test-Path -LiteralPath $outputAssetPath) {
        Remove-Item -LiteralPath $outputAssetPath -Force
    }

    $outputMetaPath = "$outputAssetPath.meta"
    if (Test-Path -LiteralPath $outputMetaPath) {
        Remove-Item -LiteralPath $outputMetaPath -Force
    }
}

$summaryPath = Join-Path $script:OutputRoot "TRIMMED_PROJECT_SUMMARY.txt"
$summary = @(
    "Generated: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')",
    "Source: $script:SourceRoot",
    "Output: $script:OutputRoot",
    "Copied asset files: $($script:CopiedAssetPaths.Count)",
    "",
    "Seed directories:",
    ($seedDirectories | ForEach-Object { "- $_" }),
    "",
    "Seed files:",
    ($seedFiles | ForEach-Object { "- $_" })
)
Set-Content -LiteralPath $summaryPath -Value $summary

$requiredPaths = @(
    "Assets\\Scenes\\MainMenu.unity",
    "Assets\\Scenes\\Battle.unity",
    "Assets\\Resources\\Stage01Demo\\Stage01DemoBattleInput.asset",
    "Assets\\Resources\\Stage01Demo\\Stage01HeroCatalog.asset",
    "Assets\\Resources\\UI\\BattleHud\\top_scoreboard_runtime_base.png",
    "Assets\\Resources\\Battle\\jjc_background.png",
    "Assets\\Scripts\\Editor\\WindowsBuildExporter.cs"
)

$missingPaths = @()
foreach ($requiredPath in $requiredPaths) {
    $outputPath = Join-Path $script:OutputRoot $requiredPath
    if (-not (Test-Path -LiteralPath $outputPath)) {
        $missingPaths += $requiredPath
    }
}

if ($missingPaths.Count -gt 0) {
    throw "Trimmed project is missing required files:`n$($missingPaths -join "`n")"
}

Write-Host "Trimmed Unity project created at $script:OutputRoot"
Write-Host "Copied asset files: $($script:CopiedAssetPaths.Count)"
