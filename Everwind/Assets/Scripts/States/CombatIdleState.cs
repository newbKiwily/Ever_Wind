using UnityEngine;
public class CombatIdleState : IState
{
    // private 필드이므로 _camelCase 적용
    private float _combatIdleTime = 5.0f;

    public void EnterState(PlayerStateContexter controller)
    {
        // PlayIdle 호출 (PascalCase)
        controller.GetAnimationContexter().PlayIdle(false);

        controller.OnWeapon(); // 만약 onWeapon도 수정 대상이라면 OnWeapon으로 변경 필요
        var networkClient = SingletonManager.Instance.GetSingleton<NetworkClient>();
        var pkt = PacketMethod.BuildCombatStateSync(networkClient.UserDbId, true);
        networkClient.Send(pkt);

        controller.player.StopMoveto();
    }

    public void UpdateState(PlayerStateContexter controller, InputManager inputManager)
    {
        if (controller.player.GetCombatManager().IsAttackKeyDown())
        {
            _combatIdleTime = 5.0f;
            return;
        }

        if (inputManager.GetChangeTargetDown())
        {
            if (controller.player.GetCombatManager().ChangeTargetEnemy() != null)
            {
            }

            _combatIdleTime = 5.0f;
            return;
        }

        if (controller.player.GetInputVector().sqrMagnitude == 0)
        {
            _combatIdleTime -= Time.deltaTime;

            if (_combatIdleTime <= 0)
            {
                controller.TransitionState(States.Idle);
                return;
            }
        }

        if (controller.player.GetInputVector().sqrMagnitude > 0)
        {
            _combatIdleTime = 5.0f;
            controller.TransitionState(States.CombatRun);
            return;
        }
    }

    public void ExitState(PlayerStateContexter controller)
    {
        _combatIdleTime = 5.0f;
    }
}