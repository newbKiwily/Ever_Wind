using System;
using System.Collections.Generic;
using UnityEngine;

public static class UIEvents
{
    public static bool IsPopupOpen { get; private set; }
    public static bool IsPointerOverQuestScroll { get; private set; }

    public static event Action<DisplayUIManager.ProfileState, float> OnProfileChangeRequested;
    public static event Action<int, float, bool> OnSkillCooldownChanged;
    public static event Action<List<GameObject>> OnEnemyListUpdated;
    public static event Action<Vector3, int> OnDamageTextRequested;
    public static event Action<Sprite, Vector3, Vector3> OnMinimapImageChanged;
    public static event Action<Player> OnLocalPlayerSpawned;
    public static event Action OnDeadUiOpenRequested;
    public static event Action OnDeadUiCloseRequested;
    public static event Action OnReviveRequested;
    public static event Action OnQuestProgressChanged;

    public static void EvProfileChangeRequested(DisplayUIManager.ProfileState state, float duration)
    {
        OnProfileChangeRequested?.Invoke(state, duration);
    }

    public static void EvSkillCooldownChanged(int skillIndex, float ratio, bool isReady)
    {
        OnSkillCooldownChanged?.Invoke(skillIndex, ratio, isReady);
    }

    public static void EvEnemyListUpdated(List<GameObject> enemies)
    {
        OnEnemyListUpdated?.Invoke(enemies);
    }

    public static void EvDamageTextRequested(Vector3 worldPos, int damage)
    {
        OnDamageTextRequested?.Invoke(worldPos, damage);
    }

    public static void EvMinimapImageChanged(Sprite minimapSprite, Vector3 minimapPosition, Vector3 minimapRotation)
    {
        OnMinimapImageChanged?.Invoke(minimapSprite, minimapPosition, minimapRotation);
    }

    public static void EvLocalPlayerSpawned(Player player)
    {
        OnLocalPlayerSpawned?.Invoke(player);
    }

    public static void EvDeadUiOpenRequested()
    {
        OnDeadUiOpenRequested?.Invoke();
    }

    public static void EvDeadUiCloseRequested()
    {
        OnDeadUiCloseRequested?.Invoke();
    }

    public static void EvReviveRequested()
    {
        OnReviveRequested?.Invoke();
    }

    public static void EvQuestProgressChanged()
    {
        OnQuestProgressChanged?.Invoke();
    }

    public static void SetPopupOpenState(bool isOpen)
    {
        IsPopupOpen = isOpen;
    }

    public static void SetPointerOverQuestScroll(bool isPointerOver)
    {
        IsPointerOverQuestScroll = isPointerOver;
    }
}

