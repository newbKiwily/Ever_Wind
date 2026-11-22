param([switch]$Reset)
$ErrorActionPreference='Stop'
if(-not $Reset){throw '기존 데이터를 모두 삭제하고 초기화하려면 -Reset을 명시하세요.'}
if(Get-Process LoginServer,StartupTests -ErrorAction SilentlyContinue){throw 'DB 초기화 전에 서버를 종료하세요.'}
$projectRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
$serverConfig=Get-Content -LiteralPath (Join-Path $projectRoot 'EverWindServer/LoginServer/ServerApplication.h') -Raw -Encoding UTF8
$databaseHost=[regex]::Match($serverConfig,'dbHost = "([^"]+)"').Groups[1].Value
$databaseUser=[regex]::Match($serverConfig,'dbUser = "([^"]+)"').Groups[1].Value
$databaseSchema=[regex]::Match($serverConfig,'dbSchema = "([^"]+)"').Groups[1].Value
$schemaFile=Join-Path $projectRoot 'EverWindServer/LoginServer/DB/InitializeDatabase.sql'
$schemaSql=Get-Content -LiteralPath $schemaFile -Raw -Encoding UTF8
# 파일의 대상과 서버 접속 설정이 달라진 상태에서는 초기화하지 않는다.
$declaredSchema=[regex]::Match($schemaSql,'(?m)^USE `([^`]+)`;').Groups[1].Value
if($declaredSchema -ne $databaseSchema){throw 'SQL 대상 DB와 서버 설정이 다릅니다. 초기화 파일을 확인하세요.'}
$previousPassword=$env:MYSQL_PWD
$previousEncoding=$OutputEncoding
try {
    $env:MYSQL_PWD=[regex]::Match($serverConfig,'dbPassword = "([^"]+)"').Groups[1].Value
    $OutputEncoding=[System.Text.UTF8Encoding]::new($false)
    $schemaSql | & 'C:/Bitnami/wampstack-8.1.2-0/mariadb/bin/mysql.exe' --host=$databaseHost --user=$databaseUser --database=$databaseSchema --default-character-set=utf8mb4 --batch
    if($LASTEXITCODE -ne 0){throw "DB 초기화 실패: $LASTEXITCODE"}
    Write-Output 'DB 구조 재생성 및 기존 데이터 초기화 완료. 서버를 시작하면 맵 ID가 다시 등록됩니다.'
} finally {
    $env:MYSQL_PWD=$previousPassword
    $OutputEncoding=$previousEncoding
}
