
public interface IState
{
    void EnterState(PlayerStateContexter contexter);
    void UpdateState(PlayerStateContexter contexter, InputManager inputManager);
    void ExitState(PlayerStateContexter contexter);
}
