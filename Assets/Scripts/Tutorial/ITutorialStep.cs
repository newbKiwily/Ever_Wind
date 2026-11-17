using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ITutorialStep
{
    public void EnterStep(TutorialGuide step);
    public void UpdateStep(TutorialGuide step, InputManager inputManager);

    public void ExitStep(TutorialGuide step);

    public void ClearEvent(TutorialGuide step);
}
