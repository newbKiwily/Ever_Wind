using UnityEngine;

public class DamagedState : IState
{
    public static bool IsFinished;

    public void EnterState(PlayerStateContexter controller)
    {
        UIEvents.EvProfileChangeRequested(DisplayUIManager.ProfileState.Hit, 1.0f);
        controller.player.StopMoveto();

        var tempCombatManager = controller.player.GetCombatManager();
        int damagedCount = Mathf.Clamp(tempCombatManager.DamagedCount, 1, 3);

        var animCode = controller.GetAnimationContexter().PlayDamaged(damagedCount);

        NetworkClient networkClient = SingletonManager.Instance.GetSingleton<NetworkClient>();
        var pkt = PacketMethod.BuildOneshotAnimReq(networkClient.UserDbId, (OneshotAnimKey)animCode);
        networkClient.Send(pkt);

        if (damagedCount >= 3)
        {
            tempCombatManager.ResetDamageCombo(true);
        }

        IsFinished = false;
    }

    public void UpdateState(PlayerStateContexter controller, InputManager inputManager)
    {
        if (!IsFinished)
            return;

        if (controller.GetPrevState() is CombatIdleState ||
            controller.GetPrevState() is CombatRunState ||
            controller.GetPrevState() is AttackState ||
            controller.GetPrevState() is DamagedState)
        {
            controller.TransitionState(States.CombatIdle);
        }
        else
        {
            controller.TransitionState(States.Idle);
        }
    }

    public void ExitState(PlayerStateContexter controller)
    {
        controller.GetAnimationContexter().ExitInteract();
    }
}

