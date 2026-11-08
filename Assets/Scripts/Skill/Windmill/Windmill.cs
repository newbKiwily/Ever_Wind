using System.Collections;
using UnityEngine;

public class Windmill : AreaSkill
{   
    public override void Cast(Transform attacker)
    {

        Init();
        IsLocked = false;      
        GameObject detectorObj = Instantiate(ColliderPrefab, attacker.position, attacker.rotation);
        AreaTargetDetector detector = detectorObj.GetComponent<AreaTargetDetector>();

        detector.Owner = this;  
        CombatManager cm = attacker.GetComponent<CombatManager>();
        cm.StartCoroutine(DetectionRoutine(detector,attacker));
        SingletonManager.Instance.GetSingleton<EffectManager>().PlayEffect("Skill_windmill", attacker.position);
    }

    protected override void Init()
    {
        SkillAnimationName = "Windmill";
        CoolTime = 2.0f;
        Dmg = 15.0f;
        DetectTime = 0.2f;

    }
    private IEnumerator DetectionRoutine(AreaTargetDetector detector,Transform attacker)
    {
        // 즉발 감지 시간
        yield return new WaitForSeconds(DetectTime);

        
        detector.Finish();

        // 3) 대미지 처리
        if (CollectedTargets != null)
        {
            foreach (var enemy in CollectedTargets)
            {
                DealDamage(attacker, enemy, Dmg);
            }
        }

    }
}

