using System;

public class CombatStep : ITutorialStep
{
    private bool _cleared = false;
    private bool _readyForNext = false;
    private Action<int, int> _clearAction;

    public void EnterStep(TutorialGuide step, TextRenderManager textRenderManager)
    {
        _clearAction += (_, _) => ClearEvent(step, textRenderManager);
        PlayEvents.OnCombatEnemyKilled += _clearAction;

        textRenderManager.StartShow("CombatT");
        textRenderManager.AutoShow(0, 5);
    }

    public void UpdateStep(TutorialGuide step, TextRenderManager textRenderManager, InputManager inputManager)
    {
        if (_cleared && !_readyForNext) return;

        if (_readyForNext && textRenderManager.IsDoneShowingText())
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

    public void ClearEvent(TutorialGuide step, TextRenderManager textRenderManager)
    {
        if (_cleared) return;
        _cleared = true;

        textRenderManager.AutoShow(6, 7);
        _readyForNext = true;

        if (_clearAction != null)
            PlayEvents.OnCombatEnemyKilled -= _clearAction;

        _clearAction = null;
    }
}


