using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CombatManager : MonoBehaviour
{
    private Player _player;
    private InputManager _inputManager;
    public Skill CurrentCastingSkill;
    public int CurrentCastingSkillIndex;
    public Skill[] Skills = new Skill[5];
    private int _bufferedSkillIndex = -1;

    private KeyCode _prevKeyCode;
    private KeyCode _currKeyCode;

    private GameObject _prevTargetEnemy;
    public GameObject TargetEnemy;
    public float TargetRadius;
    public int DamagedCount;
    public bool IgnoreNextDamageAnimation { get; private set; }

    private List<GameObject> _inRadiusEnemy;

    private float[] _skillCooldownRemain = new float[5];
    private float[] _skillCooldownMax = new float[5];
    public GameObject TargetingUI;

    public float GetSkillCooldownRatio(int index)
    {
        if (index < 0 || index >= _skillCooldownMax.Length) return 0f;

        float max = _skillCooldownMax[index];
        float remain = _skillCooldownRemain[index];

        if (max <= 0f) return 1f;

        return Mathf.Clamp01((max - remain) / max);
    }

    public bool IsSkillReady(int index)
    {
        if (index < 0 || index >= _skillCooldownRemain.Length) return false;

        return _skillCooldownRemain[index] <= 0;
    }

    public void Init(Player player, InputManager inputManager)
    {
        _player = player;
        _inputManager = inputManager;
        _currKeyCode = KeyCode.None;
        TargetEnemy = null;
        _prevTargetEnemy = null;
        TargetRadius = 50.0f;
        ResetDamageCombo();
        _inRadiusEnemy = new List<GameObject>();
        player.EvArrive += OnTargetArrived;
        InitSkillCooldownMax();
        TargetingUI = GameObject.Find("TargetingUI");
        TargetingUI.SetActive(false);
    }

    void Update()
    {
        for (int i = 0; i < _skillCooldownRemain.Length; i++)
        {
            if (_skillCooldownRemain[i] > 0)
            {
                _skillCooldownRemain[i] = Mathf.Max(0f, _skillCooldownRemain[i] - Time.deltaTime);
                NotifySkillCooldownChanged(i);
            }
        }

        UpdateTargetingUI();
    }

    private void UpdateTargetingUI()
    {
        if (TargetEnemy == null)
        {
            if (TargetingUI.activeSelf) TargetingUI.SetActive(false);
            return;
        }
        if (!TargetingUI.activeSelf) TargetingUI.SetActive(true);

        Vector3 offset = new Vector3(0, 4.2f, 0);
        TargetingUI.transform.position = TargetEnemy.transform.position + offset;

        TargetingUI.transform.LookAt(TargetingUI.transform.position + Camera.main.transform.rotation * Vector3.forward,Camera.main.transform.rotation * Vector3.up);
    }

    public void KnockBack()
    {
        var tempStateContexter = _player.GetPlayerStateContexter();
        var curr = tempStateContexter.GetCurrState();

        if (curr is AttackState)
        {
            ResetDamageCombo(true);
            AttackEnd();
            return;
        }

        if (IgnoreNextDamageAnimation)
        {
            ConsumeIgnoredDamageHit();
            return;
        }

        DamagedCount = Mathf.Clamp(DamagedCount + 1, 1, 3);
        tempStateContexter.TransitionState(States.Damaged);
    }

    public bool IsAttackKeyDown()
    {
        var tempStateContexter = _player.GetPlayerStateContexter();
        var curr = tempStateContexter.GetCurrState();

        if (curr is AttackState)
        {
            return TryBufferSkillInput();
        }

        if (curr is DamagedState)
            return true;

        int keyPressed = _inputManager.GetAttackKeyDown();
        if (keyPressed == 0) return false;

        return ExecuteSkill(keyPressed - 1);
    }

    public bool TryBufferSkillInput()
    {
        int keyPressed = _inputManager.GetAttackKeyDown();
        if (keyPressed == 0)
            return false;

        int skillIndex = keyPressed - 1;
        if (skillIndex < 0 || skillIndex >= Skills.Length || Skills[skillIndex] == null)
            return false;

        _bufferedSkillIndex = skillIndex;
        return true;
    }

    public bool ExecuteSkill(int skillIndex)
    {
        if (skillIndex < 0 || skillIndex >= Skills.Length || Skills[skillIndex] == null)
            return false;

        if (!IsSkillReady(skillIndex)) return false;

        var tempStateContexter = _player.GetPlayerStateContexter();
        var currState = tempStateContexter.GetCurrState();

        if (currState is AttackState || currState is DamagedState)
        {
            return false;
        }
        _prevKeyCode = _currKeyCode;
        _currKeyCode = (KeyCode)((int)KeyCode.Alpha1 + skillIndex);

        CurrentCastingSkillIndex = skillIndex;
        CurrentCastingSkill = Skills[skillIndex];

        if (TargetEnemy == null)
        {
            TargetEnemy = TargetingEnemy();
        }

        GameObject target = TargetEnemy;

        if (target == null)
        {
            Debug.Log("Skill cast cancelled because there is no target.");
            CurrentCastingSkill = null;
            return false;
        }

        Debug.Log($"Start casting {CurrentCastingSkill.name} toward {target.name}.");
        tempStateContexter.TransitionState(States.CombatRun, target.transform);

        return true;
    }

    public void Attack()
    {
        if (CurrentCastingSkill == null)
        {
            Debug.LogWarning("CombatManager.Attack was called without a current casting skill.");
            return;
        }

        if (CurrentCastingSkillIndex < 0 || CurrentCastingSkillIndex >= Skills.Length)
        {
            Debug.LogWarning($"CombatManager.Attack received an invalid skill index: {CurrentCastingSkillIndex}");
            return;
        }

        Skill castingSkill = CurrentCastingSkill;

        _skillCooldownMax[CurrentCastingSkillIndex] = castingSkill.CoolTime;
        _skillCooldownRemain[CurrentCastingSkillIndex] = castingSkill.CoolTime;
        NotifySkillCooldownChanged(CurrentCastingSkillIndex);
        castingSkill.Cast(this.transform);
    }

    public void BroadcastSkillCooldownStates()
    {
        for (int i = 0; i < Skills.Length; i++)
        {
            NotifySkillCooldownChanged(i);
        }
    }

    public void AttackEnd()
    {
        _prevKeyCode = KeyCode.None;
        _currKeyCode = KeyCode.None;

        if (_bufferedSkillIndex >= 0)
        {
            int bufferedSkillIndex = _bufferedSkillIndex;
            _bufferedSkillIndex = -1;

            if (ExecuteBufferedSkill(bufferedSkillIndex))
                return;
        }

        _player.GetPlayerStateContexter().TransitionState(States.CombatIdle);
        CurrentCastingSkill = null;
    }

    public void EndDamaged()
    {
        DamagedState.IsFinished = true;
        ResetDamageCombo(true);
    }

    public void ResetDamageCombo(bool ignoreNextDamageAnimation = false)
    {
        DamagedCount = 0;
        IgnoreNextDamageAnimation = ignoreNextDamageAnimation;
    }

    public void ClearBufferedSkill()
    {
        _bufferedSkillIndex = -1;
    }

    public void ResetCombatStateForMapChange()
    {
        _prevKeyCode = KeyCode.None;
        _currKeyCode = KeyCode.None;
        _bufferedSkillIndex = -1;
        CurrentCastingSkill = null;
        CurrentCastingSkillIndex = -1;
        _prevTargetEnemy = null;
        TargetEnemy = null;
        _inRadiusEnemy?.Clear();
        ResetDamageCombo();
        _player.StopMoveto();

        if (TargetingUI != null && TargetingUI.activeSelf)
        {
            TargetingUI.SetActive(false);
        }

        var stateContexter = _player.GetPlayerStateContexter();
        if (stateContexter != null)
        {
            stateContexter.TransitionState(States.Idle);
        }
    }

    private bool ExecuteBufferedSkill(int skillIndex)
    {
        if (skillIndex < 0 || skillIndex >= Skills.Length || Skills[skillIndex] == null)
            return false;

        if (!IsSkillReady(skillIndex))
            return false;

        CurrentCastingSkillIndex = skillIndex;
        CurrentCastingSkill = Skills[skillIndex];
        _currKeyCode = (KeyCode)((int)KeyCode.Alpha1 + skillIndex);

        if (TargetEnemy == null)
        {
            TargetEnemy = TargetingEnemy();
        }

        GameObject target = TargetEnemy;
        if (target == null)
        {
            CurrentCastingSkill = null;
            return false;
        }

        _player.GetPlayerStateContexter().TransitionState(States.CombatRun, target.transform);
        return true;
    }

    public void ConsumeIgnoredDamageHit()
    {
        IgnoreNextDamageAnimation = false;
        DamagedCount = 0;
    }

    public GameObject TargetingEnemy()
    {
        Collider[] colliders = Physics.OverlapSphere(this.transform.position, TargetRadius);

        if (colliders.Length <= 0)
            return null;

        float minDistance = float.MaxValue;
        GameObject closetObj = null;

        Vector3 currentPosition = this.transform.position;
        _inRadiusEnemy.Clear();

        foreach (var it in colliders)
        {
            var temp = it.gameObject.GetComponent<IDamageable>();
            if (temp == null || it.CompareTag("Player"))
                continue;

            _inRadiusEnemy.Add(it.gameObject);

            float distance = (it.transform.position - currentPosition).sqrMagnitude;

            if (distance < minDistance)
            {
                closetObj = it.gameObject;
                minDistance = distance;
            }
        }

        UIEvents.EvEnemyListUpdated(_inRadiusEnemy);

        if (_inRadiusEnemy.Count > 0)
        {
            _inRadiusEnemy.Sort((a, b) =>
            {
                float distSqrA = (a.transform.position - currentPosition).sqrMagnitude;
                float distSqrB = (b.transform.position - currentPosition).sqrMagnitude;
                return distSqrA.CompareTo(distSqrB);
            });

            closetObj = _inRadiusEnemy[0];
        }
        else
        {
            return null;
        }

        _prevTargetEnemy = closetObj;
        TargetEnemy = closetObj;
        _player.LookAtTarget(TargetEnemy.transform);
        return closetObj;
    }

    public GameObject ChangeTargetEnemy()
    {
        if (TargetEnemy == null)
            return null;

        Collider[] colliders = Physics.OverlapSphere(this.transform.position, TargetRadius);

        if (colliders.Length <= 0)
            return null;

        int currentIndex = _inRadiusEnemy.IndexOf(TargetEnemy);
        int nextIndex = currentIndex + 1;

        if (nextIndex >= _inRadiusEnemy.Count)
        {
            nextIndex = 0;
        }

        GameObject nextTarget = _inRadiusEnemy[nextIndex];

        _prevTargetEnemy = TargetEnemy;
        TargetEnemy = nextTarget;
        _player.LookAtTarget(TargetEnemy.transform);
        return nextTarget;
    }

    public void OnTargetArrived()
    {
        var tempStateContexter = _player.GetPlayerStateContexter();
        if (CurrentCastingSkill != null)
        {
            tempStateContexter.TransitionState(States.Attack);
            Debug.Log($"Arrived at target. Start attack animation {CurrentCastingSkill.SkillAnimationName}.");
        }
        else
        {
            tempStateContexter.TransitionState(States.CombatIdle);
            Debug.Log("Arrived at target without a registered skill. Return to CombatIdle.");
        }
    }

    private void InitSkillCooldownMax()
    {
        for (int i = 0; i < Skills.Length; i++)
        {
            if (Skills[i] != null)
                _skillCooldownMax[i] = Skills[i].CoolTime;
            else
                _skillCooldownMax[i] = 0f;

            _skillCooldownRemain[i] = 0f;
        }
    }

    private void NotifySkillCooldownChanged(int index)
    {
        if (index < 0 || index >= Skills.Length)
            return;

        UIEvents.EvSkillCooldownChanged(index, GetSkillCooldownRatio(index), IsSkillReady(index));
    }
}

