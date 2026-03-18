using System.Collections.Generic;
using UnityEngine;


public enum States
{
    Idle,
    Move,
    CombatRun,
    Interact,
    CombatIdle,
    Attack,
    Damaged,
    Dead
}

public class PlayerStateContexter : MonoBehaviour
{
    private IState _currentState;
    private IState _prevState;

    public Player player;
    private AnimationContexter _animationContexter;
    private InputManager _inputManager;
    private Vector3 _armPosition = new Vector3(0.12f, 0.24f, 0.02f);
    private Quaternion _armRotation = Quaternion.Euler(37.075f, 148.54f, 265.49f);
    private Vector3 _backPosition = new Vector3(0.9459f, 0.8265f, -0.121f);
    private Quaternion _backRotation = Quaternion.Euler(187.69f, -6.533f, 38.847f);

    public GameObject Weapon;
    public GameObject armObj;
    public GameObject backObj;

    private Dictionary<States, IState> _stateTable = new Dictionary<States, IState>();

    public Transform CurrentTarget { get; private set; }

    public void Init(Player player, InputManager inputManager)
    {
        this.player = player;
        this._inputManager = inputManager;
        player.OnDied += HandleDied;
    }

    public AnimationContexter GetAnimationContexter()
    {
        return _animationContexter;
    }

    public void TransitionState(States targetState, Transform target = null)
    {
        if (_currentState != null)
            _currentState.ExitState(this);

        _prevState = _currentState;

        CurrentTarget = target;
        _currentState = _stateTable[targetState];

        _currentState.EnterState(this);
    }

    private void Start()
    {
        _animationContexter = GetComponent<AnimationContexter>();

        _stateTable.Add(States.Idle, new IdleState());
        _stateTable.Add(States.Move, new MoveState());
        _stateTable.Add(States.CombatRun, new CombatRunState());
        _stateTable.Add(States.Interact, new InteractState());
        _stateTable.Add(States.CombatIdle, new CombatIdleState());
        _stateTable.Add(States.Attack, new AttackState());
        _stateTable.Add(States.Damaged, new DamagedState());
        _stateTable.Add(States.Dead, new DeadState());
        _currentState = _stateTable[States.Idle];

        // 초기 상태 설정
        TransitionState(States.Idle);
        OffWeapon();
    }

    private void Update()
    {
        if (_currentState != null)
        {
            _currentState.UpdateState(this, _inputManager);
        }
    }

    public IState GetPrevState()
    {
        return _prevState;
    }

    public IState GetCurrState()
    {
        return _currentState;
    }

    public void OnWeapon()
    {
        Weapon.transform.parent = armObj.transform;
        Weapon.transform.localPosition = _armPosition;
        Weapon.transform.localRotation = _armRotation;
    }

    public void OffWeapon()
    {
        Weapon.transform.parent = backObj.transform;
        Weapon.transform.localPosition = _backPosition;
        Weapon.transform.localRotation = _backRotation;
    }

    public void HandleDied()
    {
        TransitionState(States.Dead);
        player.GetCombatManager().TargetEnemy = null;
    }
}