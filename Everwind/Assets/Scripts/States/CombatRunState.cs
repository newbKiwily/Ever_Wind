using UnityEngine;
using System;

public class CombatRunState : IState
{
    private Action _onArriveAction;

    public void EnterState(PlayerStateContexter controller)
    {
        controller.GetAnimationContexter().PlayMove(false);
        controller.player.GetCombatManager().ResetDamageCombo();

        Transform target = controller.CurrentTarget;

        if (target != null)
        {
            controller.player.StartMoveto(target);

            _onArriveAction = () => OnArrive(controller);
            controller.player.EvArrive += _onArriveAction;
        }
        else
        {
            controller.player.StopMoveto();
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
        if (_onArriveAction != null)
        {
            controller.player.EvArrive -= _onArriveAction;
            _onArriveAction = null;
        }
    }

    private void OnArrive(PlayerStateContexter controller)
    {
        controller.TransitionState(States.Attack);
    }
}

