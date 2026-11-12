param([string]$Executable = 'EverWindServer/LoginServer/bin/LoginServer.exe')
$ErrorActionPreference = 'Stop'
$everwindRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
$everwindExecutable = if ([IO.Path]::IsPathRooted($Executable)) { $Executable } else { Join-Path $everwindRoot $Executable }
if (-not (Test-Path -LiteralPath $everwindExecutable -PathType Leaf)) {
    throw "Build LoginServer first: $everwindExecutable"
}
$everwindPreviousMaps = $env:EVERWIND_MAP_DIRECTORY
$everwindPreviousPath = $env:PATH
try {
    $env:EVERWIND_MAP_DIRECTORY = Join-Path $everwindRoot 'EverWindServer/LoginServer/Shared/Maps'
    $everwindMariaDbLib = 'C:\Bitnami\wampstack-8.1.2-0\mariadb\lib'
    if (Test-Path -LiteralPath (Join-Path $everwindMariaDbLib 'libmariadb.dll')) {
        $env:PATH = "$everwindMariaDbLib;$env:PATH"
    }
    & $everwindExecutable
} finally {
    $env:EVERWIND_MAP_DIRECTORY = $everwindPreviousMaps
    $env:PATH = $everwindPreviousPath
}
