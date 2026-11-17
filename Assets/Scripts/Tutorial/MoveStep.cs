using System;
using UnityEngine;

public class MoveStep : ITutorialStep
{
    private Action<int, int> _deleteAction;
    private GameObject _targetBox;
    public void EnterStep(TutorialGuide step)
    {
        // 이동 판정은 서버의 범위 검증을 사용하며 안내 마커는 생성하지 않는다.

        _deleteAction += (_, _) => ClearEvent(step);
        PlayEvents.OnMoveCompleted += _deleteAction;

    }

    public void UpdateStep(TutorialGuide step, InputManager inputManager)
    {
        if (_deleteAction != null) return;


        if (inputManager.AnyKeyDownExcludeMouse())
        {
            step.TransitionStep(TutorialStep.Combat);
        }
    }

    public void ExitStep(TutorialGuide step)
    {
        GameObject.Destroy(_targetBox);
        _targetBox=null;
        if (_deleteAction != null)
            PlayEvents.OnMoveCompleted -= _deleteAction;

        _deleteAction = null;
    }

    public void ClearEvent(TutorialGuide step)
    {
        if (_deleteAction != null)
            PlayEvents.OnMoveCompleted -= _deleteAction;

        GameObject.Destroy(_targetBox);
        _deleteAction = null;
    }
}


