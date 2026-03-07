using UnityEngine;
using System;

public class MoveState : IState
{
    private Action _onArriveAction;

    public void EnterState(PlayerStateContexter controller)
    {
        controller.GetAnimationContexter().PlayMove(true);
        controller.player.GetCombatManager().DamagedCount = 0;
        controller.player.StopMoveto();
    }

    public void EnterState(PlayerStateContexter controller, Transform target)
    {
        if (target != null)
        {
            controller.GetAnimationContexter().PlayMove(true);
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
                controller.player.StartMoveto(target.transform);
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