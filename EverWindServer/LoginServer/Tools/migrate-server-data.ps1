param([ValidateSet('Definitions','Maps','All')][string]$Stage='All')
$ErrorActionPreference='Stop'
$everwindRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
if(Get-Process LoginServer,StartupTests -ErrorAction SilentlyContinue){throw 'Stop the server before migrating its data.'}
$everwindConfig=Get-Content (Join-Path $everwindRoot 'EverWindServer/LoginServer/ServerApplication.h') -Raw
$everwindHost=[regex]::Match($everwindConfig,'dbHost = "([^"]+)"').Groups[1].Value
$everwindUser=[regex]::Match($everwindConfig,'dbUser = "([^"]+)"').Groups[1].Value
$everwindSchema=[regex]::Match($everwindConfig,'dbSchema = "([^"]+)"').Groups[1].Value
$everwindPreviousPassword=$env:MYSQL_PWD
try {
    $env:MYSQL_PWD=[regex]::Match($everwindConfig,'dbPassword = "([^"]+)"').Groups[1].Value
    $everwindFiles=@()
    if($Stage -in @('Definitions','All')){$everwindFiles+='20261112_game_definitions.sql'}
    if($Stage -in @('Maps','All')){
        foreach($everwindMap in @('EverWindServer/LoginServer/Shared/Maps/Map0.txt','EverWindServer/LoginServer/Shared/Maps/Map1.txt')){
            if(-not(Test-Path -LiteralPath (Join-Path $everwindRoot $everwindMap))){throw "Missing exported map: $everwindMap"}
        }
        $everwindFiles+='20261112_map_files.sql'
    }
    foreach($everwindFile in $everwindFiles){
        $everwindSql=Get-Content (Join-Path $everwindRoot "EverWindServer/LoginServer/DB/Migrations/$everwindFile") -Raw
        $everwindSql | & 'C:/Bitnami/wampstack-8.1.2-0/mariadb/bin/mysql.exe' --host=$everwindHost --user=$everwindUser --database=$everwindSchema --default-character-set=utf8mb4 --batch
        if($LASTEXITCODE -ne 0){throw "Migration failed: $everwindFile"}
        Write-Output "Applied: $everwindFile"
    }
}finally{$env:MYSQL_PWD=$everwindPreviousPassword}
