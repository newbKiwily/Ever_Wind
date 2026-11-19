#pragma once
#include "GameplayData.h"
namespace EverWind::Gameplay
{
struct GameDataHub
{
    // 임시 넉백 확인용이다. false로 되돌리면 변경 전 피해량과 3회 넉백을 복원한다.
    // 앞서 설정한 몬스터 HP 1000은 이 임시 설정의 복원 대상이 아니다.
    static constexpr bool TemporaryKnockbackTest = false;

    static constexpr int ConsecutiveHitThreshold = TemporaryKnockbackTest ? 1 : 3;

    static constexpr float PlayerDamageMultiplier = TemporaryKnockbackTest ? .1f : 1.0f;

    static constexpr float PlayerHitSeconds = .5f;
    static constexpr float MonsterHitSeconds = 1.0f;
    static constexpr float KnockbackSeconds = .3f;
    static constexpr float KnockbackDistance = 2.0f;
    static constexpr float PlayerReviveDelaySeconds = 10.0f;
    static constexpr float AreaTargetReportGraceSeconds = 1.0f;

    std::unordered_map<int, Skill> skills;

    std::unordered_map<int, Recipe> recipes;

    std::unordered_map<int, ItemDefinition> items;

    std::unordered_map<int, QuestDefinition> quests;

    std::unordered_map<int, TutorialDefinition> tutorials;

    bool tutorialEnabled = true;

    int tutorialExitMapId = 1;

    void Validate() const;
};
}
