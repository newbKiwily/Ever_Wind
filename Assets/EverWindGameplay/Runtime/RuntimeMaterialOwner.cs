using UnityEngine;

namespace EverWind.Gameplay
{
public sealed class RuntimeMaterialOwner : MonoBehaviour
{
    public Material Value;
    void OnDestroy()
    {
        if (Value != null)
            Destroy(Value);
    }
}
}
