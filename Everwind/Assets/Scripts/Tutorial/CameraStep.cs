using System;

public class CameraStep : ITutorialStep
{
    private Action<int, int> _deleteAction;

    public void EnterStep(TutorialGuide step, TextRenderManager textRenderManager)
    {
        _deleteAction += (_, _) => ClearEvent(step, textRenderManager);
        PlayEvents.OnCameraCompleted += _deleteAction;

        textRenderManager.StartShow("CameraT");
        textRenderManager.AutoShow(0, 1);
    }

    public void UpdateStep(TutorialGuide step, TextRenderManager textRenderManager, InputManager inputManager)
    {
        if (_deleteAction != null) return;

        if (!textRenderManager.IsDoneShowingText()) return;

        if (inputManager.AnyKeyDownExcludeMouse())
        {
            step.TransitionStep(TutorialStep.Move);
        }
    }

    public void ExitStep(TutorialGuide step)
    {
        if (_deleteAction != null)
            PlayEvents.OnCameraCompleted -= _deleteAction;

        _deleteAction = null;
    }

    public void ClearEvent(TutorialGuide step, TextRenderManager textRenderManager)
    {
        textRenderManager.AutoShow(2, 3);

        if (_deleteAction != null)
            PlayEvents.OnCameraCompleted -= _deleteAction;

        _deleteAction = null;
    }
}


