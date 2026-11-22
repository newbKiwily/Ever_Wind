using UnityEngine;

namespace EverWind.Gameplay
{
public sealed class FollowEffect : Effect
{
    [SerializeField, Min(0)] float followDuration = .2f;
    [SerializeField] bool followRotation = true;

    Transform target;
    Vector3 localPosition;
    Quaternion localRotation;

    protected override void OnPlay()
    {
        target = PlayInfo.FollowTarget;
        if (target == null) return;
        localPosition = target.InverseTransformPoint(transform.position);
        localRotation = Quaternion.Inverse(target.rotation) * transform.rotation;
        // 검을 따라 이동하는 발생기와 달리 이미 방출된 입자는 월드에 남긴다.
        foreach (var system in Systems)
        {
            var main = system.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
        }
    }

    protected override void OnTick(float deltaTime)
    {
        if (EmissionStopped) return;
        if (target == null || !target.gameObject.activeInHierarchy)
        {
            StopEmission();
            return;
        }
        transform.position = target.TransformPoint(localPosition);
        if (followRotation) transform.rotation = target.rotation * localRotation;
        if (Elapsed >= followDuration) StopEmission();
    }

    protected override void OnEmissionStopped() { target = null; }
    protected override void OnReset()
    {
        target = null;
        localPosition = Vector3.zero;
        localRotation = Quaternion.identity;
    }
}
}
