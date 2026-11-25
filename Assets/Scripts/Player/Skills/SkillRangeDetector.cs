using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EverWind.Gameplay
{
// RO 구 영역을 검사하고 중복 대상을 제거한다.
public sealed class SkillRangeDetector : MonoBehaviour
{
    [Serializable]
    public struct Shape
    {
        public Vector3 Origin;
        [Min(0)] public float Radius;
    }
    public Shape[] Shapes = Array.Empty<Shape>();
    public LayerMask TargetLayers = ~0;

    public int[] Detect(PlayerServerTransmitter actor)
    {
        var ids = new HashSet<int>();
        foreach (var collider in DetectColliders(gameObject.scene.GetPhysicsScene())) Add(collider);
        var result = new List<int>(ids);
        result.Sort();
        if (result.Count > Protocol.MaxAttackTargets) result.RemoveRange(Protocol.MaxAttackTargets, result.Count - Protocol.MaxAttackTargets);
        return result.ToArray();

        void Add(Collider collider)
        {
            var monster = collider.GetComponentInParent<MonsterServerTransmitter>();
            if (monster != null && monster.Hp > 0 && monster.Registry == actor.Registry && monster.gameObject.activeInHierarchy) ids.Add(monster.EntityId);
        }
    }

    public Collider[] DetectColliders(PhysicsScene physics)
    {
        var colliders = new HashSet<Collider>();
        var overlaps = new Collider[256];
        foreach (var shape in Shapes)
        {
            if (shape.Radius <= 0) continue;
            int count = physics.OverlapSphere(transform.TransformPoint(shape.Origin), shape.Radius, overlaps, TargetLayers, QueryTriggerInteraction.Collide);
            for (int i = 0; i < count; ++i) colliders.Add(overlaps[i]);
        }
        var result = new Collider[colliders.Count]; colliders.CopyTo(result); return result;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        foreach (var shape in Shapes)
            Gizmos.DrawWireSphere(transform.TransformPoint(shape.Origin), shape.Radius);
    }
}
}
