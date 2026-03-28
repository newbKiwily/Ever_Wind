$ErrorActionPreference = "Stop"

$projectRoot = Split-Path -Parent $PSScriptRoot
$mapPrefabs = @(
    (Join-Path $projectRoot "Assets\Scenes\Map1 1.prefab"),
    (Join-Path $projectRoot "Assets\Scenes\Map1 2.prefab")
)

$prefabGuidPattern = 'm_SourcePrefab: \{fileID: 100100000, guid: ([0-9a-f]+), type: 3\}'
$documentPattern = '(?ms)^--- !u!(\d+) &(-?\d+)\r?\n(.*?)(?=^--- !u!|\z)'

function Get-ReferencedPrefabPaths {
    param(
        [string[]]$PrefabPaths
    )

    $guids = [System.Collections.Generic.HashSet[string]]::new()
    foreach ($prefabPath in $PrefabPaths) {
        $content = [System.IO.File]::ReadAllText($prefabPath)
        foreach ($match in [regex]::Matches($content, $prefabGuidPattern)) {
            [void]$guids.Add($match.Groups[1].Value)
        }
    }

    $guidToAssetPath = @{}
    Get-ChildItem (Join-Path $projectRoot "Assets") -Recurse -Filter *.meta | ForEach-Object {
        $metaContent = [System.IO.File]::ReadAllText($_.FullName)
        $metaMatch = [regex]::Match($metaContent, '(?m)^guid: ([0-9a-f]+)$')
        if ($metaMatch.Success) {
            $assetPath = $_.FullName.Substring(0, $_.FullName.Length - 5)
            $guidToAssetPath[$metaMatch.Groups[1].Value] = $assetPath
        }
    }

    $paths = [System.Collections.Generic.List[string]]::new()
    foreach ($guid in ($guids | Sort-Object)) {
        if ($guidToAssetPath.ContainsKey($guid)) {
            [void]$paths.Add($guidToAssetPath[$guid])
        }
    }

    return $paths
}

function Add-MeshCollidersToPrefab {
    param(
        [string]$PrefabPath
    )

    $text = [System.IO.File]::ReadAllText($PrefabPath)
    if (-not $text.Contains("MeshFilter:")) {
        return [pscustomobject]@{
            Path = $PrefabPath
            Added = 0
        }
    }

    $prefixMatch = [regex]::Match($text, '^(.*?)(?=^--- !u!)', [System.Text.RegularExpressions.RegexOptions]::Singleline -bor [System.Text.RegularExpressions.RegexOptions]::Multiline)
    $prefix = $prefixMatch.Value
    $matches = [regex]::Matches($text, $documentPattern)

    $docs = [System.Collections.Generic.List[object]]::new()
    $gameObjects = @{}
    $meshFiltersByGameObject = @{}
    $gameObjectsWithCollider = [System.Collections.Generic.HashSet[string]]::new()
    $maxFileId = [long]0

    foreach ($match in $matches) {
        $classId = [int]$match.Groups[1].Value
        $fileId = [long]$match.Groups[2].Value
        $body = $match.Groups[3].Value
        $full = $match.Value

        if ([math]::Abs($fileId) -gt $maxFileId) {
            $maxFileId = [math]::Abs($fileId)
        }

        $doc = [pscustomobject]@{
            ClassId = $classId
            FileId = $fileId
            Body = $body
            Full = $full
        }
        [void]$docs.Add($doc)

        if ($body.StartsWith("GameObject:")) {
            $gameObjects["$fileId"] = $doc
            continue
        }

        $gameObjectMatch = [regex]::Match($body, 'm_GameObject: \{fileID: (-?\d+)\}')
        if (-not $gameObjectMatch.Success) {
            continue
        }

        $gameObjectFileId = $gameObjectMatch.Groups[1].Value

        if ($body.StartsWith("MeshFilter:")) {
            $meshMatch = [regex]::Match($body, '(?m)^  m_Mesh: \{fileID: .*?\}$')
            if ($meshMatch.Success) {
                $meshFiltersByGameObject[$gameObjectFileId] = $meshMatch.Value
            }
            continue
        }

        if ($body -match '^(MeshCollider|BoxCollider|SphereCollider|CapsuleCollider|TerrainCollider|CharacterController):') {
            [void]$gameObjectsWithCollider.Add($gameObjectFileId)
        }
    }

    $newDocs = [System.Collections.Generic.List[string]]::new()
    $addedCount = 0

    foreach ($entry in $meshFiltersByGameObject.GetEnumerator()) {
        $gameObjectFileId = $entry.Key
        if ($gameObjectsWithCollider.Contains($gameObjectFileId)) {
            continue
        }

        if (-not $gameObjects.ContainsKey($gameObjectFileId)) {
            continue
        }

        $maxFileId++
        $newColliderFileId = $maxFileId
        $meshLine = $entry.Value
        $gameObjectDoc = $gameObjects[$gameObjectFileId]

        $replacement = "  - component: {fileID: $newColliderFileId}`r`n  m_Layer:"
        $gameObjectDoc.Full = [regex]::Replace($gameObjectDoc.Full, '(?m)^  m_Layer:', $replacement, 1)

        $newDoc = @"
--- !u!64 &$newColliderFileId
MeshCollider:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: $gameObjectFileId}
  m_Material: {fileID: 0}
  m_IncludeLayers:
    serializedVersion: 2
    m_Bits: 0
  m_ExcludeLayers:
    serializedVersion: 2
    m_Bits: 0
  m_LayerOverridePriority: 0
  m_IsTrigger: 0
  m_ProvidesContacts: 0
  m_Enabled: 1
  serializedVersion: 5
  m_Convex: 0
  m_CookingOptions: 30
$meshLine
"@

        [void]$newDocs.Add($newDoc)
        [void]$gameObjectsWithCollider.Add($gameObjectFileId)
        $addedCount++
    }

    if ($addedCount -eq 0) {
        return [pscustomobject]@{
            Path = $PrefabPath
            Added = 0
        }
    }

    $builder = [System.Text.StringBuilder]::new()
    [void]$builder.Append($prefix)
    foreach ($doc in $docs) {
        [void]$builder.Append($doc.Full)
    }
    foreach ($newDoc in $newDocs) {
        [void]$builder.Append($newDoc)
    }

    $utf8NoBom = [System.Text.UTF8Encoding]::new($false)
    [System.IO.File]::WriteAllText($PrefabPath, $builder.ToString(), $utf8NoBom)

    return [pscustomobject]@{
        Path = $PrefabPath
        Added = $addedCount
    }
}

$targetPrefabs = Get-ReferencedPrefabPaths -PrefabPaths $mapPrefabs
$results = foreach ($prefabPath in $targetPrefabs) {
    Add-MeshCollidersToPrefab -PrefabPath $prefabPath
}

$results |
    Where-Object { $_.Added -gt 0 } |
    Sort-Object Path |
    ForEach-Object { "{0}`t+{1}" -f $_.Path, $_.Added }
