using UnityEngine;
using System;

public class CombatRunState : IState
{

    private Action _onArriveAction;

    public void EnterState(PlayerStateContexter controller)
    {

        controller.GetAnimationContexter().PlayMove(false);
        controller.player.GetCombatManager().DamagedCount = 0;
        controller.player.StopMoveto();
    }

    public void EnterState(PlayerStateContexter controller, Transform target)
    {
        if (target != null)
        {
            controller.GetAnimationContexter().PlayMove(false);
            controller.player.StartMoveto(target);

            _onArriveAction = () => OnArrive(controller);
            controller.player.EvArrive += _onArriveAction;
        }
        else
        {
            EnterState(controller);
        }
    }

    public void UpdateState(PlayerStateContexter controller, InputManager inputManager)
    {
        if (inputManager.GetChangeTargetDown())
        {
            GameObject newTarget = controller.player.GetCombatManager().ChangeTargetEnemy();
            if (newTarget != null)
            {
                controller.TransitionState(States.CombatRun, newTarget.transform);
                return;
            }
            else
            {
                controller.TransitionState(States.CombatIdle);
                return;
            }
        }

        if (controller.player.GetInputVector().sqrMagnitude == 0)
        {
            controller.TransitionState(States.CombatIdle);
            return;
        }

        if (controller.player.GetCombatManager().IsAttackKeyDown())
        {
            return;
        }
    }

    public void ExitState(PlayerStateContexter controller)
    {
        // 이벤트 해제 로직이 필요할 경우 여기서 _onArriveAction을 활용할 수 있습니다.
    }

    private void OnArrive(PlayerStateContexter controller)
    {
        controller.TransitionState(States.Attack);
    }
}