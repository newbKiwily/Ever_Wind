using UnityEngine;

[CreateAssetMenu]
public class HPPotion : ConsumeItem
{
    public float HealAmount;

    public override void Use()
    {
        var worldLoader = SingletonManager.Instance.GetSingleton<WorldLoader>();
        if (worldLoader == null || worldLoader.InstancedPlayer == null)
            return;

        Player player = worldLoader.InstancedPlayer.GetComponent<Player>();
        if (player == null)
            return;

        PlayerStatManager statManager = player.GetPlayerStatManager();
        if (statManager == null)
            return;

        float currentHp = statManager.GetHp();
        float maxHp = statManager.GetMaxHp();

        if (currentHp >= maxHp)
            return;

        float healedHp = Mathf.Min(currentHp + HealAmount, maxHp);
        statManager.SetHp(healedHp);
        player.RefreshHpUI();
    }
}
