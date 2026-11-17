using System;

public class EquipStep : ITutorialStep
{
    private Action<int, int> _deleteAction;
    private bool _mapChangeRequested;

    public void EnterStep(TutorialGuide step)
    {
        _mapChangeRequested = false;
        _deleteAction += (_, _) => ClearEvent(step);
        PlayEvents.OnEquipCompleted += _deleteAction;

    }

    public void UpdateStep(TutorialGuide step, InputManager inputManager)
    {
        if (_deleteAction != null)
            return;

        if (_mapChangeRequested)
            return;


        step.CompleteTutorial();

        // 맵 전환은 서버가 튜토리얼 완료를 확정한 뒤 처리한다.
        _mapChangeRequested = true;
    }

    public void ExitStep(TutorialGuide step)
    {
        if (_deleteAction != null)
            PlayEvents.OnEquipCompleted -= _deleteAction;

        _deleteAction = null;
        _mapChangeRequested = false;
    }

    public void ClearEvent(TutorialGuide step)
    {
        if (_deleteAction != null)
            PlayEvents.OnEquipCompleted -= _deleteAction;

        _deleteAction = null;
    }
}


