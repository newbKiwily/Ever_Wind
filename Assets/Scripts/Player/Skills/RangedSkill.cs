namespace EverWind.Gameplay
{
public enum ROSpawnPoint { Caster = 0, Target = 1 }
public sealed class RangedSkill : SkillDefinition
{
    public ROSpawnPoint SpawnPoint = ROSpawnPoint.Target;
    public UnityEngine.Vector3 Offset;
    public float SpawnTime, ServerRadius = 3;
    public UnityEngine.GameObject ROModel;
    public UnityEngine.Vector3 ROModelOffset, ROModelRotation;
    public SkillRangeDetector.Shape[] DetectionShapes = { new SkillRangeDetector.Shape { Radius = 2 } };
    public override bool NeedsTarget => SpawnPoint != ROSpawnPoint.Caster;
    void OnEnable() { Kind = SkillKind.Ranged; NormalizeSpawnPoint(); }
    void OnValidate() { NormalizeSpawnPoint(); }
    void NormalizeSpawnPoint()
    {
        if (SpawnPoint != ROSpawnPoint.Caster && SpawnPoint != ROSpawnPoint.Target) SpawnPoint = ROSpawnPoint.Target;
        RequiresTarget = NeedsTarget;
    }
    public override UnityEngine.Vector3 Center(UnityEngine.Transform caster, UnityEngine.Transform target)
    {
        var origin = NeedsTarget && target != null ? target.position : caster.position;
        return origin + caster.TransformDirection(Offset);
    }
}
}
