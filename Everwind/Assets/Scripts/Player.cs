using System;
using UnityEngine;

public class Player : MonoBehaviour, IDamageable
{
    private Vector3 _direction;
    private CharacterController _controller;
    private float _verticalVelocity;

    public Transform CameraTransform;

    private Transform _destination;
    private bool _isMoveto;

    public event Action EvArrive;

    private float _h, _v;
    public FieldItem ClosetItem;
    public float Radius;

    public Action<float> OnTakeDamage;

    private Vector3 _moveDir;
    private uint _moveTimeStamp = 0;

    private float _moveSyncTimer;
    private const float MOVE_SYNC_INTERVAL = 0.05f;

    public event Action OnDied;

    private PlayerStatManager _statManager;
    private CombatManager _combatManager;
    private PlayerStateContexter _stateContexter;
    private InputManager _inputManager;

    public PlayerStatManager GetPlayerStatManager()
    { return _statManager; }
    public CombatManager GetCombatManager()
    { return _combatManager; }
    public PlayerStateContexter GetPlayerStateContexter()
    { return _stateContexter; }

    private NetworkClient _networkClient;

    public void TakeDamage(float damage, Transform attacker)
    {
        float finalDamage = _statManager.CalculateDamaged(damage);
        SingletonManager.Instance.GetSingleton<EffectManager>().PlayEffect("Damaged", this.transform.position);
        UIEvents.EvDamageTextRequested(transform.position, Mathf.RoundToInt(finalDamage));
        if (_combatManager.TargetEnemy == null)
        {
            _combatManager.TargetEnemy = attacker.gameObject;
        }
        _statManager.SetHp(_statManager.GetHp() - finalDamage);
        OnTakeDamage?.Invoke(_statManager.GetHp());
        if (_statManager.GetHp() <= 0)
        {
            OnDied?.Invoke();
            return;
        }
        _combatManager.KnockBack();
    }

    public void RefreshHpUI()
    {
        OnTakeDamage?.Invoke(_statManager.GetHp());
    }

    public Vector3 GetInputVector()
    {
        if (_isMoveto)
            return new Vector3(1, 0, 0);
        Vector3 vec = new Vector3(_h, 0, _v);
        return vec;
    }

    public void Init()
    {
        _inputManager = SingletonManager.Instance.GetSingleton<InputManager>();
        _controller = this.GetComponent<CharacterController>();
        _combatManager = this.GetComponent<CombatManager>();
        _combatManager.Init(this, _inputManager);
        _stateContexter = this.GetComponent<PlayerStateContexter>();
        _stateContexter.Init(this, _inputManager);
        _statManager = this.GetComponent<PlayerStatManager>();
        _statManager.Init();
        _networkClient = SingletonManager.Instance.GetSingleton<NetworkClient>();
        Radius = 50.0f;
    }

    void LateUpdate()
    {
        _moveSyncTimer += Time.deltaTime;
        if (_moveSyncTimer >= MOVE_SYNC_INTERVAL)
        {
            _moveSyncTimer = 0f;
            SendMoveSync();
        }
    }

    void SendMoveSync()
    {
        _moveTimeStamp++;
        Vector3 pos = transform.position;

        bool isMoving = _moveDir.sqrMagnitude > 0.001f || _isMoveto;

        float speedParam = isMoving ?
            1f : 0f;

        byte[] packet = PacketMethod.BuildMoveSyncRequest(
            _networkClient.UserDbId,
            pos,
            speedParam, _moveTimeStamp
        );

        _networkClient.Send(packet);
    }

    void Update()
    {
        if (_stateContexter.GetCurrState() is AttackState || _stateContexter.GetCurrState() is DamagedState)
            return;

        _h = _inputManager.GetHorizontal();
        _v = _inputManager.GetVertical();

        if (_isMoveto)
        {
            if (_h != 0 || _v != 0)
            {
                StopMoveto();
            }
            else
            {
                MoveTo();
            }
        }

        Vector3 cameraForward = Vector3.Scale(CameraTransform.forward, new Vector3(1, 0, 1)).normalized;
        _moveDir = cameraForward * _v + CameraTransform.right * _h;

        _direction = _moveDir * _statManager.GetSpeed();

        if (_controller.isGrounded)
        {
            _verticalVelocity = -2.0f;
        }
        else
        {
            _verticalVelocity += Physics.gravity.y * Time.deltaTime;
        }

        _direction.y = _verticalVelocity;
        _controller.Move(_direction * Time.deltaTime);

        if (_moveDir.sqrMagnitude > 0)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(_moveDir), Time.deltaTime * 10f);
        }
    }

    bool MoveTo()
    {
        Vector3 dirVec = _destination.position - this.transform.position;

        if (dirVec.magnitude < 2.0f)
        {
            _isMoveto = false;

            if (EvArrive != null)
            {
                EvArrive();
            }
            return true;
        }

        Vector3 uVec = dirVec.normalized;

        _controller.Move(uVec * _statManager.GetSpeed() * Time.deltaTime);

        Vector3 horizontalDirection = new Vector3(uVec.x, 0, uVec.z);

        if (horizontalDirection != Vector3.zero)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(horizontalDirection), Time.deltaTime * 10f);
        }

        return false;
    }

    public void LookAtTarget(Transform targetTransform)
    {
        if (targetTransform == null)
            return;

        Vector3 directionToTarget = targetTransform.position - transform.position;

        Vector3 horizontalDirection = new Vector3(directionToTarget.x, 0, directionToTarget.z);

        if (horizontalDirection == Vector3.zero)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(horizontalDirection);
        transform.rotation = targetRotation;
    }

    public void StartMoveto(Transform targetTransform)
    {
        _destination = targetTransform;
        _isMoveto = true;
    }

    public void StopMoveto()
    {
        _destination = null;
        _isMoveto = false;
    }

    public GameObject DetectObtainable()
    {
        Collider[] colliders = Physics.OverlapSphere(this.transform.position, Radius);

        if (colliders.Length <= 0)
            return null;

        float minDistance = float.MaxValue;
        GameObject closetObj = null;
        foreach (var it in colliders)
        {
            var temp = it.gameObject.GetComponent<IObtainable>();
            if (temp == null)
                continue;

            float distance = (it.transform.position - this.transform.position).sqrMagnitude;

            if (distance <= minDistance)
            {
                closetObj = it.gameObject;
                minDistance = distance;
            }
        }

        if (closetObj != null)
        {
            ClosetItem = closetObj.GetComponent<FieldItem>();
            return closetObj;
        }

        return null;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("TutorialBox"))
        {
            other.gameObject.SetActive(false);
            PlayEvents.EvMoveCompleted();
        }
    }

    public Vector3 GetPlacePositionForItem()
    {
        float bottom = (transform.position.y + _controller.center.y) - (_controller.height / 2f);
        return new Vector3(transform.position.x, bottom, transform.position.z);
    }
}


