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
    private Dictionary<TutorialStep, ITutorialStep> _stateTable = new Dictionary<TutorialStep, ITutorialStep>();

    public GameObject moveT_taretBox;
    public GameObject dummy_enemy, dummy_enemy2;
    public GameObject obtainObj1, obtainObj2, obtainObj3, ObtainObj4;

    public void TransitionStep(TutorialStep targetState)
    {
        if (_currentStep != null)
            _currentStep.ExitStep(this);

        _currentStepType = targetState;
        _currentStep = _stateTable[targetState];
        _currentStep.EnterStep(this, _textRenderManager);
    }

    private void Start()
    {
        _inputManager = SingletonManager.Instance.GetSingleton<InputManager>();
        _textRenderManager = SingletonManager.Instance.GetSingleton<TextRenderManager>();

        dummy_enemy.SetActive(true);
        dummy_enemy2.SetActive(true);

        _stateTable.Add(TutorialStep.Camera, new CameraStep());
        _stateTable.Add(TutorialStep.Move, new MoveStep());
        _stateTable.Add(TutorialStep.Combat, new CombatStep());
        _stateTable.Add(TutorialStep.Interact, new InteractStep());
        _stateTable.Add(TutorialStep.Craft, new CraftStep());
        _stateTable.Add(TutorialStep.Equip, new EquipStep());

        TransitionStep(TutorialStep.Craft);
    }

    private void Update()
    {
        if (_currentStep != null)
        {
            _currentStep.UpdateStep(this, _textRenderManager, _inputManager);
        }
    }

    public TutorialStep GetTutorialStepType()
    {
        return _currentStepType;
    }
}