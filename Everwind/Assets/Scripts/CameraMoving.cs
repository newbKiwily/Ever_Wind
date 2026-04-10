using UnityEngine;

public class CameraMoving : MonoBehaviour
{
    private float _yaxis;
    private float _xaxis;

    public Transform Target;

    public float RotSensitive = 2.0f;
    private float _dis = 20.0f;

    private float _rotationMin = -10f;
    private float _rotationMax = 80f;
    private float _smoothTime = 0.05f;

    private Vector3 _targetRotation;
    private Vector3 _currentVel;
    private float _cameraMoveTime;
    private bool _cameraTutorialCompleted;

    public GameObject PreviewCamera;

    public float ZoomSensitive = 8.0f;
    private float _minDis = 8.0f;
    private float _maxDis = 20.0f;

    private float _previewFollowSpeed = 50.0f;

    private InputManager InputManager => SingletonManager.Instance.GetSingleton<InputManager>();

    private void Awake()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        _cameraMoveTime = 5.0f;
        _cameraTutorialCompleted = false;
    }

    void LateUpdate()
    {
        if (InputManager == null || Target == null)
            return;

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0f && !UIEvents.IsPointerOverQuestScroll)
        {
            _dis -= scroll * ZoomSensitive;
            _dis = Mathf.Clamp(_dis, _minDis, _maxDis);
        }

        if (InputManager.GetControlCamera())
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;

            _yaxis += Input.GetAxis("Mouse X") * RotSensitive;
            _xaxis -= Input.GetAxis("Mouse Y") * RotSensitive;
            _xaxis = Mathf.Clamp(_xaxis, _rotationMin, _rotationMax);

            _targetRotation = Vector3.SmoothDamp(
                _targetRotation,
                new Vector3(_xaxis, _yaxis),
                ref _currentVel,
                _smoothTime
            );

            if (!_cameraTutorialCompleted)
            {
                _cameraMoveTime -= Time.deltaTime;
                if (_cameraMoveTime <= 0)
                {
                    _cameraTutorialCompleted = true;
                    PlayEvents.EvCameraCompleted();
                }
            }
        }
        else
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        transform.eulerAngles = _targetRotation;
        transform.position = Target.position - transform.forward * _dis;

        if (PreviewCamera != null && PreviewCamera.activeSelf)
        {
            Transform pCam = PreviewCamera.transform;

            float frontDistance = 4f;
            float height = 2f;

            Vector3 desiredPos = Target.position + (Target.forward * frontDistance) + (Vector3.up * height);

            pCam.position = Vector3.Lerp(pCam.position, desiredPos, Time.deltaTime * _previewFollowSpeed);

            Vector3 lookPos = Target.position + Vector3.up * 1.2f;
            pCam.LookAt(lookPos);
        }
    }

    public void OnPreviewCam()
    {
        if (PreviewCamera != null)
            PreviewCamera.SetActive(true);
    }

    public void OffPreviCam()
    {
        if (PreviewCamera != null)
            PreviewCamera.SetActive(false);
    }
}


