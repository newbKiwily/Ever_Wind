# Invoke the installed Unity CLI against this repository's actual Unity project.
$ErrorActionPreference = 'Stop'
$everwindProject = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$everwindCliCommand = Get-Command unity -CommandType Application -ErrorAction SilentlyContinue
if ($everwindCliCommand) {
    $everwindCli = $everwindCliCommand.Source
} else {
    $everwindCli = Join-Path $env:LOCALAPPDATA 'Unity\bin\unity.exe'
    if (-not (Test-Path -LiteralPath $everwindCli -PathType Leaf)) {
        throw 'Unity CLI was not found. Install Unity CLI and restart PowerShell.'
    }
}
if ($args.Count -eq 0) {
    Write-Host 'Usage: .\tools\unity.ps1 status --format json'
    Write-Host '       .\tools\unity.ps1 command editor status --format json'
    exit 0
}
$everwindArguments = @($args)
$everwindOptions = @('--project-path', $everwindProject)
if ($everwindArguments[0] -eq 'command') {
    $everwindOptions += @('--caller', 'plugin', '--skill', 'unity-cli')
}
# Keep CLI options before the separator used for editor-command arguments.
$everwindSeparator = [Array]::IndexOf($everwindArguments, '--')
if ($everwindSeparator -ge 0) {
    $everwindBefore = @($everwindArguments | Select-Object -First $everwindSeparator)
    $everwindAfter = @($everwindArguments | Select-Object -Skip $everwindSeparator)
    $everwindArguments = $everwindBefore + $everwindOptions + $everwindAfter
} else {
    $everwindArguments += $everwindOptions
}
& $everwindCli @everwindArguments
exit $LASTEXITCODE
