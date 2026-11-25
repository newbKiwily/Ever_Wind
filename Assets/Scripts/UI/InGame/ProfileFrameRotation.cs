using UnityEngine;

public sealed class ProfileFrameRotation : MonoBehaviour
{
    public float DegreesPerSecond = 30;
    void Update() => transform.Rotate(0, 0, DegreesPerSecond * Time.unscaledDeltaTime, Space.Self);
}
