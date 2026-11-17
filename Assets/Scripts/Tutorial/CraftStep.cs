using System;

public class CraftStep : ITutorialStep
{
    private Action<int, int> _deleteAction;

    public void EnterStep(TutorialGuide step)
    {
        _deleteAction += (_, _) => ClearEvent(step);
        PlayEvents.OnCraftCompleted += _deleteAction;

    }

    public void UpdateStep(TutorialGuide step, InputManager inputManager)
    {
        if (_deleteAction != null)
            return;


        if (inputManager.AnyKeyDownExcludeMouse())
        {
            step.TransitionStep(TutorialStep.Equip);
        }
    }

    public void ExitStep(TutorialGuide step)
    {
        if (_deleteAction != null)
            PlayEvents.OnCraftCompleted -= _deleteAction;

        _deleteAction = null;
    }

    public void ClearEvent(TutorialGuide step)
    {
        if (_deleteAction != null)
            PlayEvents.OnCraftCompleted -= _deleteAction;

        _deleteAction = null;
    }
}


