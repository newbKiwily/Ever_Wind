-- 스킬 실행 종류와 범위기의 대상 요구 여부. 기존 수치와 계정 데이터는 유지한다.
ALTER TABLE SkillDefinition ADD COLUMN IF NOT EXISTS SkillKind SMALLINT UNSIGNED NOT NULL DEFAULT 0;
ALTER TABLE SkillDefinition ADD COLUMN IF NOT EXISTS RequiresTarget BOOLEAN NOT NULL DEFAULT TRUE;
-- 기존 광역 정의만 새 범위 공격 종류로 전환한다.
UPDATE SkillDefinition SET SkillKind=2 WHERE AreaRadius>0 AND SkillKind=0;
