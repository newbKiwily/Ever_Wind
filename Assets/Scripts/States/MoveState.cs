using UnityEngine;
using System;

public class MoveState : IState
{
    private Action _onArriveAction;

    public void EnterState(PlayerStateContexter controller)
    {
        controller.GetAnimationContexter().PlayMove(true);
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
        if (controller.player.GetInputVector().sqrMagnitude == 0)
        {
            controller.TransitionState(States.Idle);
            return;
        }

        if (inputManager.GetEnterCombatDown())
        {
            controller.TransitionState(States.CombatIdle);
            return;
        }

        if (inputManager.GetInteractDown())
        {
            var target = controller.player.DetectObtainable();
            if (target != null)
            {
                controller.TransitionState(States.Move, target.transform);
                return;
            }
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
        controller.TransitionState(States.Interact);
    }
}

