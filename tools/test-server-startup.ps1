param([switch]$SkipBuild)
$ErrorActionPreference = 'Stop'
$everwindRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
Push-Location $everwindRoot
try {
    if (-not $SkipBuild) { & (Join-Path $PSScriptRoot 'build-server.ps1') }
    $everwindTestDirectory = Join-Path $everwindRoot '.utmp/server-startup'
    New-Item -ItemType Directory -Force -Path $everwindTestDirectory | Out-Null
    [xml]$everwindProject = Get-Content 'EverWindServer/LoginServer/LoginServer.vcxproj' -Raw
    $everwindObjectDirectory = Join-Path $everwindRoot '.utmp/gameplay/production-server-obj'
    $everwindObjects = $everwindProject.SelectNodes("//*[local-name()='ClCompile' and @Include]") |
        Where-Object { [IO.Path]::GetFileName($_.Include) -ne 'main.cpp' } |
        ForEach-Object {
            $everwindObject = Join-Path $everwindObjectDirectory ([IO.Path]::GetFileNameWithoutExtension($_.Include) + '.obj')
            if (-not (Test-Path -LiteralPath $everwindObject)) { throw "Missing server object: $everwindObject" }
            '"' + $everwindObject + '"'
        }
    $everwindResponse = Join-Path $everwindTestDirectory 'objects.rsp'
    Set-Content -LiteralPath $everwindResponse -Value $everwindObjects -Encoding ascii
    $everwindCommand = Join-Path $everwindTestDirectory 'build.cmd'
    # Link a test executable against production objects without adding a VS project.
    $everwindBuild = @'
@echo off
call "C:\Program Files\Microsoft Visual Studio\2022\Community\VC\Auxiliary\Build\vcvars64.bat" >nul
if errorlevel 1 exit /b %errorlevel%
cl /nologo /std:c++20 /EHsc /MDd /D_DEBUG /utf-8 /I "EverWindServer\LoginServer" /I "C:\Bitnami\wampstack-8.1.2-0\mariadb\include\mysql" "tests\server\StartupTests.cpp" @".utmp\server-startup\objects.rsp" /Fe:".utmp\server-startup\StartupTests.exe" /Fo:".utmp\server-startup\StartupTests.obj" /link /LIBPATH:"C:\Bitnami\wampstack-8.1.2-0\mariadb\lib"
exit /b %errorlevel%
'@
    Set-Content -LiteralPath $everwindCommand -Value $everwindBuild -Encoding ascii
    & cmd /c $everwindCommand
    if ($LASTEXITCODE -ne 0) { throw 'Startup test build failed' }
    $everwindPreviousPath = $env:PATH
    try {
        $env:PATH = "C:\Bitnami\wampstack-8.1.2-0\mariadb\lib;$env:PATH"
        & (Join-Path $everwindTestDirectory 'StartupTests.exe')
        if ($LASTEXITCODE -ne 0) { throw 'Startup tests failed' }
    } finally { $env:PATH = $everwindPreviousPath }
} finally { Pop-Location }
