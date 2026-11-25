Add-Type -AssemblyName System.Drawing
$outDir = $PSScriptRoot
$fonts = [System.Drawing.Text.PrivateFontCollection]::new()
$fonts.AddFontFile((Join-Path $PSScriptRoot '../../Assets/TextMesh Pro/Resources/Noto_Sans_KR/static/NotoSansKR-Regular.ttf'))
$family = $fonts.Families[0]
$ink = [System.Drawing.ColorTranslator]::FromHtml('#172B3A')
$muted = [System.Drawing.ColorTranslator]::FromHtml('#526777')
$edge = [System.Drawing.ColorTranslator]::FromHtml('#627E8E')
function Start-Canvas([string]$title, [string]$subtitle, [int]$height = 1080) {
    $script:bitmap = [System.Drawing.Bitmap]::new(1800, $height)
    $script:g = [System.Drawing.Graphics]::FromImage($bitmap)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit
    $g.Clear([System.Drawing.ColorTranslator]::FromHtml('#F6F8FA'))
    Txt $title 45 24 38 $ink
    Txt $subtitle 48 90 20 $muted
}
function Txt([string]$s,[float]$x,[float]$y,[float]$size = 23,$color=$ink) {
    $font=[System.Drawing.Font]::new($family,$size,[System.Drawing.FontStyle]::Regular,[System.Drawing.GraphicsUnit]::Pixel)
    $brush=[System.Drawing.SolidBrush]::new($color)
    $g.DrawString($s,$font,$brush,$x,$y)
    $brush.Dispose(); $font.Dispose()
}
function Box([float]$x,[float]$y,[float]$w,[float]$h,[string]$title,[string]$body,[string]$fill='#FFFFFF') {
    $brush=[System.Drawing.SolidBrush]::new([System.Drawing.ColorTranslator]::FromHtml($fill))
    $pen=[System.Drawing.Pen]::new([System.Drawing.ColorTranslator]::FromHtml('#CBD7DF'),2)
    $g.FillRectangle($brush,$x,$y,$w,$h); $g.DrawRectangle($pen,$x,$y,$w,$h)
    Txt $title ($x+18) ($y+12) 26
    if($body) { Txt $body ($x+18) ($y+54) 21 $muted }
    $brush.Dispose(); $pen.Dispose()
}
function Arrow([float]$x1,[float]$y1,[float]$x2,[float]$y2,[string]$label='', [bool]$dashed=$false) {
    $pen=[System.Drawing.Pen]::new($edge,3)
    $pen.CustomEndCap=[System.Drawing.Drawing2D.AdjustableArrowCap]::new(5,6)
    if($dashed) { $pen.DashStyle=[System.Drawing.Drawing2D.DashStyle]::Dash }
    $g.DrawLine($pen,$x1,$y1,$x2,$y2)
    if($label) { Txt $label (($x1+$x2)/2+8) (($y1+$y2)/2-32) 20 $muted }
    $pen.Dispose()
}
function Line([float]$x1,[float]$y1,[float]$x2,[float]$y2) {
    $pen=[System.Drawing.Pen]::new($edge,3)
    $g.DrawLine($pen,$x1,$y1,$x2,$y2); $pen.Dispose()
}
function Finish([string]$name,[string]$source,[int]$y=1010) {
    Txt $source 48 $y 19 $muted
    $bitmap.Save((Join-Path $outDir $name),[System.Drawing.Imaging.ImageFormat]::Png)
    $g.Dispose(); $bitmap.Dispose()
}

Start-Canvas 'EverWind 시스템 아키텍처' '최신 누적 기록 기준 · 입력/표현, 서버 확정, 파일 정의와 개인 저장의 책임'
Txt 'UNITY C# CLIENT' 55 145 26
Txt 'C++ IOCP SERVER' 650 145 26
Txt 'DEFINITIONS / PERSISTENCE' 1240 145 26
Box 45 200 500 122 'InputManager → Controller' "입력·대상·접근·취소 조정`nState는 행동 허용 정책" '#EAF4FF'
Box 45 375 500 122 'Action / MovementMotor / Skill SO' "로컬 이동 예측·스킬 표현 실행`n공유 정의와 사용별 실행 상태 분리" '#EAF4FF'
Box 45 550 500 122 'NetworkClient / PacketMethod' "공통 TCP · 패킷 빌드/복사 파싱`nUnityMainThreadDispatcher로 분배" '#EAF4FF'
Box 45 790 500 135 'ContainerInMap / WorldLoader' "스냅샷 → Addressables 준비 → 생성`n송수신기·AlivePresentation으로 표현" '#EAF4FF'
Arrow 295 322 295 375
Arrow 295 497 295 550
Arrow 295 672 295 790 '수신 이벤트'
Box 645 200 500 122 'Session / IOCP / PacketMethod' "프레임 조립 → 크기/ID/인증 검사`nRequestKind별 핸들러 → 게임 큐" '#EAF6EF'
Box 645 375 500 122 'MapManager' "단일 게임 스레드·작업 큐·맵 수명`n큐 실행 시 최신 상태로 검증" '#EAF6EF'
Box 645 550 500 122 'GameplayProcessor / Validation' "요청·갱신 순서와 공통 결과 조정`n시작 / 타격 / 완료 조건을 각각 검사" '#EAF6EF'
Box 645 790 500 135 'Map → Actor' "Player / Monster / Resource / StaticLevel`n명중·HP·보상·성장 결과는 서버 확정" '#EAF6EF'
Arrow 895 322 895 375
Arrow 895 497 895 550
Arrow 895 672 895 790
Line 545 590 585 590
Line 585 590 585 235
Arrow 585 235 645 235
Txt 'TCP' 550 335 18 $muted
Line 645 305 610 305
Line 610 305 610 640
Arrow 610 640 545 640
Box 1235 200 520 145 'GameData.ewdata' "GameDataFile.Load → GameDataHub`n아이템·스킬·성장·몬스터·채집물 정의`n누락/잘못된 참조는 초기화 실패" '#FFF4DC'
Box 1235 420 520 145 'Content/MapData' "맵 TXT · float 높이 격자`n경계·스폰·정적 충돌·트리거·배치`n종류 정의 ID와 개체 ID 구분" '#FFF4DC'
Box 1235 720 520 170 'Queries / DBManager / DB' "인증·계정·개인 진행·인벤토리`nUserInfo / UserSkillRank 등 저장`n공통 기준 테이블은 실행 중 조회 안 함" '#F0EAFE'
Line 1235 270 1190 270
Line 1190 270 1190 420
Arrow 1190 420 1145 420
Arrow 1235 490 1145 490
Arrow 1145 850 1235 850
Finish 'architecture.png' '근거: 누적 기록 A-3~A-10, I·K·L·M·R·V, A-23~A-24. 연결선은 기록에 서술된 책임/데이터 흐름의 요약.'

Start-Canvas '서버 객체 소유와 Actor 상속' '구현 기록의 관계만 표시 · Map과 Session은 동일한 Player 객체를 shared_ptr로 공유'
Box 50 170 790 110 'ServerApplication' '직접 조립·초기화·시작·종료' '#EAF6EF'
Box 50 370 370 155 'MapManager' "unique_ptr<Map> 목록`nunique_ptr<Processor> · 큐" '#EAF6EF'
Box 50 665 370 155 'Map' "shared_ptr<Actor> 목록`n종류별 인덱스는 비소유" '#EAF6EF'
Arrow 235 280 235 370 '소유'
Arrow 235 525 235 665 '소유'
Box 475 370 370 155 'Session' "shared_ptr<Player>`nweak Session 송신 콜백" '#EAF4FF'
Box 475 665 370 155 'Player' "개인 성장·장비·퀘스트`nMap과 Session이 공동 소유" '#EAF4FF'
Arrow 420 740 475 740
Txt '동일 Player 실체' 315 825 21
Arrow 660 525 660 665 '공유 소유'
Box 1050 170 650 130 'Actor' "ID / map / position / radius / istrigger`n공통 HP·행동·넉백 상태" '#FFF4DC'
Box 1150 390 550 100 'Player' 'Actor 직접 상속 + IDamageable' '#FFFFFF'
Box 1150 540 550 100 'Monster' 'Actor 직접 상속 + IDamageable · 서버 AI' '#FFFFFF'
Box 1150 690 550 100 'Resource' 'Actor 직접 상속 · 잔량/점유/리필' '#FFFFFF'
Box 1150 840 550 100 'StaticLevel' 'Actor 직접 상속 · istrigger=false' '#FFFFFF'
Line 1000 250 1000 890
Arrow 1000 250 1050 250
Line 1150 440 1000 440
Line 1150 590 1000 590
Line 1150 740 1000 740
Line 1150 890 1000 890
Txt '직접 상속' 960 330 21 $muted
Txt 'istrigger 기본 true: 플레이어·몬스터·채집물 통과.' 50 900 22
Txt 'Player 게임 상태 변경은 단일 게임 스레드에서 수행.' 50 943 22
Finish 'ownership.png' '근거: 누적 기록 A-5~A-7, I·P·R. 화살표의 소유/상속 표기는 각 라벨을 따른다.'

Start-Canvas '스킬 요청에서 명중 확정까지' '일반 단일 공격과 RO 탐지 보고의 책임 경계 · ACK는 명중 결과와 다름' 1120
$xs=@(215,660,1105,1550)
$labels=@('Controller / AttackAction','공유 SkillDefinition SO','서버 게임 큐 / Processor','Map / 대상 / 개인 성장')
for($i=0;$i -lt 4;$i++) {
    Box ($xs[$i]-185) 155 370 90 $labels[$i] '' '#EAF4FF'
    $pen=[System.Drawing.Pen]::new([System.Drawing.ColorTranslator]::FromHtml('#CCD7DF'),2)
    $pen.DashStyle=[System.Drawing.Drawing2D.DashStyle]::Dash
    $g.DrawLine($pen,$xs[$i],245,$xs[$i],990);$pen.Dispose()
}
Arrow 215 300 1105 300 '① Attack 요청: 대상/승인 중심 후보'
Txt '대상·접근·습득/사거리 확인' 35 258 20
Arrow 1105 370 215 370 '② 검증 후 ACK / ActionStarted: ActionId·start·duration·center'
Arrow 215 450 660 450 '③ SO.Begin(action) / Tick'
Txt '사용별 상태: ActionId / NextHit / RO / Center / Origin' 65 500 21
Arrow 660 600 1105 600 '④ RO는 타격 Sequence + 탐지 ID 보고'
Txt '여러 OverlapSphere → 대상 중복 제거' 380 640 21
Txt '단일 공격은 서버 타격 시 대상/거리/Geometry 재검사' 45 700 21
Arrow 1105 770 1550 770 '⑤ 행동·순서·시각·대상·거리 검사'
Txt '서버가 피해/사망·피격/넉백·처치 경험치 확정' 925 812 21
Arrow 1550 900 215 900 '⑥ Hit / Stats / Growth / ActionEnded 등 결과 이벤트'
Txt '행동 종료·취소·사망 → SO.End(action), RO/시전 효과 정리' 50 960 21
Finish 'skill-flow.png' '근거: 누적 기록 A-3~A-4, A-14·A-16·A-23~A-24, Y-2~Y-5, Z-4~Z-7. 최신 통합 전투 재검증을 뜻하지 않음.' 1050
$fonts.Dispose()
