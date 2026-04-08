using System;

public class InteractStep : ITutorialStep
{
    private Action<int, int> _deleteAction;

    public void EnterStep(TutorialGuide step, TextRenderManager textRenderManager)
    {
        _deleteAction += (_, _) => ClearEvent(step, textRenderManager);
        PlayEvents.OnInteractionCompleted += _deleteAction;

        textRenderManager.StartShow("InteractT");
        textRenderManager.AutoShow(0, 2);
    }

    public void UpdateStep(TutorialGuide step, TextRenderManager textRenderManager, InputManager inputManager)
    {
        if (_deleteAction != null) return;

        if (!textRenderManager.IsDoneShowingText()) return;

        if (inputManager.AnyKeyDownExcludeMouse())
        {
            step.TransitionStep(TutorialStep.Craft);
        }
    }

    public void ExitStep(TutorialGuide step)
    {
        if (_deleteAction != null)
            PlayEvents.OnInteractionCompleted -= _deleteAction;

        _deleteAction = null;
    }

    public void ClearEvent(TutorialGuide step, TextRenderManager textRenderManager)
    {
        textRenderManager.AutoShow(3, 6);

        if (_deleteAction != null)
            PlayEvents.OnInteractionCompleted -= _deleteAction;

        _deleteAction = null;
    }
}


