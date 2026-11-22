using UnityEngine;

namespace EverWind.Gameplay
{
public sealed class AbsorbEffect : Effect
{
    [SerializeField, Min(0)] float scatterDuration = .3f;
    [SerializeField, Min(0)] float acceleration = 20;
    [SerializeField, Min(.01f)] float maxSpeed = 12;
    [SerializeField, Min(.001f)] float arrivalRadius = .15f;

    Transform target;
    ParticleSystem.Particle[][] particles;

    protected override void OnPlay()
    {
        target = PlayInfo.AttractTarget;
        if (particles == null || particles.Length != Systems.Length)
            particles = new ParticleSystem.Particle[Systems.Length][];
    }

    protected override void OnTick(float deltaTime)
    {
        if (target == null || !target.gameObject.activeInHierarchy)
        {
            Finish();
            return;
        }
        if (!EmissionStopped && Elapsed < scatterDuration) return;
        StopEmission();
        Vector3 destination = target.TransformPoint(PlayInfo.TargetLocalOffset);
        for (int i = 0; i < Systems.Length; i++)
        {
            var system = Systems[i];
            if (system == null) continue;
            int capacity = system.main.maxParticles;
            if (particles[i] == null || particles[i].Length < capacity)
                particles[i] = new ParticleSystem.Particle[capacity];
            var buffer = particles[i];
            int count = system.GetParticles(buffer);
            var main = system.main;
            Transform space = main.simulationSpace == ParticleSystemSimulationSpace.World ? null
                : main.simulationSpace == ParticleSystemSimulationSpace.Custom ? main.customSimulationSpace : system.transform;
            for (int j = 0; j < count; j++)
            {
                Vector3 position = space != null ? space.TransformPoint(buffer[j].position) : buffer[j].position;
                Vector3 velocity = space != null ? space.TransformVector(buffer[j].velocity) : buffer[j].velocity;
                Vector3 delta = destination - position;
                velocity = Vector3.ClampMagnitude(velocity + delta.normalized * acceleration * deltaTime, maxSpeed);
                if (delta.magnitude <= Mathf.Max(arrivalRadius, Vector3.Dot(velocity, delta.normalized) * deltaTime))
                {
                    buffer[j].remainingLifetime = 0;
                    continue;
                }
                buffer[j].velocity = space != null ? space.InverseTransformVector(velocity) : velocity;
            }
            system.SetParticles(buffer, count);
        }
    }

    protected override void OnReset() { target = null; }
}
}
