-- Map identities remain for UserInfo's foreign key. Spatial data now lives in Shared/Maps.
DROP TABLE IF EXISTS Monster;
ALTER TABLE MapInfo DROP COLUMN IF EXISTS SpawnX,DROP COLUMN IF EXISTS SpawnY,DROP COLUMN IF EXISTS SpawnZ,
 DROP COLUMN IF EXISTS SpawnEnemyX,DROP COLUMN IF EXISTS SpawnEnemyY,DROP COLUMN IF EXISTS SpawnEnemyZ,
 DROP COLUMN IF EXISTS SpawnEnemyRadius,DROP COLUMN IF EXISTS MaxEnemyCount;
