using System;

public static class TutorialEvents
{
    public static event Action OnCameraCompleted;
    public static event Action OnMoveCompleted;
    public static event Action OnCombatEnemyKilled;
    public static event Action OnInteractionCompleted;
    public static event Action OnCraftCompleted;
    public static event Action OnEquipCompleted;

    public static void RaiseCameraCompleted()
    {
        OnCameraCompleted?.Invoke();
    }

    public static void RaiseMoveCompleted()
    {
        OnMoveCompleted?.Invoke();
    }

    public static void RaiseCombatEnemyKilled()
    {
        OnCombatEnemyKilled?.Invoke();
    }

    public static void RaiseInteractionCompleted()
    {
        OnInteractionCompleted?.Invoke();
    }

    public static void RaiseCraftCompleted()
    {
        OnCraftCompleted?.Invoke();
    }

    public static void RaiseEquipCompleted()
    {
        OnEquipCompleted?.Invoke();
    }
}
