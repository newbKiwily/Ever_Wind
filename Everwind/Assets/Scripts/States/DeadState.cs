public class DeadState : IState
{
    private PlayerStateContexter _tempController;

    public void EnterState(PlayerStateContexter controller)
    {
        _tempController = controller;

        SingletonManager.Instance.GetSingleton<InputManager>().LockMoveAndAttack();

        controller.GetAnimationContexter().PlayDead();

        UIEvents.OnReviveRequested += HandleRevive;
        UIEvents.RaiseDeadUiOpenRequested();

        NetworkClient networkClient = SingletonManager.Instance.GetSingleton<NetworkClient>();
        var pkt = PacketMethod.BuildDeadSyncReq(networkClient.UserDbId, true);
        networkClient.Send(pkt);
    }

    private void HandleRevive()
    {
        _tempController.TransitionState(States.CombatIdle);
        _tempController.player.GetPlayerStatManager().SetHp(_tempController.player.GetPlayerStatManager().GetMaxHp());
    }

    public void UpdateState(PlayerStateContexter controller, InputManager inputManager) { }

    public void ExitState(PlayerStateContexter controller)
    {
        UIEvents.OnReviveRequested -= HandleRevive;
        UIEvents.RaiseDeadUiCloseRequested();
        SingletonManager.Instance.GetSingleton<InputManager>().UnlockMoveAndAttack();

        // ExitDead (PascalCase) 적용
        controller.GetAnimationContexter().ExitDead();

        NetworkClient networkClient = SingletonManager.Instance.GetSingleton<NetworkClient>();
        var pkt = PacketMethod.BuildDeadSyncReq(networkClient.UserDbId, false);
        networkClient.Send(pkt);
    }
}
