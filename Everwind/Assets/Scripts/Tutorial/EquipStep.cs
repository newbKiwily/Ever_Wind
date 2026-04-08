using System;

public class EquipStep : ITutorialStep
{
    private Action<int, int> _deleteAction;

    public void EnterStep(TutorialGuide step, TextRenderManager textRenderManager)
    {
        _deleteAction += (_, _) => ClearEvent(step, textRenderManager);
        PlayEvents.OnEquipCompleted += _deleteAction;

        textRenderManager.StartShow("EquipT");
        textRenderManager.AutoShow(0, 2);
    }

    public void UpdateStep(TutorialGuide step, TextRenderManager textRenderManager, InputManager inputManager)
    {
        if (_deleteAction != null)
            return;

        if (!textRenderManager.IsDoneShowingText())
            return;

        if (inputManager.AnyKeyDownExcludeMouse())
        {
        }
    }

    public void ExitStep(TutorialGuide step)
    {
        if (_deleteAction != null)
            PlayEvents.OnEquipCompleted -= _deleteAction;

        _deleteAction = null;
    }

    public void ClearEvent(TutorialGuide step, TextRenderManager textRenderManager)
    {
        textRenderManager.AutoShow(3, 5);

        if (_deleteAction != null)
            PlayEvents.OnEquipCompleted -= _deleteAction;

        _deleteAction = null;
    }
}


