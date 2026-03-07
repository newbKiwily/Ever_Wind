public class IdleState : IState
{
    public void EnterState(PlayerStateContexter controller)
    {
        controller.GetAnimationContexter().PlayIdle(true);
        controller.player.StopMoveto();
        controller.OffWeapon();
        var networkClient = SingletonManager.Instance.GetSingleton<NetworkClient>();
        var pkt = PacketMethod.BuildCombatStateSync(networkClient.UserDbId, false);
        networkClient.Send(pkt);
    }

    public void UpdateState(PlayerStateContexter controller, InputManager inputManager)
    {
        if (controller.player.GetInputVector().sqrMagnitude > 0)
        {
            controller.TransitionState(States.Move);
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
    }
}