using System;

public class CameraStep : ITutorialStep
{
    private Action _deleteAction;
    private InputManager InputManager => SingletonManager.Instance.GetSingleton<InputManager>();

    public void EnterStep(TutorialGuide step, TextRenderManager textRenderManager)
    {
        _deleteAction += () => ClearEvent(step, textRenderManager);
        CameraMoving.OnCameraTClear += _deleteAction;

        textRenderManager.StartShow("CameraT");
        textRenderManager.AutoShow(0, 1);

    }
    public void UpdateStep(TutorialGuide step, TextRenderManager textRenderManager, InputManager inputManager)
    {
        if (_deleteAction != null) return;

        if (!textRenderManager.IsDoneShowingText()) return;

        bool keyboardInput = inputManager.AnyKeyDownExcludeMouse();

        if (keyboardInput)
        {
            step.TransitionStep(TutorialStep.Move);
        }
    }

    public void ExitStep(TutorialGuide step)
    {
        if (_deleteAction != null)
            CameraMoving.OnCameraTClear -= _deleteAction;
        _deleteAction = null;
    }

    public void ClearEvent(TutorialGuide step, TextRenderManager textRenderManager)
    {
        textRenderManager.AutoShow(2, 3);
        if (_deleteAction != null)
            CameraMoving.OnCameraTClear -= _deleteAction;
        _deleteAction = null;

    }
}