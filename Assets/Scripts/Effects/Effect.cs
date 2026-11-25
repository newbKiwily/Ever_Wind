using UnityEngine;

namespace EverWind.Gameplay
{
public abstract class Effect : MonoBehaviour
{
    [Header("풀 설정")]
    [SerializeField, Min(0)] int initialPoolSize = 3;
    [SerializeField, Min(1)] int maxPoolSize = 15;
    [Header("실행 설정")]
    [Tooltip("0이면 프리팹의 파티클 방출 설정을 사용합니다.")]
    [SerializeField, Min(0)] float emissionDuration;
    [Tooltip("입자 종료 후에도 남는 Trail 등의 추가 대기 시간입니다. 에셋의 잔상 수명에 맞춥니다.")]
    [SerializeField, Min(0)] float tailDuration;
    [SerializeField, Min(.01f)] float maxLifetime = 10;
    [SerializeField] bool useUnscaledTime;

    protected ParticleSystem[] Systems { get; private set; }
    protected EffectPlayInfo PlayInfo { get; private set; }
    protected float Elapsed { get; private set; }
    protected bool EmissionStopped { get; private set; }
    public int InitialPoolSize => Mathf.Clamp(initialPoolSize, 0, MaxPoolSize);
    public int MaxPoolSize => Mathf.Max(1, maxPoolSize);
    public bool UseUnscaledTime => useUnscaledTime || PlayInfo.UnscaledTime;
    public bool IsPlaying { get; private set; }
    public bool IsComplete { get; private set; }
    public ulong PlaybackId { get; private set; }
    internal EffectManager Owner { get; private set; }
    internal Effect Prefab { get; private set; }

    Vector3 initialScale;
    Transform[] effectTransforms;
    int[] originalLayers;
    bool initialized;
    float emptyDuration;

    internal void Initialize(EffectManager owner, Effect prefab)
    {
        Owner = owner;
        Prefab = prefab;
        if (initialized) return;
        initialized = true;
        initialScale = transform.localScale;
        effectTransforms = GetComponentsInChildren<Transform>(true);
        originalLayers = new int[effectTransforms.Length];
        for (int i = 0; i < originalLayers.Length; ++i) originalLayers[i] = effectTransforms[i].gameObject.layer;
        Systems = GetComponentsInChildren<ParticleSystem>(true);
        foreach (var system in Systems)
        {
            var main = system.main;
            main.playOnAwake = false;
            main.stopAction = ParticleSystemStopAction.None;
            main.useUnscaledTime = useUnscaledTime;
        }
    }

    internal void Play(EffectPlayInfo info)
    {
        ResetForPool();
        ++PlaybackId;
        PlayInfo = info;
        // 활성 효과는 추적 대상이나 Manager의 스케일을 상속하지 않는 월드 루트다.
        transform.SetParent(null, false);
        transform.SetPositionAndRotation(info.Position, info.Rotation.Equals(default(Quaternion)) ? Quaternion.identity : info.Rotation);
        transform.localScale = info.Scale == Vector3.zero ? initialScale : Vector3.Scale(initialScale, info.Scale);
        if (info.OverrideLayer)
            foreach (var child in effectTransforms) if (child != null) child.gameObject.layer = info.Layer;
        foreach (var system in Systems) { var main = system.main; main.useUnscaledTime = UseUnscaledTime; }
        enabled = true;
        IsPlaying = true;
        gameObject.SetActive(true);
        OnPlay();
        foreach (var system in Systems) system.Play(false);
    }

    internal void Tick(float deltaTime)
    {
        if (!IsPlaying || IsComplete) return;
        Elapsed += deltaTime;
        OnTick(deltaTime);
        if (emissionDuration > 0 && Elapsed >= emissionDuration) StopEmission();
        if (Elapsed >= Mathf.Max(.01f, maxLifetime)) Finish();
        if (IsComplete) return;
        foreach (var system in Systems)
            if (system != null && system.IsAlive(false))
            {
                emptyDuration = 0;
                return;
            }
        emptyDuration += deltaTime;
        if (emptyDuration >= tailDuration) Finish();
    }

    public void StopEmission()
    {
        if (!IsPlaying || EmissionStopped) return;
        EmissionStopped = true;
        foreach (var system in Systems)
            if (system != null) system.Stop(false, ParticleSystemStopBehavior.StopEmitting);
        OnEmissionStopped();
    }

    public void ResetForPool()
    {
        if (effectTransforms != null)
            for (int i = 0; i < effectTransforms.Length; ++i) if (effectTransforms[i] != null) effectTransforms[i].gameObject.layer = originalLayers[i];
        if (Systems != null)
            foreach (var system in Systems)
                if (system != null) system.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
        OnReset();
        PlayInfo = default;
        Elapsed = 0;
        emptyDuration = 0;
        EmissionStopped = false;
        IsPlaying = false;
        IsComplete = false;
    }

    protected void Finish() { IsComplete = true; }
    protected virtual void OnPlay() { }
    protected virtual void OnTick(float deltaTime) { }
    protected virtual void OnEmissionStopped() { }
    protected virtual void OnReset() { }
}
}
