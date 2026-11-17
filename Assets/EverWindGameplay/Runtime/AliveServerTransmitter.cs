using UnityEngine;

namespace EverWind.Gameplay
{
public abstract class AliveServerTransmitter : ServerTransmitter
{
    private Vector3 destination;

    private bool havePosition;

    public AlivePresentation Presentation { get; private set; }
    public float Hp { get; private set; }
    public float MaxHp { get; private set; }
    public ulong ActionId { get; private set; }
    public PlayerMode Mode { get; private set; }
    protected override void InitializeEntity(ServerMessage spawn)
    {
        base.InitializeEntity(spawn);

        Apply(spawn);

        ApplyPosition(spawn.Position);

        Presentation?.Handle(spawn);
    }

    public override void Receive(ServerMessage message)
    {
        // 공통 상태와 자식의 상태 정책을 갱신한 뒤 구독 컴포넌트에 전달한다.
        Apply(message);

        ApplyServerPosition(message);

        Presentation?.Handle(message);

        base.Receive(message);
    }

    public void ConfigureVisual(ActionVisualProfile profile, Animator animator)
    {
        Presentation = new AlivePresentation(this, profile, animator);
    }

    protected virtual void ApplyServerPosition(ServerMessage message)
    {
        if (message.Kind == EventKind.State || message.Kind == EventKind.MoveResult || message.Kind == EventKind.Hit)
        {
            destination = message.Position;

            havePosition = true;
        }
    }

    protected void ApplyPosition(Vector3 position)
    {
        transform.position = position;
    }

    protected virtual void LateUpdate()
    {
        if (!IsLocal && havePosition)
        {
            Vector3 previous = transform.position;

            ApplyPosition(Vector3.Lerp(previous, destination, 1 - Mathf.Exp(-18 * Time.unscaledDeltaTime)));

            Vector3 direction = destination - previous;

            direction.y = 0;

            if (direction.sqrMagnitude > .0001f)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }
        }

        Presentation?.Tick(Time.unscaledDeltaTime);
    }

    private void Apply(ServerMessage message)
    {
        if (message.Kind == EventKind.Spawn || message.Kind == EventKind.State || message.Kind == EventKind.Hit || message.Kind == EventKind.Stats)
        {
            Hp = message.Hp;

            MaxHp = message.MaxHp;
        }

        if (message.Kind == EventKind.Spawn || message.Kind == EventKind.State || message.Kind == EventKind.Hit || message.Kind == EventKind.ActionStarted || message.Kind == EventKind.ActionEnded || message.Kind == EventKind.ActionCancelled)
        {
            ApplyMode(message.Mode);
        }

        if (message.Kind == EventKind.ActionStarted)
        {
            ActionId = message.ActionId;
        }
        else if ((message.Kind == EventKind.ActionEnded || message.Kind == EventKind.ActionCancelled) && ActionId == message.ActionId)
        {
            ActionId = 0;
        }
    }

    protected virtual void ApplyMode(PlayerMode mode)
    {
        Mode = mode;
    }
}
}
