public class AttackState : IState
{
    private Skill _currSkill;

    public void EnterState(PlayerStateContexter controller)
    {
        var tempCombatManager = controller.player.GetCombatManager();
        _currSkill = tempCombatManager.CurrentCastingSkill;
        var currSkillIdx = tempCombatManager.CurrentCastingSkillIndex;

        if (_currSkill == null || !tempCombatManager.IsSkillReady(currSkillIdx))
        {
            controller.TransitionState(States.CombatIdle);
            return;
        }

        if (tempCombatManager.TargetEnemy != null)
        {
            controller.player.LookAtTarget(tempCombatManager.TargetEnemy.transform);
        }

        // 이전 단계에서 수정한 PlayAttack 호출
        controller.GetAnimationContexter().PlayAttack(currSkillIdx + 1);

        NetworkClient networkClient = SingletonManager.Instance.GetSingleton<NetworkClient>();
        var pkt = PacketMethod.BuildOneshotAnimReq(networkClient.UserDbId, (OneshotAnimKey)(currSkillIdx + 1));
        networkClient.Send(pkt);
    }

    public void UpdateState(PlayerStateContexter controller, InputManager inputManager)
    {
        controller.player.GetCombatManager().TryBufferSkillInput();
    }

    public void ExitState(PlayerStateContexter controller)
    {
        // 이전 단계에서 수정한 ExitInteract 호출
        controller.GetAnimationContexter().ExitInteract();
        _currSkill = null;
    }
}
