using System;
using System.Collections.Generic;
using UnityEngine;

public static class UIEvents
{
    public static bool IsPopupOpen { get; private set; }

    public static event Action<DisplayUIManager.ProfileState, float> OnProfileChangeRequested;
    public static event Action<int, float, bool> OnSkillCooldownChanged;
    public static event Action<List<GameObject>> OnEnemyListUpdated;
    public static event Action<Vector3, int> OnDamageTextRequested;
    public static event Action OnDeadUiOpenRequested;
    public static event Action OnDeadUiCloseRequested;
    public static event Action OnReviveRequested;

    public static void RaiseProfileChangeRequested(DisplayUIManager.ProfileState state, float duration)
    {
        OnProfileChangeRequested?.Invoke(state, duration);
    }

    public static void RaiseSkillCooldownChanged(int skillIndex, float ratio, bool isReady)
    {
        OnSkillCooldownChanged?.Invoke(skillIndex, ratio, isReady);
    }

    public static void RaiseEnemyListUpdated(List<GameObject> enemies)
    {
        OnEnemyListUpdated?.Invoke(enemies);
    }

    public static void RaiseDamageTextRequested(Vector3 worldPos, int damage)
    {
        OnDamageTextRequested?.Invoke(worldPos, damage);
    }

    public static void RaiseDeadUiOpenRequested()
    {
        OnDeadUiOpenRequested?.Invoke();
    }

    public static void RaiseDeadUiCloseRequested()
    {
        OnDeadUiCloseRequested?.Invoke();
    }

    public static void RaiseReviveRequested()
    {
        OnReviveRequested?.Invoke();
    }

    public static void SetPopupOpenState(bool isOpen)
    {
        IsPopupOpen = isOpen;
    }
}
