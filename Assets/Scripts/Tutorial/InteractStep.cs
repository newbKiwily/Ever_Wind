using System;

public class InteractStep : ITutorialStep
{
    private Action<int, int> _deleteAction;

    public void EnterStep(TutorialGuide step)
    {
        _deleteAction += (_, _) => ClearEvent(step);
        PlayEvents.OnInteractionCompleted += _deleteAction;

    }

    public void UpdateStep(TutorialGuide step, InputManager inputManager)
    {
        if (_deleteAction != null) return;


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

    public void ClearEvent(TutorialGuide step)
    {
        if (_deleteAction != null)
            PlayEvents.OnInteractionCompleted -= _deleteAction;

        _deleteAction = null;
    }
}


