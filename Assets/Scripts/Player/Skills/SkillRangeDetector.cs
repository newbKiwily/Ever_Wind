using System;
using System.Collections.Generic;
using UnityEngine;

namespace EverWind.Gameplay
{
// 수동 설정한 탐지선을 실행한다. 파티클 분석·편집 도구는 별도로 구현한다.
public sealed class SkillRangeDetector : MonoBehaviour
{
    [Serializable]
    public struct DetectionRay
    {
        public Vector3 Origin;
        public Vector3 Direction;
        [Min(0)] public float Length;
        [Min(0)] public float Thickness;
    }

    public DetectionRay[] Rays = Array.Empty<DetectionRay>();
    public LayerMask TargetLayers = ~0;

    public int[] Detect(PlayerServerTransmitter actor)
    {
        var ids = new HashSet<int>();
        foreach (var ray in Rays)
        {
            if (ray.Length <= 0 || ray.Direction.sqrMagnitude < .0001f) continue;
            Vector3 origin = transform.TransformPoint(ray.Origin);
            Vector3 direction = transform.TransformDirection(ray.Direction).normalized;
            var hits = ray.Thickness > 0
                ? Physics.SphereCastAll(origin, ray.Thickness, direction, ray.Length, TargetLayers, QueryTriggerInteraction.Collide)
                : Physics.RaycastAll(origin, direction, ray.Length, TargetLayers, QueryTriggerInteraction.Collide);
            foreach (var hit in hits) Add(hit.collider);
            if (ray.Thickness > 0)
                foreach (var collider in Physics.OverlapSphere(origin, ray.Thickness, TargetLayers, QueryTriggerInteraction.Collide)) Add(collider);
        }
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

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        foreach (var ray in Rays)
            Gizmos.DrawLine(transform.TransformPoint(ray.Origin), transform.TransformPoint(ray.Origin) + transform.TransformDirection(ray.Direction).normalized * ray.Length);
    }
}
}
