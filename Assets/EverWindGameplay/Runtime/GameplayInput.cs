using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EverWind.Gameplay
{
// Same axes and camera-relative movement as InputManager/Player, without their singleton dependencies.
public static class GameplayInput
{
    public static InputManager Legacy;
    public static bool IsTyping
    {
        get {
            var selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            return selected != null &&
                   ((selected.GetComponent<InputField>() is InputField field && field.isFocused) ||
                    (selected.GetComponent<TMPro.TMP_InputField>() is TMPro.TMP_InputField tmp &&
                     tmp.isFocused));
        }
    }
    public static bool IsPointerOverUI =>
        EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    public static Vector3 Movement(Transform camera, float horizontal, float vertical)
    {
        if (camera == null)
            return new Vector3(horizontal, 0, vertical);
        Vector3 forward = Vector3.Scale(camera.forward, new Vector3(1, 0, 1)).normalized;
        return forward * vertical + camera.right * horizontal;
    }
    public static Vector3 ReadMovement()
    {
        return Movement(Camera.main != null ? Camera.main.transform : null,
                        Legacy != null ? Legacy.GetHorizontal() : Input.GetAxis("Horizontal"),
                        Legacy != null ? Legacy.GetVertical() : Input.GetAxis("Vertical"));
    }
}
}
