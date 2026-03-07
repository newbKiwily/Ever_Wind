using AYellowpaper.SerializedCollections;
using System.Collections.Generic;
using UnityEngine;

public class EffectManager : SingletonBase<EffectManager>
{
    [SerializeField]
    private SerializedDictionary<string, ParticleSystem> _particleTable = new SerializedDictionary<string, ParticleSystem>();

    private Dictionary<string, List<ParticleSystem>> _particlePool = new Dictionary<string, List<ParticleSystem>>();

    [SerializeField]
    private int _instanceAmount = 3;
    [SerializeField]
    private int _maxAmount;

    protected override void Awake()
    {
        Priority = 7;
        base.Awake();
    }

    private void Start()
    {
        _maxAmount = _instanceAmount * 5;
        foreach (var particle in _particleTable)
        {
            List<ParticleSystem> particleSystems = new List<ParticleSystem>();

            for (int i = 0; i < _instanceAmount; i++)
            {
                var temp = Instantiate(particle.Value, this.transform);
                temp.gameObject.SetActive(false);
                particleSystems.Add(temp);
            }

            _particlePool.Add(particle.Key, particleSystems);
        }
    }


    public void PlayEffect(string key, Vector3 position)
    {
        if (!_particlePool.ContainsKey(key)) return;

        bool isContaining = _particlePool.TryGetValue(key, out var list);
        if (isContaining == false || list.Count > _maxAmount)
            return;

        var particle = list.Find(p => !p.gameObject.activeSelf);
        if (particle != null)
        {
            particle.transform.position = position;
            particle.gameObject.SetActive(true);
            particle.Play();
        }
        else
        {
            var complement = Instantiate(_particleTable[key], this.transform);
            complement.gameObject.SetActive(false);
            _particlePool[key].Add(complement);
            PlayEffect(key, position);
        }
    }
}