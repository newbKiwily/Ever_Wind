using UnityEngine;


public enum EnemySyncState
{
    Idle,
    Move,
    Attack,
    Damaged,
    Dead
}

public class EnemyNetworkSync : MonoBehaviour
{
    private Enemy _enemy;
    private Animator _animator;
    private CharacterController _cc;

    public EnemySyncState CurrentState = EnemySyncState.Idle;
    public int CurrentOwnerDbId = -1;
    public bool IsAttacking = false;
    public int DamagedCount = 1;

    public float MoveSpeed = 3.0f;
    public float AttackDistance = 2.0f;
    public float RotationSpeed = 10f;

    public float PositionCorrectionSpeed = 10f;
    private Vector3 _serverTargetPosition;

    public float SyncInterval = 0.1f;

    private float _syncTimer = 0f;

    private Transform _localPlayerTarget;

    private void Start()
    {
        _enemy = GetComponent<Enemy>();
        _animator = GetComponent<Animator>();
        _cc = GetComponent<CharacterController>();
        _serverTargetPosition = transform.position;
    }

    private void Update()
    {
        // 죽어있으면 아무 동작도 하지 않음
        if (CurrentState == EnemySyncState.Dead)
            return;

        int myDbId = SingletonManager.Instance.GetSingleton<NetworkClient>().UserDbId;
        bool isOwner = (CurrentOwnerDbId == myDbId);

        if (_cc != null) _cc.enabled = isOwner;

        if (isOwner)
        {
            if (CurrentState == EnemySyncState.Move)
            {
                if (_localPlayerTarget == null)
                {
                    var playerObj = SingletonManager.Instance.GetSingleton<WorldLoader>().InstancedPlayer;
                    if (playerObj != null)
                    {
                        var playerState = playerObj.GetComponent<Player>().GetPlayerStateContexter().GetCurrState();
                        // 플레이어가 살아있을 때만 타겟으로 지정
                        if (!(playerState is DeadState))
                        {
                            _localPlayerTarget = playerObj.transform;
                        }
                    }

                    // 그래도 타겟이 없으면 (또는 죽어있으면) 이번 프레임 넘김
                    if (_localPlayerTarget == null)
                        return;
                }
                else
                {
                    // 2. 타겟을 쫓아가는 도중에 타겟이 죽었는지 실시간 체크
                    var targetState = _localPlayerTarget.GetComponent<Player>().GetPlayerStateContexter().GetCurrState();
                    if (targetState is DeadState)
                    {
                        _localPlayerTarget = null; // 타겟 해제
                        ChangeStateLocal(EnemySyncState.Idle); // 대기 상태로 전환
                        return; // 이동 및 공격 로직 중단
                    }
                }

                Vector3 dir = _localPlayerTarget.position - transform.position;
                float distance = dir.magnitude;

                if (distance <= AttackDistance)
                {
                    ChangeStateLocal(EnemySyncState.Attack);
                    byte[] attackAnimPkt = PacketMethod.BuildEnemyAttackAnim(_enemy.InstanceNum);
                    SingletonManager.Instance.GetSingleton<NetworkClient>().Send(attackAnimPkt);
                    return;
                }

                Vector3 moveDir = dir.normalized;
                _cc.Move(moveDir * MoveSpeed * Time.deltaTime);

                Vector3 horizontalDir = new Vector3(moveDir.x, 0, moveDir.z);
                if (horizontalDir != Vector3.zero)
                {
                    Vector3 lookTargetDir = horizontalDir;

                    if (GetComponent<Spider>() != null)
                    {
                        lookTargetDir = -horizontalDir;
                    }

                    transform.rotation = Quaternion.Slerp(
                        transform.rotation,
                        Quaternion.LookRotation(lookTargetDir),
                        Time.deltaTime * RotationSpeed
                    );
                }


                _syncTimer += Time.deltaTime;
                if (_syncTimer >= SyncInterval)
                {
                    _syncTimer = 0f;
                    byte[] movePkt = PacketMethod.BuildEnemyMoveSync(_enemy.InstanceNum, transform.position);
                    SingletonManager.Instance.GetSingleton<NetworkClient>().Send(movePkt);
                }
            }
        }
        else
        {

            if (CurrentState == EnemySyncState.Move)
            {
                if (Vector3.Distance(transform.position, _serverTargetPosition) > 0.05f)
                {
                    transform.position = Vector3.Lerp(transform.position, _serverTargetPosition, Time.deltaTime * PositionCorrectionSpeed);

                    Vector3 moveDir = (_serverTargetPosition - transform.position).normalized;
                    Vector3 horizontalDir = new Vector3(moveDir.x, 0, moveDir.z);
                    if (horizontalDir != Vector3.zero)
                    {
                        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(horizontalDir), Time.deltaTime * RotationSpeed);
                    }
                }
            }
            else if (CurrentState == EnemySyncState.Attack || CurrentState == EnemySyncState.Idle)
            {
                if (CurrentOwnerDbId != -1)
                {
                    var otherPlayer = SingletonManager.Instance.GetSingleton<OtherPlayerManager>().FindPlayer(CurrentOwnerDbId);
                    if (otherPlayer != null)
                    {
                        Transform targetPlayer = otherPlayer.GetComponent<Transform>();

                        Vector3 lookDir = (targetPlayer.position - transform.position).normalized;
                        Vector3 horizontalDir = new Vector3(lookDir.x, 0, lookDir.z);

                        if (horizontalDir != Vector3.zero)
                        {
                            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(horizontalDir), Time.deltaTime * RotationSpeed);
                        }
                    }
                }
            }
        }
    }

    private void ChangeStateLocal(EnemySyncState newState)
    {
        if (CurrentState == newState) return;

        // 이전 상태 종료 처리
        if (CurrentState == EnemySyncState.Move) _animator.SetBool("Move", false);
        if (CurrentState == EnemySyncState.Idle) _animator.SetBool("Idle", false);

        CurrentState = newState;

        switch (CurrentState)
        {
            case EnemySyncState.Idle:
                _animator.SetBool("Idle", true);
                break;
            case EnemySyncState.Move:
                _animator.SetBool("Move", true);
                break;
            case EnemySyncState.Attack:
                _animator.SetTrigger("Attack");
                break;
            case EnemySyncState.Dead:
                // 사망 처리는 서버로부터 Dead 상태를 명시적으로 받았을 때만 처리
                break;
        }
    }


    public void OnReceiveDamaged(float currentHp, float damageAmount, int ownerDbId)
    {

        CurrentOwnerDbId = ownerDbId;
        _enemy.SyncDamage(currentHp, damageAmount);

        // 피격 모션 세팅 및 재생
        _enemy.SwitchDamagedMotion(DamagedCount);
        _animator.Play("Damaged", 0, 0.0f);

        DamagedCount++;
        if (DamagedCount > 3) DamagedCount = 1;

        if (currentHp > 0)
        {
            ChangeStateLocal(EnemySyncState.Move);
        }
        else
        {
            ChangeStateLocal(EnemySyncState.Dead);
            if (_enemy.ShouldRequestDeath())
            {
                byte[] deadPkt = PacketMethod.BuildEnemyDeadReq(_enemy.InstanceNum);
                SingletonManager.Instance.GetSingleton<NetworkClient>().Send(deadPkt);
            }
        }
    }

    public void OnReceiveMoveSync(Vector3 pos)
    {
        int myDbId = SingletonManager.Instance.GetSingleton<NetworkClient>().UserDbId;

        if (CurrentOwnerDbId == myDbId)
            return;

        _serverTargetPosition = pos;
        if (CurrentState != EnemySyncState.Move && CurrentState != EnemySyncState.Attack)
        {
            ChangeStateLocal(EnemySyncState.Move);
        }
    }


    public void Attack()
    {
        IsAttacking = true;

        int myDbId = SingletonManager.Instance.GetSingleton<NetworkClient>().UserDbId;
        if (CurrentOwnerDbId == myDbId)
        {
            if (_localPlayerTarget != null)
            {
                var targetDamageable = _localPlayerTarget.GetComponent<IDamageable>();
                if (targetDamageable != null)
                {

                    targetDamageable.TakeDamage(10.0f, this.transform);
                }
            }
        }
    }

    public void EndAttack()
    {
        IsAttacking = false;
        DamagedCount = 1;

        int myDbId = SingletonManager.Instance.GetSingleton<NetworkClient>().UserDbId;
        bool isOwner = (CurrentOwnerDbId == myDbId);

        if (isOwner)
        {
            if (_localPlayerTarget != null)
            {
                var targetState = _localPlayerTarget.GetComponent<Player>().GetPlayerStateContexter().GetCurrState();

                if (targetState is DeadState)
                {
                    _localPlayerTarget = null;
                    ChangeStateLocal(EnemySyncState.Idle);
                }
                else
                {
                    ChangeStateLocal(EnemySyncState.Move);
                }
            }
            else
            {
                ChangeStateLocal(EnemySyncState.Idle);
            }
        }
        else
        {
            if (CurrentOwnerDbId != -1)
            {
                ChangeStateLocal(EnemySyncState.Move);
            }
            else
            {
                ChangeStateLocal(EnemySyncState.Idle);
            }
        }
    }

    public void OnReceiveAttackAnim()
    {
        int myDbId = SingletonManager.Instance.GetSingleton<NetworkClient>().UserDbId;
        if (CurrentOwnerDbId == myDbId) return;

        ChangeStateLocal(EnemySyncState.Attack);
    }

    public void OnReceiveDeadAck()
    {
        ChangeStateLocal(EnemySyncState.Dead);
        _enemy.Die();
    }
}
