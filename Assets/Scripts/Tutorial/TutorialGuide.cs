using System;
using System.Collections.Generic;
using UnityEngine;

public enum TutorialStep
{
    Camera,
    Move,
    Combat,
    Interact,
    Craft,
    Equip
}

public class TutorialGuide : MonoBehaviour
{
    public Action<int> RequestAdvance;
    public Action RequestExitMap;
    public TextRenderManager TextRenderer;
    private bool presentationCleared;
    public void MarkPresentationCleared() => presentationCleared = true;
    private bool applyingServer;
    private ITutorialStep _currentStep;
    private TutorialStep _currentStepType;
    private InputManager _inputManager;
    private bool _hasActiveTutorial;
    private Dictionary<TutorialStep, ITutorialStep> _stateTable = new Dictionary<TutorialStep, ITutorialStep>();

    public GameObject moveT_taretBox;
    public Vector3 TargetBoxPosition = new Vector3(314.77f, 209.75f, -48.13f);

    public void TransitionStep(TutorialStep targetState)
    {
        if (!applyingServer && RequestAdvance != null) { RequestAdvance((int)_currentStepType); return; }
        if (_currentStep != null)
            _currentStep.ExitStep(this);

        _currentStepType = targetState;
        presentationCleared = false;
        _hasActiveTutorial = true;
        _currentStep = _stateTable[targetState];
        _currentStep.EnterStep(this);
    }

    private void Start()
    {
        _inputManager = SingletonManager.Instance.GetSingleton<InputManager>();

        _stateTable.Add(TutorialStep.Camera, new CameraStep());
        _stateTable.Add(TutorialStep.Move, new MoveStep());
        _stateTable.Add(TutorialStep.Combat, new CombatStep());
        _stateTable.Add(TutorialStep.Interact, new InteractStep());
        _stateTable.Add(TutorialStep.Craft, new CraftStep());
        _stateTable.Add(TutorialStep.Equip, new EquipStep());

        // 초깃값을 추측하지 않고 서버의 Tutorial 이벤트를 기다린다.

    }

    private void Update()
    {
        if (_currentStep != null)
        {
            if (TextRenderer != null && !TextRenderer.HasFinishedStage((int)_currentStepType, presentationCleared)) return;
            _currentStep.UpdateStep(this, _inputManager);
        }
    }

    public void CompleteTutorial()
    {
        if (!applyingServer && RequestAdvance != null) { RequestAdvance(5); return; }
        if (_currentStep != null)
        {
            _currentStep.ExitStep(this);
        }

        _currentStep = null;
        _hasActiveTutorial = false;
    }

    public TutorialStep GetTutorialStepType()
    {
        return _currentStepType;
    }

    public bool HasActiveTutorial()
    {
        return _hasActiveTutorial;
    }
    public void ApplyServerStep(int phase, ushort evidence = 0)
    {
        if (phase < -1 || phase >= _stateTable.Count) return;
        bool same = _hasActiveTutorial && phase == (int)_currentStepType;
        bool completed = _hasActiveTutorial && _currentStepType == TutorialStep.Equip && phase < 0;
        if (!same)
        {
            applyingServer = true;
            if (phase < 0) CompleteTutorial(); else TransitionStep((TutorialStep)phase);
            applyingServer = false;
        }
        if (phase > 0 && (evidence & (1 << phase)) != 0)
        {
            presentationCleared = true;
            _currentStep?.ClearEvent(this);
        }
        if (completed) RequestExitMap?.Invoke();
    }
    private void OnDestroy()
    {
        _currentStep?.ExitStep(this);
        RequestAdvance = null;
        RequestExitMap = null;
    }
}


