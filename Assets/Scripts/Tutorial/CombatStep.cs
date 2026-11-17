using System;

public class CombatStep : ITutorialStep
{
    private bool _cleared = false;
    private bool _readyForNext = false;
    private Action<int, int> _clearAction;

    public void EnterStep(TutorialGuide step)
    {
        _clearAction += (_, _) => ClearEvent(step);
        PlayEvents.OnCombatEnemyKilled += _clearAction;

    }

    public void UpdateStep(TutorialGuide step, InputManager inputManager)
    {
        if (_cleared && !_readyForNext) return;

        if (_readyForNext)
        {
            if (inputManager.AnyKeyDownExcludeMouse())
            {
                step.TransitionStep(TutorialStep.Interact);
                _readyForNext = false;
            }
        }
    }

    public void ExitStep(TutorialGuide step)
    {
        _cleared = false;
        _readyForNext = false;

        if (_clearAction != null)
            PlayEvents.OnCombatEnemyKilled -= _clearAction;

        _clearAction = null;
    }

    public void ClearEvent(TutorialGuide step)
    {
        if (_cleared) return;
        _cleared = true;

        _readyForNext = true;

        if (_clearAction != null)
            PlayEvents.OnCombatEnemyKilled -= _clearAction;

        _clearAction = null;
    }
}


