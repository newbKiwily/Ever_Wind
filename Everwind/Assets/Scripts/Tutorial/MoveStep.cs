using System;
using UnityEngine;

public class MoveStep : ITutorialStep
{
    private Action<int, int> _deleteAction;
    private GameObject _targetBox;
    public void EnterStep(TutorialGuide step, TextRenderManager textRenderManager)
    {
        _targetBox=GameObject.Instantiate(step.moveT_taretBox, step.TargetBoxPosition, Quaternion.identity);

        _deleteAction += (_, _) => ClearEvent(step, textRenderManager);
        PlayEvents.OnMoveCompleted += _deleteAction;

        textRenderManager.StartShow("MoveT");
        textRenderManager.AutoShow(0, 1);
    }

    public void UpdateStep(TutorialGuide step, TextRenderManager textRenderManager, InputManager inputManager)
    {
        if (_deleteAction != null) return;

        if (!textRenderManager.IsDoneShowingText()) return;

        if (inputManager.AnyKeyDownExcludeMouse())
        {
            step.TransitionStep(TutorialStep.Combat);
        }
    }

    public void ExitStep(TutorialGuide step)
    {
        if (_deleteAction != null)
            PlayEvents.OnMoveCompleted -= _deleteAction;

        _deleteAction = null;
    }

    public void ClearEvent(TutorialGuide step, TextRenderManager textRenderManager)
    {
        textRenderManager.AutoShow(2, 3);

        if (_deleteAction != null)
            PlayEvents.OnMoveCompleted -= _deleteAction;

        GameObject.Destroy(_targetBox);
        _deleteAction = null;
    }
}


