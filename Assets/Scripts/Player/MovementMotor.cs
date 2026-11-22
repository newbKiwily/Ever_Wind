using UnityEngine;

namespace EverWind.Gameplay
{
public sealed class MovementMotor : MonoBehaviour
{
    private float sendTimer;

    private bool dirty;

    public float Radius = .45f;

    public float MoveSpeed = 5;

    public float GroundStepUp = .6f, GroundStepDown = 1.5f;

    public LayerMask BlockingMask;

    public PlayerServerTransmitter Actor;

    public bool Forced { get; set; }
    public void Step(Vector3 delta, bool request = true, bool faceMovement = true)
    {
        delta.y = 0;

        float distance = delta.magnitude;

        if (distance < .00001f)
        {
            return;
        }

        Vector3 desiredDirection = delta;
        // 서버의 반경 확장 상자와 같은 보수적 정사각형 범위를 사용한다.
        var origin = transform.position + Vector3.up;

        float allowedDistance = distance;
        // 개체 콜라이더는 타겟 선택과 상호작용에 유지하고 이동 차단에서는 제외한다.
        // 통과 가능한 채집물 뒤의 벽을 놓치지 않도록 모든 충돌을 확인한다.
        foreach (var hit in Physics.BoxCastAll(origin, new Vector3(Radius, .9f, Radius), delta / distance, Quaternion.identity, distance + .01f, BlockingMask, QueryTriggerInteraction.Ignore))
        {
            var entity = hit.collider.GetComponentInParent<ServerTransmitter>();

            if (entity != null && (entity.Kind == EntityKind.Player || entity.Kind == EntityKind.Monster || entity.Kind == EntityKind.Resource))
                continue;
            allowedDistance = Mathf.Min(allowedDistance, Mathf.Max(0, hit.distance - .01f));
        }

        delta = delta.normalized * allowedDistance;

        var next = transform.position + delta;
        var center = Actor.Registry.MapCenter;

        float limit = Actor.Registry.MapRadius - Radius;

        if (new Vector2(next.x - center.x, next.z - center.z).magnitude > limit)
        {
            var xz = new Vector2(next.x - center.x, next.z - center.z).normalized *limit;

            next.x = xz.x + center.x;

            next.z = xz.y + center.z;
        }

        if (!ContainerInMap.Current.Loader.TryGetGroundHeight(next, GroundStepUp, GroundStepDown, out float groundHeight))
        {
            return;
        }

        next.y = groundHeight;

        transform.position = next;

        if (faceMovement)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(desiredDirection), Time.deltaTime * 10f);
        }

        if (request)
        {
            dirty = true;
        }
    }

    void Update()
    {
        sendTimer += Time.unscaledDeltaTime;

        if (sendTimer < .05f || !dirty || Forced || Actor == null || !Actor.Transport.Ready)
        {
            return;
        }

        FlushPending();
    }
    // 같은 연결에서 행동 요청보다 마지막 예측 이동을 먼저 전송한다.
    public void FlushPending()
    {
        if (!dirty || Forced || Actor == null || !Actor.Transport.Ready)
        {
            return;
        }

        sendTimer = 0;

        dirty = false;

        var request = Actor.CreateMoveRequest(transform.position);

        if (request != null)
        {
            Actor.Send(request);
        }
    }

    public void ClearPending()
    {
        dirty = false;
    }
}
}
