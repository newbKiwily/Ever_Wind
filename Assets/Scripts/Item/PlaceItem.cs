using UnityEngine;

[CreateAssetMenu]
public class PlaceItem : ConsumeItem
{
    public GameObject Placement;

    public override void Use()
    {
        EverWind.Gameplay.GameplayIntegration.Active?.Use(this);
    }
}
