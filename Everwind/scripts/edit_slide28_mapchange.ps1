param(
    [string]$SourcePptx,
    [string]$OutputPptx
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

Add-Type -AssemblyName System.IO.Compression.FileSystem

$projectRoot = Split-Path $PSScriptRoot -Parent
if (-not $SourcePptx) {
    $sourceFile = Get-ChildItem -LiteralPath $projectRoot -Filter "*.pptx" | Where-Object { $_.Name -like "*backup1.pptx" } | Select-Object -First 1
    if (-not $sourceFile) {
        throw "Source pptx not found in project root."
    }
    $SourcePptx = $sourceFile.FullName
}
if (-not $OutputPptx) {
    $OutputPptx = Join-Path $projectRoot "mapchange_slide28_updated.pptx"
}

function New-TextBody {
    param(
        [string[]]$Lines
    )

    $paragraphs = foreach ($line in $Lines) {
        $escaped = [System.Security.SecurityElement]::Escape($line)
        '<a:p><a:pPr algn="ctr"/><a:r><a:rPr lang="ko-KR" altLang="en-US" sz="900" dirty="0"><a:solidFill><a:schemeClr val="tx1"/></a:solidFill><a:latin typeface="+mj-lt"/></a:rPr><a:t>' + $escaped + '</a:t></a:r><a:endParaRPr lang="ko-KR" altLang="en-US" sz="900" dirty="0"><a:solidFill><a:schemeClr val="tx1"/></a:solidFill><a:latin typeface="+mj-lt"/></a:endParaRPr></a:p>'
    }

    return '<p:txBody><a:bodyPr rtlCol="0" anchor="ctr"/><a:lstStyle/>' + ($paragraphs -join '') + '</p:txBody>'
}

function U {
    param(
        [int[]]$Codes
    )

    return -join ($Codes | ForEach-Object { [char]$_ })
}

function New-ShapeBlock {
    param(
        [string]$Template,
        [int]$Id,
        [string]$Name,
        [int]$X,
        [int]$Y,
        [int]$Cx,
        [int]$Cy,
        [string[]]$Lines
    )

    $block = $Template
    $block = [regex]::Replace($block, '<a:extLst>.*?</a:extLst>', '', [System.Text.RegularExpressions.RegexOptions]::Singleline)
    $block = [regex]::Replace($block, '<p:cNvPr id="\d+" name="[^"]*">', '<p:cNvPr id="' + $Id + '" name="' + $Name + '">', 1)
    $block = [regex]::Replace($block, '<a:off x="\d+" y="\d+"\/><a:ext cx="\d+" cy="\d+"\/>', '<a:off x="' + $X + '" y="' + $Y + '"/><a:ext cx="' + $Cx + '" cy="' + $Cy + '"/>', 1)
    $block = [regex]::Replace($block, '<p:txBody>.*?</p:txBody>', (New-TextBody -Lines $Lines), [System.Text.RegularExpressions.RegexOptions]::Singleline)

    return $block
}

$workDir = Join-Path ([System.IO.Path]::GetDirectoryName($OutputPptx)) "_pptx_edit_slide28"
$zipPath = Join-Path ([System.IO.Path]::GetDirectoryName($OutputPptx)) "_pptx_edit_slide28.zip"

if (Test-Path $workDir) {
    Remove-Item $workDir -Recurse -Force
}
if (Test-Path $zipPath) {
    Remove-Item $zipPath -Force
}
if (Test-Path $OutputPptx) {
    Remove-Item $OutputPptx -Force
}

Copy-Item $SourcePptx $zipPath
[System.IO.Compression.ZipFile]::ExtractToDirectory($zipPath, $workDir)

$slidePath = Join-Path $workDir "ppt\slides\slide28.xml"
$content = Get-Content -LiteralPath $slidePath -Raw -Encoding UTF8

$matches = @{}
foreach ($id in 32, 33, 34, 35, 36, 37) {
    $match = [regex]::Match($content, '<p:sp><p:nvSpPr><p:cNvPr id="' + $id + '".*?</p:sp>', [System.Text.RegularExpressions.RegexOptions]::Singleline)
    if (-not $match.Success) {
        throw "Shape id $id not found in slide28.xml"
    }
    $matches[$id] = $match
}

$startIndex = $matches[32].Index
$endIndex = $matches[37].Index + $matches[37].Length
$template = $matches[34].Value

$rowY = 2035459
$bottomY = 3313460
$width = 1350000
$height = 825288
$xPositions = @{
    32 = 1064421
    33 = 2717421
    34 = 4370421
    38 = 6023421
    36 = 7676421
    35 = 9329421
}

$replacement = @(
    New-ShapeBlock -Template $template -Id 32 -Name "사각형: 잘린 한쪽 모서리 31" -X $xPositions[32] -Y $rowY -Cx $width -Cy $height -Lines @(
        "PacketMethod.cs",
        "C2S_MAP_CAHNGE_REQ",
        (U @(0xC218, 0xC2E0))
    )
    New-ShapeBlock -Template $template -Id 33 -Name "사각형: 잘린 한쪽 모서리 32" -X $xPositions[33] -Y $rowY -Cx $width -Cy $height -Lines @(
        "PacketMethod",
        "(Server)",
        "HandleMapChange()",
        (U @(0xC138, 0xC158, 0x0020, 0xB9F5, 0x0020, 0xC774, 0xB3D9, 0x0020, 0xCC98, 0xB9AC))
    )
    New-ShapeBlock -Template $template -Id 34 -Name "사각형: 잘린 한쪽 모서리 33" -X $xPositions[34] -Y $rowY -Cx $width -Cy $height -Lines @(
        (U @(0xBCC0, 0xACBD, 0xB41C, 0x0020, 0xB9F5, 0xC758)),
        "SpawnPos",
        (U @(0xC218, 0xC2E0))
    )
    New-ShapeBlock -Template $template -Id 38 -Name "사각형: 잘린 한쪽 모서리 37" -X $xPositions[38] -Y $rowY -Cx $width -Cy $height -Lines @(
        "OtherPlayer",
        (U @(0x0045, 0x006E, 0x0065, 0x006D, 0x0079, 0x0020, 0xC815, 0xBCF4)),
        (U @(0xC218, 0xC2E0))
    )
    New-ShapeBlock -Template $template -Id 36 -Name "사각형: 잘린 한쪽 모서리 35" -X $xPositions[36] -Y $rowY -Cx $width -Cy $height -Lines @(
        "PacketMethod.cs",
        "Handle()",
        ("Datacenter.cs" + (U @(0xC5D0))),
        (U @(0xB9F5, 0x0020, 0xC815, 0xBCF4, 0x0020, 0xC800, 0xC7A5))
    )
    New-ShapeBlock -Template $template -Id 35 -Name "사각형: 잘린 한쪽 모서리 34" -X $xPositions[35] -Y $rowY -Cx $width -Cy $height -Lines @(
        "PacketMethod",
        "(Server)",
        "SC2_MAP_CHANGE_ACK",
        (U @(0xC218, 0xC2E0))
    )
    New-ShapeBlock -Template $template -Id 37 -Name "사각형: 잘린 한쪽 모서리 36" -X $xPositions[35] -Y $bottomY -Cx $width -Cy $height -Lines @(
        "PacketMethod.cs",
        "HandleMapChangeAck()",
        "WorldLoader.cs",
        (U @(0xC800, 0xC7A5, 0x0020, 0xB370, 0xC774, 0xD130, 0x0020, 0xB85C, 0xB4DC, 0x002F, 0xBC18, 0xC601))
    )
) -join ''

$updated = $content.Substring(0, $startIndex) + $replacement + $content.Substring($endIndex)
[System.IO.File]::WriteAllText($slidePath, $updated, [System.Text.UTF8Encoding]::new($false))

Remove-Item $zipPath -Force
[System.IO.Compression.ZipFile]::CreateFromDirectory($workDir, $zipPath)
Move-Item $zipPath $OutputPptx
