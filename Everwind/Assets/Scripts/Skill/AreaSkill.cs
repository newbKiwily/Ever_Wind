using System.Collections.Generic;
using UnityEngine;

public abstract class AreaSkill : Skill
{
    public GameObject ColliderPrefab;
    protected float DetectTime;

    protected HashSet<GameObject> CollectedTargets;

    public void ReceiveTargets(HashSet<GameObject> targets)
    {
        CollectedTargets = targets;
    }
}