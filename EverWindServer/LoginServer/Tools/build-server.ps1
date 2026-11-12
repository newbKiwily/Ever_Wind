param([ValidateSet('Debug','Release')][string]$BuildConfiguration='Debug')
$ErrorActionPreference='Stop'
$everwindRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
$everwindMsbuild=Join-Path ${env:ProgramFiles} 'Microsoft Visual Studio/2022/Community/MSBuild/Current/Bin/MSBuild.exe'
if(-not(Test-Path -LiteralPath $everwindMsbuild)){throw 'Visual Studio 2022 C++ Build Tools were not found.'}
$everwindOut=Join-Path $everwindRoot 'EverWindServer/LoginServer/bin/'
$everwindObj=Join-Path $everwindRoot 'EverWindServer/LoginServer/obj/'
$everwindBinary=[IO.Path]::GetFullPath((Join-Path $everwindOut 'LoginServer.exe'))
if(Get-Process LoginServer -ErrorAction SilentlyContinue | Where-Object {$_.Path -eq $everwindBinary}){throw 'Stop the running production-server/LoginServer.exe before rebuilding it.'}
& $everwindMsbuild (Join-Path $everwindRoot 'EverWindServer/LoginServer/LoginServer.vcxproj') "/p:Configuration=$BuildConfiguration" '/p:Platform=x64' "/p:OutDir=$everwindOut" "/p:IntDir=$everwindObj" '/m:4' '/nologo' '/verbosity:minimal'
if($LASTEXITCODE -ne 0){throw "Server build failed: $LASTEXITCODE"}

$everwindSharedOutput=Join-Path $everwindOut 'Shared'
New-Item -ItemType Directory -Force -Path $everwindSharedOutput | Out-Null
Copy-Item -LiteralPath (Join-Path $everwindRoot 'EverWindServer/LoginServer/Shared/Maps') -Destination $everwindSharedOutput -Recurse -Force
Get-ChildItem -LiteralPath (Join-Path $everwindRoot 'EverWindServer/LoginServer/Shared') -Filter '*.heights' -File | Copy-Item -Destination $everwindSharedOutput -Force
$everwindOldRules=Join-Path $everwindSharedOutput 'Gameplay.rules'
if(Test-Path -LiteralPath $everwindOldRules){Remove-Item -LiteralPath $everwindOldRules}

