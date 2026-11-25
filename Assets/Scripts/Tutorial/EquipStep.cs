public class EquipStep : ITutorialStep
{
    private bool cleared;
    private bool requested;

    public void EnterStep(TutorialGuide step)
    {
        cleared = false;
        requested = false;
    }

    public void UpdateStep(TutorialGuide step, InputManager inputManager)
    {
        if (!cleared || requested) return;
        requested = true;
        step.CompleteTutorial();
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
