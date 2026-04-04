using System;

public class CraftStep : ITutorialStep
{
    private Action _deleteAction;

    public void EnterStep(TutorialGuide step, TextRenderManager textRenderManager)
    {
        _deleteAction += () => ClearEvent(step, textRenderManager);
        TutorialEvents.OnCraftCompleted += _deleteAction;

        textRenderManager.StartShow("CraftT");
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
            step.TransitionStep(TutorialStep.Equip);
        }
    }

    public void ExitStep(TutorialGuide step)
    {
        if (_deleteAction != null)
            TutorialEvents.OnCraftCompleted -= _deleteAction;

        _deleteAction = null;
    }

    public void ClearEvent(TutorialGuide step, TextRenderManager textRenderManager)
    {
        textRenderManager.AutoShow(3, 6);

        if (_deleteAction != null)
            TutorialEvents.OnCraftCompleted -= _deleteAction;

        _deleteAction = null;
    }
}
