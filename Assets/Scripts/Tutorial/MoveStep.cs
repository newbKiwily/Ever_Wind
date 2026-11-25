using UnityEngine;

public class MoveStep : ITutorialStep
{
    private bool cleared;
    private GameObject targetBox;

    public void EnterStep(TutorialGuide step)
    {
        cleared = false;
        RemoveTarget();
        if (step.moveT_taretBox != null)
        {
            targetBox = Object.Instantiate(step.moveT_taretBox, step.TargetBoxPosition, Quaternion.identity, step.transform);
            targetBox.SetActive(true);
        }
    }

    public void UpdateStep(TutorialGuide step, InputManager inputManager)
    {
        if (!cleared) return;
        if (inputManager.AnyKeyDownExcludeMouse()) step.TransitionStep(TutorialStep.Combat);
    }

    public void ExitStep(TutorialGuide step)
    {
        RemoveTarget();
        cleared = false;
    }

    public void ClearEvent(TutorialGuide step)
    {
        RemoveTarget();
        cleared = true;
    }

    private void RemoveTarget()
    {
        if (targetBox == null) return;
        targetBox.SetActive(false);
        Object.Destroy(targetBox);
        targetBox = null;
    }
}
