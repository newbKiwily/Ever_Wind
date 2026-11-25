using UnityEngine;

namespace EverWind.Gameplay
{
public struct EffectPlayInfo
{
    public Vector3 Position;
    public Quaternion Rotation;
    public Transform FollowTarget;
    public Transform AttractTarget;
    public Vector3 TargetLocalOffset;
    public bool OverrideLayer, UnscaledTime;
    public int Layer;
    public Vector3 Scale;

    public EffectPlayInfo(Vector3 position, Quaternion rotation)
    {
        Position = position;
        Rotation = rotation;
        FollowTarget = null;
        AttractTarget = null;
        TargetLocalOffset = Vector3.zero;
        OverrideLayer = UnscaledTime = false;
        Layer = 0;
        Scale = Vector3.one;
    }
}

// 풀에 반환된 뒤 같은 오브젝트가 재사용되어도 이전 요청으로 조작할 수 없다.
public readonly struct EffectHandle
{
    readonly EffectManager owner;
    readonly Effect effect;
    readonly ulong playbackId;

    internal EffectHandle(EffectManager owner, Effect effect)
    {
        this.owner = owner;
        this.effect = effect;
        playbackId = effect.PlaybackId;
    }

    public bool IsPlaying => owner != null && effect != null && effect.IsPlaying && effect.Owner == owner && effect.PlaybackId == playbackId;
    public void StopEmission() { if (IsPlaying) effect.StopEmission(); }
    public void Release() { if (IsPlaying) owner.ReleaseEffect(effect); }
}
}
