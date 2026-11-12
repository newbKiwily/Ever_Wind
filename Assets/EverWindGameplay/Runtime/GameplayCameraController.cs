using UnityEngine;

namespace EverWind.Gameplay
{
// CameraMoving's orbit/zoom/follow calculations, using the new entity registry instead of old singletons.
public sealed class GameplayCameraController : MonoBehaviour
{
    public ServerTransceiverManager Registry;
    public float RotationSensitivity = 2, ZoomSensitivity = 8;
    public float InitialDistance = 20, MinDistance = 8, MaxDistance = 20;
    public float MinPitch = -10, MaxPitch = 80, SmoothTime = .05f;
    public Vector2 InitialAngles;
    public GameObject PreviewCamera;
    public float Distance { get; private set; }
    Vector3 targetRotation, currentVelocity;
    float pitch, yaw;
    bool captured;
    CursorLockMode previousLock;
    bool previousVisible;
    void Awake()
    {
        Distance = InitialDistance;
        pitch = InitialAngles.x;
        yaw = InitialAngles.y;
        targetRotation = new Vector3(pitch, yaw, 0);
    }
    public void Orbit(Vector2 mouse, float scroll, bool rotate, float dt)
    {
        Distance = Mathf.Clamp(Distance - scroll * ZoomSensitivity, MinDistance, MaxDistance);
        if (!rotate)
            return;
        yaw += mouse.x * RotationSensitivity;
        pitch = Mathf.Clamp(pitch - mouse.y * RotationSensitivity, MinPitch, MaxPitch);
        targetRotation = Vector3.SmoothDamp(targetRotation, new Vector3(pitch, yaw, 0), ref currentVelocity,
                                            SmoothTime, Mathf.Infinity, dt);
    }
    void LateUpdate()
    {
        var actor = Registry != null ? Registry.LocalActor : null;
        bool ready = actor != null && Registry.Transport.Ready;
        bool rotate = ready && !GameplayInput.IsTyping && Input.GetMouseButton(1) &&
                      (captured || !GameplayInput.IsPointerOverUI);
        CaptureCursor(rotate);
        if (!ready)
            return;
        float scroll =
            GameplayInput.IsTyping || GameplayInput.IsPointerOverUI ? 0 : Input.GetAxis("Mouse ScrollWheel");
        Orbit(new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")), scroll, rotate,
              Time.deltaTime);
        transform.eulerAngles = targetRotation;
        transform.position = actor.transform.position - transform.forward * Distance;
        if (PreviewCamera != null && PreviewCamera.activeSelf)
        {
            var p = actor.transform.position;
            PreviewCamera.transform.position =
                Vector3.Lerp(PreviewCamera.transform.position,
                             p + actor.transform.forward * 4 + Vector3.up * 2, Time.deltaTime * 50);
            PreviewCamera.transform.LookAt(p + Vector3.up * 1.2f);
        }
    }
    void CaptureCursor(bool capture)
    {
        if (capture == captured)
            return;
        if (capture)
        {
            previousLock = Cursor.lockState;
            previousVisible = Cursor.visible;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = previousLock;
            Cursor.visible = previousVisible;
        }
        captured = capture;
    }
    void OnDisable() => CaptureCursor(false);
}
}
