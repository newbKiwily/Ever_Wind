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
        if (!applyingServer && EverWind.Gameplay.GameplayIntegration.Active != null) { EverWind.Gameplay.GameplayIntegration.Active.AdvanceTutorial((int)_currentStepType); return; }
        if (_currentStep != null)
            _currentStep.ExitStep(this);

        _currentStepType = targetState;
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
            _currentStep.UpdateStep(this, _inputManager);
        }
    }

    public void CompleteTutorial()
    {
        if (!applyingServer && EverWind.Gameplay.GameplayIntegration.Active != null) { EverWind.Gameplay.GameplayIntegration.Active.AdvanceTutorial(5); return; }
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
    public void ApplyServerStep(int phase)
    {
        if(_hasActiveTutorial && phase==(int)_currentStepType)return;
        bool completed=_hasActiveTutorial && _currentStepType==TutorialStep.Equip && phase<0;
        applyingServer=true;
        if(phase<0)CompleteTutorial();else TransitionStep((TutorialStep)phase);
        applyingServer=false;
        if(completed) EverWind.Gameplay.GameplayIntegration.Active.Send(new EverWind.Gameplay.Request{Kind=EverWind.Gameplay.RequestKind.ChangeMap,Definition=1});
    }
    private void OnDestroy(){_currentStep?.ExitStep(this);}
}


