using UnityEngine;
using EverWind.Gameplay;

public sealed class UIEffectPresenter : MonoBehaviour
{
    public Camera UICamera;
    public float EffectDistance = 9.5f;
    void OnEnable() { PlayEvents.UIEffectRequested += Play; }
    void OnDisable() { PlayEvents.UIEffectRequested -= Play; }
    void Play(UIEffectRequest request)
    {
        if (UICamera == null || request.Prefab == null) return;
        var world = UICamera.ScreenToWorldPoint(new Vector3(request.ScreenPosition.x, request.ScreenPosition.y, EffectDistance));
        var info = new EffectPlayInfo(world, UICamera.transform.rotation)
        {
            OverrideLayer = true, Layer = gameObject.layer, Scale = Vector3.one * Mathf.Max(.001f, request.Scale), UnscaledTime = true
        };
        PlayEvents.EvUIEffectPlay(request.Prefab, info);
    }
}
