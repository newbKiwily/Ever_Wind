param([string]$Executable = '.utmp/gameplay/production-server/LoginServer.exe')
$ErrorActionPreference = 'Stop'
$everwindRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$everwindExecutable = if ([IO.Path]::IsPathRooted($Executable)) { $Executable } else { Join-Path $everwindRoot $Executable }
if (-not (Test-Path -LiteralPath $everwindExecutable -PathType Leaf)) {
    throw "Build LoginServer first: $everwindExecutable"
}
$everwindPreviousRules = $env:EVERWIND_GAMEPLAY_RULES
$everwindPreviousPath = $env:PATH
try {
    $env:EVERWIND_GAMEPLAY_RULES = Join-Path $everwindRoot 'Shared/Gameplay.rules'
    $everwindMariaDbLib = 'C:\Bitnami\wampstack-8.1.2-0\mariadb\lib'
    if (Test-Path -LiteralPath (Join-Path $everwindMariaDbLib 'libmariadb.dll')) {
        $env:PATH = "$everwindMariaDbLib;$env:PATH"
    }
    & $everwindExecutable
} finally {
    $env:EVERWIND_GAMEPLAY_RULES = $everwindPreviousRules
    $env:PATH = $everwindPreviousPath
}
