
using UnityEngine;
using System;

public class InteractState : IState
{
    public static Action InteractTClear;
    private Action _onObtainedAction;
    private FieldItem _item;

    public void EnterState(PlayerStateContexter controller)
    {
        _item = controller.player.ClosetItem;

        if (_item != null)
        {
            _onObtainedAction = () => OnObtained(controller);
            _item.EvObtained += _onObtainedAction;

            _item.StartRooting();
            controller.GetAnimationContexter().PlayInteract();
            NetworkClient networkClient = SingletonManager.Instance.GetSingleton<NetworkClient>();
            var pkt = PacketMethod.BuildInteractSyncReq(networkClient.UserDbId, true);
            networkClient.Send(pkt);
            controller.OffWeapon();

            var pkt2 = PacketMethod.BuildCombatStateSync(networkClient.UserDbId, false);
            networkClient.Send(pkt2);
        }
        else
        {
            Debug.Log("InteractState 진입했으나 상호작용 목표물이 없습니다. Idle로 복귀.");
            controller.TransitionState(States.Idle);
        }
    }

    private void OnObtained(PlayerStateContexter controller)
    {
        InteractTClear?.Invoke();

        UIEvents.RaiseProfileChangeRequested(DisplayUIManager.ProfileState.Success, 1.0f);
        controller.GetAnimationContexter().PlayOneshot(OneshotAni.Success);
        NetworkClient networkClient = SingletonManager.Instance.GetSingleton<NetworkClient>();
        var pkt = PacketMethod.BuildOneshotAnimReq(networkClient.UserDbId, OneshotAnimKey.Success);
        networkClient.Send(pkt);
        controller.TransitionState(States.Idle);
    }

    public void UpdateState(PlayerStateContexter controller, InputManager inputManager)
    {
        if (controller.player.GetInputVector().sqrMagnitude > 0)
        {
            controller.TransitionState(States.Move);
            return;
        }

        if (!_item.gameObject.activeSelf)
        {
            controller.TransitionState(States.Move);
            return;
        }
    }

    public void ExitState(PlayerStateContexter controller)
    {
        if (_item != null)
        {
            if (_onObtainedAction != null)
            {
                _item.EvObtained -= _onObtainedAction;
                _onObtainedAction = null;
            }

            if (!_item.IsNullCoroutine())
            {
                _item.StopRooting();
            }
        }

        controller.player.ClosetItem = null;
        controller.GetAnimationContexter().ExitInteract();
        NetworkClient networkClient = SingletonManager.Instance.GetSingleton<NetworkClient>();
        var pkt = PacketMethod.BuildInteractSyncReq(networkClient.UserDbId, false);
        networkClient.Send(pkt);
    }
}
