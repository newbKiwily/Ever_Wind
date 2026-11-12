param([ValidateSet('Debug','Release')][string]$BuildConfiguration='Debug')
$ErrorActionPreference='Stop'
$everwindRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$everwindMsbuild=Join-Path ${env:ProgramFiles} 'Microsoft Visual Studio/2022/Community/MSBuild/Current/Bin/MSBuild.exe'
if(-not(Test-Path -LiteralPath $everwindMsbuild)){throw 'Visual Studio 2022 C++ Build Tools were not found.'}
$everwindOut=Join-Path $everwindRoot '.utmp/gameplay/production-server/'
$everwindObj=Join-Path $everwindRoot '.utmp/gameplay/production-server-obj/'
$everwindBinary=[IO.Path]::GetFullPath((Join-Path $everwindOut 'LoginServer.exe'))
if(Get-Process LoginServer -ErrorAction SilentlyContinue | Where-Object {$_.Path -eq $everwindBinary}){throw 'Stop the running production-server/LoginServer.exe before rebuilding it.'}
& $everwindMsbuild (Join-Path $everwindRoot 'EverWindServer/LoginServer/LoginServer.vcxproj') "/p:Configuration=$BuildConfiguration" '/p:Platform=x64' "/p:OutDir=$everwindOut" "/p:IntDir=$everwindObj" '/m:4' '/nologo' '/verbosity:minimal'
if($LASTEXITCODE -ne 0){throw "Server build failed: $LASTEXITCODE"}
