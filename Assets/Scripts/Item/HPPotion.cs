using UnityEngine;

[CreateAssetMenu]
public class HPPotion : ConsumeItem
{
    public float HealAmount;

    public override void Use()
    {
        EverWind.Gameplay.GameplayIntegration.Active?.Use(this);
    }
}
