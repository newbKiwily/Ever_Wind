-- 현재 DB 구조 전체를 재생성하는 초기화 SQL (2026-11-22).
-- 실행하면 아래 테이블의 기존 계정/인벤토리/진행/기준 데이터가 전부 삭제된다.
-- 실제 행이나 인증 정보는 포함하지 않는다. 자동 증가 카운터도 초기화한다.
-- 서버를 종료한 뒤 실행한다. 서버는 시작 시 맵 ID를 파일에서 다시 등록한다.
-- 현재 남아 있는 과거 기준 테이블도 구조 일치를 위해 포함하며 행은 넣지 않는다.
CREATE DATABASE IF NOT EXISTS `GameServer` CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci;
USE `GameServer`;


/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!40101 SET NAMES utf8mb4 */;
/*!40103 SET @OLD_TIME_ZONE=@@TIME_ZONE */;
/*!40103 SET TIME_ZONE='+00:00' */;
/*!40014 SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;
DROP TABLE IF EXISTS `gamesetting`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `gamesetting` (
  `SettingKey` varchar(64) NOT NULL,
  `IntValue` int(11) NOT NULL,
  PRIMARY KEY (`SettingKey`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
/*!40101 SET character_set_client = @saved_cs_client */;
DROP TABLE IF EXISTS `inventory`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `inventory` (
  `InstanceID` bigint(20) NOT NULL AUTO_INCREMENT,
  `UserID` varchar(32) NOT NULL,
  `ItemID` varchar(64) NOT NULL,
  `Amount` int(11) NOT NULL DEFAULT 1,
  `SlotIndex` int(11) NOT NULL DEFAULT 0,
  `IsEquipped` tinyint(1) NOT NULL DEFAULT 0,
  PRIMARY KEY (`InstanceID`),
  UNIQUE KEY `UK_User_Slot` (`UserID`,`SlotIndex`),
  CONSTRAINT `FK_Inventory_UserAccount` FOREIGN KEY (`UserID`) REFERENCES `useraccount` (`UserID`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
/*!40101 SET character_set_client = @saved_cs_client */;
DROP TABLE IF EXISTS `itemdefinition`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `itemdefinition` (
  `ItemID` int(11) NOT NULL,
  `ItemKey` varchar(64) NOT NULL,
  `Name` varchar(128) NOT NULL,
  `Kind` int(11) NOT NULL,
  `EquipmentSlot` int(11) NOT NULL,
  `Power` int(11) NOT NULL,
  `Heal` float NOT NULL,
  `GatherEventKey` int(11) NOT NULL,
  `CraftEventKey` int(11) NOT NULL,
  PRIMARY KEY (`ItemID`),
  UNIQUE KEY `ItemKey` (`ItemKey`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
/*!40101 SET character_set_client = @saved_cs_client */;
DROP TABLE IF EXISTS `mapinfo`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `mapinfo` (
  `MapId` int(11) NOT NULL,
  PRIMARY KEY (`MapId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
/*!40101 SET character_set_client = @saved_cs_client */;
DROP TABLE IF EXISTS `questcondition`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `questcondition` (
  `QuestID` int(11) NOT NULL,
  `ConditionIndex` int(11) NOT NULL,
  `EventKind` int(11) NOT NULL,
  `TargetID` int(11) NOT NULL,
  `RequiredCount` int(11) NOT NULL,
  PRIMARY KEY (`QuestID`,`ConditionIndex`),
  CONSTRAINT `questcondition_ibfk_1` FOREIGN KEY (`QuestID`) REFERENCES `questdefinition` (`QuestID`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
/*!40101 SET character_set_client = @saved_cs_client */;
DROP TABLE IF EXISTS `questdefinition`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `questdefinition` (
  `QuestID` int(11) NOT NULL,
  `Name` varchar(128) NOT NULL,
  `Description` text NOT NULL,
  PRIMARY KEY (`QuestID`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
/*!40101 SET character_set_client = @saved_cs_client */;
DROP TABLE IF EXISTS `questreward`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `questreward` (
  `QuestID` int(11) NOT NULL,
  `RewardIndex` int(11) NOT NULL,
  `ItemID` int(11) NOT NULL,
  `Amount` int(11) NOT NULL,
  PRIMARY KEY (`QuestID`,`RewardIndex`),
  KEY `ItemID` (`ItemID`),
  CONSTRAINT `questreward_ibfk_1` FOREIGN KEY (`QuestID`) REFERENCES `questdefinition` (`QuestID`),
  CONSTRAINT `questreward_ibfk_2` FOREIGN KEY (`ItemID`) REFERENCES `itemdefinition` (`ItemID`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
/*!40101 SET character_set_client = @saved_cs_client */;
DROP TABLE IF EXISTS `recipe`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `recipe` (
  `RecipeID` int(11) NOT NULL,
  `CraftSeconds` float NOT NULL,
  `ResultItemID` int(11) NOT NULL,
  `ResultCount` int(11) NOT NULL,
  PRIMARY KEY (`RecipeID`),
  KEY `ResultItemID` (`ResultItemID`),
  CONSTRAINT `recipe_ibfk_1` FOREIGN KEY (`ResultItemID`) REFERENCES `itemdefinition` (`ItemID`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
/*!40101 SET character_set_client = @saved_cs_client */;
DROP TABLE IF EXISTS `recipeingredient`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `recipeingredient` (
  `RecipeID` int(11) NOT NULL,
  `IngredientIndex` int(11) NOT NULL,
  `ItemID` int(11) NOT NULL,
  `RequiredCount` int(11) NOT NULL,
  PRIMARY KEY (`RecipeID`,`IngredientIndex`),
  KEY `ItemID` (`ItemID`),
  CONSTRAINT `recipeingredient_ibfk_1` FOREIGN KEY (`RecipeID`) REFERENCES `recipe` (`RecipeID`),
  CONSTRAINT `recipeingredient_ibfk_2` FOREIGN KEY (`ItemID`) REFERENCES `itemdefinition` (`ItemID`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
/*!40101 SET character_set_client = @saved_cs_client */;
DROP TABLE IF EXISTS `skilldefinition`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `skilldefinition` (
  `SkillID` int(11) NOT NULL,
  `Name` varchar(128) NOT NULL,
  `Duration` float NOT NULL,
  `ImpactSeconds` float NOT NULL,
  `Reach` float NOT NULL,
  `Damage` float NOT NULL,
  `Cooldown` float NOT NULL,
  `Knockback` float NOT NULL,
  `DamageRatio` float NOT NULL,
  `AreaRadius` float NOT NULL,
  `SkillKind` smallint(5) unsigned NOT NULL DEFAULT 0,
  `RequiresTarget` tinyint(1) NOT NULL DEFAULT 1,
  PRIMARY KEY (`SkillID`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
/*!40101 SET character_set_client = @saved_cs_client */;
DROP TABLE IF EXISTS `tutorialdefinition`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `tutorialdefinition` (
  `StepID` int(11) NOT NULL,
  `NextStepID` int(11) NOT NULL,
  `EventKind` int(11) NOT NULL,
  `TargetID` int(11) NOT NULL DEFAULT -1,
  `MinimumSeconds` float NOT NULL,
  `TargetX` float NOT NULL,
  `TargetY` float NOT NULL,
  `TargetZ` float NOT NULL,
  `TargetRadius` float NOT NULL,
  `AllowMove` tinyint(1) NOT NULL,
  `AllowAttack` tinyint(1) NOT NULL,
  PRIMARY KEY (`StepID`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
/*!40101 SET character_set_client = @saved_cs_client */;
DROP TABLE IF EXISTS `useraccount`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `useraccount` (
  `UserID` varchar(32) NOT NULL,
  `Password` varchar(64) NOT NULL DEFAULT '',
  `CreatedAt` datetime DEFAULT current_timestamp(),
  PRIMARY KEY (`UserID`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
/*!40101 SET character_set_client = @saved_cs_client */;
DROP TABLE IF EXISTS `userinfo`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `userinfo` (
  `UserID` varchar(32) NOT NULL,
  `MapId` int(11) NOT NULL DEFAULT 0,
  `PosX` float DEFAULT NULL,
  `PosY` float DEFAULT NULL,
  `PosZ` float DEFAULT NULL,
  `MaxHP` float NOT NULL DEFAULT 100,
  `HP` float NOT NULL DEFAULT 100,
  `DefencePower` int(11) NOT NULL DEFAULT 3,
  `AttackPower` int(11) NOT NULL DEFAULT 10,
  `Speed` float NOT NULL DEFAULT 4.5,
  `TutorialStep` int(11) NOT NULL DEFAULT 0,
  `GrowthLevel` int(11) NOT NULL DEFAULT 1,
  `GrowthExperience` int(11) NOT NULL DEFAULT 0,
  `SkillPoints` int(11) NOT NULL DEFAULT 3,
  `StatPoints` int(11) NOT NULL DEFAULT 3,
  `AttackRank` int(11) NOT NULL DEFAULT 1,
  `DefenceRank` int(11) NOT NULL DEFAULT 1,
  `MoveSpeedRank` int(11) NOT NULL DEFAULT 1,
  PRIMARY KEY (`UserID`),
  KEY `FK_UserInfo_MapInfo` (`MapId`),
  CONSTRAINT `FK_UserInfo_MapInfo` FOREIGN KEY (`MapId`) REFERENCES `mapinfo` (`MapId`),
  CONSTRAINT `FK_UserInfo_UserAccount` FOREIGN KEY (`UserID`) REFERENCES `useraccount` (`UserID`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
/*!40101 SET character_set_client = @saved_cs_client */;
DROP TABLE IF EXISTS `userquest`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `userquest` (
  `UserID` varchar(32) NOT NULL,
  `QuestId` int(11) NOT NULL,
  `IsCompleted` tinyint(1) NOT NULL DEFAULT 0,
  `RewardClaimed` tinyint(1) NOT NULL DEFAULT 0,
  PRIMARY KEY (`UserID`,`QuestId`),
  CONSTRAINT `FK_UserQuest_UserAccount` FOREIGN KEY (`UserID`) REFERENCES `useraccount` (`UserID`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
/*!40101 SET character_set_client = @saved_cs_client */;
DROP TABLE IF EXISTS `userquestprogress`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `userquestprogress` (
  `UserID` varchar(32) NOT NULL,
  `QuestId` int(11) NOT NULL,
  `ConditionIndex` int(11) NOT NULL,
  `CurrentCount` int(11) NOT NULL DEFAULT 0,
  PRIMARY KEY (`UserID`,`QuestId`,`ConditionIndex`),
  CONSTRAINT `FK_UserQuestProgress_UserQuest` FOREIGN KEY (`UserID`, `QuestId`) REFERENCES `userquest` (`UserID`, `QuestId`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
/*!40101 SET character_set_client = @saved_cs_client */;
DROP TABLE IF EXISTS `userskillrank`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `userskillrank` (
  `UserID` varchar(255) NOT NULL,
  `SkillID` int(11) NOT NULL,
  `SkillRank` int(11) NOT NULL,
  PRIMARY KEY (`UserID`,`SkillID`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
/*!40101 SET character_set_client = @saved_cs_client */;
/*!40103 SET TIME_ZONE=@OLD_TIME_ZONE */;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;

