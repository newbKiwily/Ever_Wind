using UnityEngine;

namespace EverWind.Gameplay
{
public sealed class GameplayTargetIndicator : MonoBehaviour
{
    public PlayerActionController Controller;
    GameObject visual;
    void Start()
    {
        var prefab = Controller.Actor.Registry.TargetIndicatorVisual;
        if (prefab != null)
        {
            visual = Instantiate(prefab, transform);
            visual.SetActive(false);
        }
    }
    void LateUpdate()
    {
        if (visual == null)
            return;
        var target = Controller.SelectedTarget;
        bool show = target != null && target.Kind == EntityKind.Monster && target.Hp > 0;
        visual.SetActive(show);
        if (!show)
            return;
        visual.transform.position = target.transform.position + new Vector3(0, 4.2f, 0);
        if (Camera.main != null)
            visual.transform.LookAt(visual.transform.position +
                                        Camera.main.transform.rotation * Vector3.forward,
                                    Camera.main.transform.rotation * Vector3.up);
    }
}
}
