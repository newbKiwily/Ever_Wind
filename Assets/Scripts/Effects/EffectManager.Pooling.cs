using System.Collections.Generic;
using EverWind.Gameplay;
using UnityEngine;

public partial class EffectManager
{
    sealed class EffectPool
    {
        public readonly List<Effect> Instances = new List<Effect>();
        public readonly Stack<Effect> Available = new Stack<Effect>();
    }

    readonly Dictionary<Effect, EffectPool> effectPools = new Dictionary<Effect, EffectPool>();
    readonly List<Effect> activeEffects = new List<Effect>();
    Transform effectPoolRoot;

    public int ActiveEffectCount => activeEffects.Count;
    public int EffectPoolCount => effectPools.Count;

    // 기존 문자열 API와 독립적이며, 이 API를 호출하기 전에는 새 풀을 생성하지 않는다.
    public EffectHandle PlayEffect(Effect prefab, EffectPlayInfo info)
    {
        if (prefab == null || !isActiveAndEnabled) return default;
        PrewarmEffect(prefab, prefab.InitialPoolSize);
        var pool = effectPools[prefab];
        Effect effect = null;
        while (pool.Available.Count > 0 && effect == null) effect = pool.Available.Pop();
        if (effect == null)
        {
            pool.Instances.RemoveAll(instance => instance == null);
            if (pool.Instances.Count >= prefab.MaxPoolSize) return default;
            effect = CreateEffect(prefab, pool);
        }
        activeEffects.Add(effect);
        try
        {
            effect.Play(info);
            return new EffectHandle(this, effect);
        }
        catch
        {
            ReleaseEffect(effect);
            throw;
        }
    }

    public void PrewarmEffect(Effect prefab, int count)
    {
        if (prefab == null) return;
        if (!effectPools.TryGetValue(prefab, out var pool))
        {
            pool = new EffectPool();
            effectPools.Add(prefab, pool);
        }
        pool.Instances.RemoveAll(instance => instance == null);
        int target = Mathf.Clamp(count, 0, prefab.MaxPoolSize);
        while (pool.Instances.Count < target) pool.Available.Push(CreateEffect(prefab, pool));
    }

    public int GetEffectPoolSize(Effect prefab)
    {
        if (prefab == null || !effectPools.TryGetValue(prefab, out var pool)) return 0;
        pool.Instances.RemoveAll(instance => instance == null);
        return pool.Instances.Count;
    }

    Effect CreateEffect(Effect prefab, EffectPool pool)
    {
        if (effectPoolRoot == null)
        {
            var root = new GameObject("EffectPool");
            root.SetActive(false);
            root.transform.SetParent(transform, false);
            effectPoolRoot = root.transform;
        }
        // 비활성 부모 아래에서 복제해 위치 설정 전 자동 방출을 방지한다.
        var effect = Instantiate(prefab, effectPoolRoot, false);
        effect.gameObject.SetActive(false);
        effect.Initialize(this, prefab);
        pool.Instances.Add(effect);
        return effect;
    }

    void TickEffects()
    {
        for (int i = activeEffects.Count - 1; i >= 0; i--)
        {
            var effect = activeEffects[i];
            if (effect == null)
            {
                activeEffects.RemoveAt(i);
                continue;
            }
            if (effect.gameObject.activeInHierarchy && effect.enabled)
                effect.Tick(effect.UseUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime);
            if (!effect.gameObject.activeInHierarchy || !effect.enabled || !effect.IsPlaying || effect.IsComplete)
                ReleaseEffect(effect);
        }
    }

    internal void ReleaseEffect(Effect effect)
    {
        if (effect == null || effect.Owner != this || !activeEffects.Remove(effect)) return;
        effect.ResetForPool();
        effect.gameObject.SetActive(false);
        effect.transform.SetParent(effectPoolRoot, false);
        effect.transform.localPosition = Vector3.zero;
        effect.transform.localRotation = Quaternion.identity;
        if (effect.Prefab != null && effectPools.TryGetValue(effect.Prefab, out var pool)) pool.Available.Push(effect);
        else Destroy(effect.gameObject);
    }

    public void ReleaseAllEffects()
    {
        for (int i = activeEffects.Count - 1; i >= 0; i--)
        {
            var effect = activeEffects[i];
            if (effect != null) ReleaseEffect(effect);
            else activeEffects.RemoveAt(i);
        }
    }

    public void ClearEffectPools()
    {
        ReleaseAllEffects();
        foreach (var pool in effectPools.Values)
            foreach (var effect in pool.Instances)
                if (effect != null) Destroy(effect.gameObject);
        effectPools.Clear();
        if (effectPoolRoot != null) Destroy(effectPoolRoot.gameObject);
        effectPoolRoot = null;
    }

    void OnDisable() { ReleaseAllEffects(); }

    protected override void OnDestroy()
    {
        ClearEffectPools();
        base.OnDestroy();
    }
}
