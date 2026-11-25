public class CombatStep : ITutorialStep
{
    private bool cleared;

    public void EnterStep(TutorialGuide step) { cleared = false; }
    public void UpdateStep(TutorialGuide step, InputManager inputManager)
    {
        if (cleared && inputManager.AnyKeyDownExcludeMouse()) step.TransitionStep(TutorialStep.Interact);
    }
    public void ExitStep(TutorialGuide step) { cleared = false; }
    public void ClearEvent(TutorialGuide step) { cleared = true; }
}
