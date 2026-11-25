public class InteractStep : ITutorialStep
{
    private bool cleared;

    public void EnterStep(TutorialGuide step)
    {
        cleared = false;
    }

    public void UpdateStep(TutorialGuide step, InputManager inputManager)
    {
        if (!cleared) return;
        if (inputManager.AnyKeyDownExcludeMouse()) step.TransitionStep(TutorialStep.Craft);
    }

    public void ExitStep(TutorialGuide step)
    {
        cleared = false;
    }

    public void ClearEvent(TutorialGuide step)
    {
        cleared = true;
    }
}
