# 서버 콘텐츠 파일 계약

`GameData.ewdata`는 UTF-8 텍스트이며 첫 줄은 `format 1`이다. 공백으로 필드를 구분하고 문자열 키는 큰따옴표로 감싼다. 빈 줄과 `#`로 시작하는 주석을 허용한다. 실수는 소수점 `.`을 사용한다. 같은 종류의 ID 중복, 알 수 없는 행/추가 필드/필수 필드 누락은 오류다. 부모 정의를 먼저 쓰고 자식 목록은 0부터 연속된 인덱스로 쓴다.

| 행 | 필드 순서 |
|---|---|
| item | id "key" kind equipmentSlot power heal gatherEventKey craftEventKey |
| skill | id duration impact reach damage cooldown knockback damageRatio areaRadius kind requiresTarget |
| recipe | id duration outputItemId outputCount |
| ingredient | recipeId index itemId count |
| quest | id |
| condition | questId index eventKind targetId requiredCount |
| reward | questId index itemId count |
| tutorial | id nextId eventKind targetId minimumSeconds x y z radius allowMove allowAttack |
| setting | TutorialEnabled 또는 TutorialExitMapId value |
| player | baseAttack baseDefence |
| level | level requiredExperienceToNext |
| skillrank | skillId rank damageMultiplier cooldown |
| monster | id visualId attack defence moveSpeed maxHp radius killExperience attackSkillId attackCooldown |
| resource | id itemId capacity radius gatherTime refillDelay |

`setting`은 TutorialEnabled(0/1)와 TutorialExitMapId가 모두 필요하다. level은 1~20 모두 필요하고 20의 경험치는 0이다. 버프를 제외한 각 스킬에 1~5랭크 표가 필요하다. 최대 레벨·랭크, 포인트 지급/환급 규칙은 합의한 고정 규칙을 유지한다. 기본 이동속도와 HP는 기존 계정 저장값을 유지한다.

skill kind는 단일0/즉발원거리1/범위2/예약버프3이다. requiresTarget과 튜토리얼 허용 값은 0/1이다. monster의 id는 서버 콘텐츠 정의 ID이며 visualId는 기존 클라이언트 외형 인덱스다. 아이템 ID/key와 기존 visualId·퀘스트 이벤트 targetId는 재번호화하지 않았다.

맵 파일은 기존 경계·높이·충돌·스폰 영역·존 설정을 유지하고 다음 행으로 엔티티를 배치한다.

```text
monsterspawn mapId instanceId monsterDefinitionId x z [respawnSeconds]
resourcespawn mapId instanceId resourceDefinitionId x z
```

위치의 y는 기존 높이 데이터에서 계산한다. 몬스터 재생성 시간은 개체 override → 맵 설정 순서다. 기존 monster/resource/resourcecfg/monsterstats 행은 이전 파일 호환용으로 읽을 수 있지만 현재 배포 맵은 정의 ID를 참조한다. 채집물 정의는 전투 스탯과 분리한다.

서버는 시작할 때 파일 전체를 읽고 검증한 뒤 월드를 구성한다. 런타임 DB 기준 정의 조회나 DB로의 대체 로딩은 없다. DB는 인증·계정·인벤토리·퀘스트 진행·개인 성장 저장과 MapId 외래키 등록에 사용한다. 과거 DB 기준 테이블은 삭제하지 않았다.

기본 파일은 배포 폴더 `Content/GameData.ewdata`이며 `EVERWIND_CONTENT_FILE`로 경로를 지정할 수 있다. 맵 경로는 기존 `EVERWIND_MAP_DIRECTORY`를 사용한다. 서버 빌드 도구가 Content와 MapData를 실행 파일 옆에 복사한다. 파일 수정은 재시작 후 적용된다.

Visual Studio의 Debug/Release 빌드도 `CopyGameplayRuntime` 타깃으로 `Content/GameData.ewdata`와 MapData를 실행 파일 옆에 복사한다. 따라서 `bin` 외의 `x64/Debug`, `x64/Release` 실행에도 동일한 파일 구성을 사용한다.

향후 Unity 편집기는 같은 ID/필드 계약으로 서버 파일과 클라이언트 프리팹을 내보내면 된다. 이번 작업에는 편집기와 클라이언트 기준 데이터 수신 변경이 포함되지 않는다.

몬스터 공격은 정의의 attackSkillId/attackCooldown을 사용한다. 현재 몬스터 AI에는 단일·즉발 원거리 공격만 등록할 수 있으며 기존 개체는 스킬1/1.8초를 유지한다.

RO 스킬의 확장 행은 `skillro skillId radius spawnDelay reserved`이다. 마지막 열은 기존 파일 열 수를 유지하기 위한 예약값 0만 허용한다. RO는 승인된 생성 위치에 고정하며 시전자를 추적하지 않는다.
