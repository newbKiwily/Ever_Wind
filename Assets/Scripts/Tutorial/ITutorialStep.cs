using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ITutorialStep
{
    public void EnterStep(TutorialGuide step, TextRenderManager textRenderManager);
    public void UpdateStep(TutorialGuide step, TextRenderManager textRenderManager, InputManager inputManager);

    public void ExitStep(TutorialGuide step);

    public void ClearEvent(TutorialGuide step, TextRenderManager textRenderManager);
}
