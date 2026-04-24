using System;

public class EquipStep : ITutorialStep
{
    private Action<int, int> _deleteAction;
    private bool _mapChangeRequested;

    public void EnterStep(TutorialGuide step, TextRenderManager textRenderManager)
    {
        _mapChangeRequested = false;
        _deleteAction += (_, _) => ClearEvent(step, textRenderManager);
        PlayEvents.OnEquipCompleted += _deleteAction;

        textRenderManager.StartShow("EquipT");
        textRenderManager.AutoShow(0, 2);
    }

    public void UpdateStep(TutorialGuide step, TextRenderManager textRenderManager, InputManager inputManager)
    {
        if (_deleteAction != null)
            return;

        if (_mapChangeRequested)
            return;

        if (!textRenderManager.IsDoneShowingText())
            return;

        step.CompleteTutorial();

        var dataCenter = SingletonManager.Instance.GetSingleton<DataCenter>();
        dataCenter.FlushQueue();

        var networkClient = SingletonManager.Instance.GetSingleton<NetworkClient>();
        var pkt = PacketMethod.BuildMapChangeReq(networkClient.UserDbId, 1);
        networkClient.Send(pkt);
        _mapChangeRequested = true;
    }

    public void ExitStep(TutorialGuide step)
    {
        if (_deleteAction != null)
            PlayEvents.OnEquipCompleted -= _deleteAction;

        _deleteAction = null;
        _mapChangeRequested = false;
    }

    public void ClearEvent(TutorialGuide step, TextRenderManager textRenderManager)
    {
        textRenderManager.AutoShow(3, 5);

        if (_deleteAction != null)
            PlayEvents.OnEquipCompleted -= _deleteAction;

        _deleteAction = null;
    }
}


