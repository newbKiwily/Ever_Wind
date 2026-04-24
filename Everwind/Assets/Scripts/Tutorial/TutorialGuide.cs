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
    private ITutorialStep _currentStep;
    private TutorialStep _currentStepType;
    private TextRenderManager _textRenderManager;
    private InputManager _inputManager;
    private DataCenter _dataCenter;
    private bool _hasActiveTutorial;
    private Dictionary<TutorialStep, ITutorialStep> _stateTable = new Dictionary<TutorialStep, ITutorialStep>();

    public GameObject moveT_taretBox;
    public Vector3 TargetBoxPosition = new Vector3(314.77f, 209.75f, -48.13f);

    public void TransitionStep(TutorialStep targetState)
    {
        if (_currentStep != null)
            _currentStep.ExitStep(this);

        _currentStepType = targetState;
        _hasActiveTutorial = true;
        _dataCenter.loginData.TutorialStep = (int)targetState;
        _currentStep = _stateTable[targetState];
        _currentStep.EnterStep(this, _textRenderManager);
    }

    private void Start()
    {
        _dataCenter = SingletonManager.Instance.GetSingleton<DataCenter>();
        _inputManager = SingletonManager.Instance.GetSingleton<InputManager>();
        _textRenderManager = SingletonManager.Instance.GetSingleton<TextRenderManager>();

        _stateTable.Add(TutorialStep.Camera, new CameraStep());
        _stateTable.Add(TutorialStep.Move, new MoveStep());
        _stateTable.Add(TutorialStep.Combat, new CombatStep());
        _stateTable.Add(TutorialStep.Interact, new InteractStep());
        _stateTable.Add(TutorialStep.Craft, new CraftStep());
        _stateTable.Add(TutorialStep.Equip, new EquipStep());

        int savedStep = _dataCenter.loginData.TutorialStep;
        if (savedStep < 0)
        {
            CompleteTutorial();
            return;
        }

        if (savedStep > (int)TutorialStep.Equip)
        {
            savedStep = (int)TutorialStep.Camera;
        }

        TransitionStep((TutorialStep)savedStep);
    }

    private void Update()
    {
        if (_currentStep != null)
        {
            _currentStep.UpdateStep(this, _textRenderManager, _inputManager);
        }
    }

    public void CompleteTutorial()
    {
        if (_currentStep != null)
        {
            _currentStep.ExitStep(this);
        }

        _currentStep = null;
        _hasActiveTutorial = false;
        _dataCenter.loginData.TutorialStep = -1;
    }

    public TutorialStep GetTutorialStepType()
    {
        return _currentStepType;
    }

    public bool HasActiveTutorial()
    {
        return _hasActiveTutorial;
    }
}

