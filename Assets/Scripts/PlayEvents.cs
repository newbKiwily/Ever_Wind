using System;

public static class PlayEvents
{
    public static event Action<int, int> OnCameraCompleted;
    public static event Action<int, int> OnMoveCompleted;
    public static event Action<int, int> OnCombatEnemyKilled;
    public static event Action<int, int> OnInteractionCompleted;
    public static event Action<int, int> OnGatherCompleted;
    public static event Action<int, int> OnCraftCompleted;
    public static event Action<int, int> OnEquipCompleted;

    public static void EvCameraCompleted(int targetId = 0, int amount = 1)
    {
        OnCameraCompleted?.Invoke(targetId, amount);
    }

    public static void EvMoveCompleted(int targetId = 0, int amount = 1)
    {
        OnMoveCompleted?.Invoke(targetId, amount);
    }

    public static void EvCombatEnemyKilled(int targetId, int amount = 1)
    {
        OnCombatEnemyKilled?.Invoke(targetId, amount);
    }

    public static void EvInteractionCompleted(int targetId = 0, int amount = 1)
    {
        OnInteractionCompleted?.Invoke(targetId, amount);
    }

    public static void EvGatherCompleted(int targetId, int amount = 1)
    {
        OnGatherCompleted?.Invoke(targetId, amount);
    }

    public static void EvCraftCompleted(int targetId, int amount = 1)
    {
        OnCraftCompleted?.Invoke(targetId, amount);
    }

    public static void EvEquipCompleted(int targetId = 0, int amount = 1)
    {
        OnEquipCompleted?.Invoke(targetId, amount);
    }
}
