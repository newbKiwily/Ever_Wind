using System;

public class InteractStep : ITutorialStep
{
    private Action _deleteAction;

    public void EnterStep(TutorialGuide step, TextRenderManager textRenderManager)
    {
        step.obtainObj1.SetActive(true);
        step.obtainObj2.SetActive(true);
        step.obtainObj3.SetActive(true);
        step.ObtainObj4.SetActive(true);

        _deleteAction += () => ClearEvent(step, textRenderManager);
        TutorialEvents.OnInteractionCompleted += _deleteAction;

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
            TutorialEvents.OnInteractionCompleted -= _deleteAction;

        _deleteAction = null;
    }

    public void ClearEvent(TutorialGuide step, TextRenderManager textRenderManager)
    {
        textRenderManager.AutoShow(3, 6);

        if (_deleteAction != null)
            TutorialEvents.OnInteractionCompleted -= _deleteAction;

        _deleteAction = null;
    }
}
