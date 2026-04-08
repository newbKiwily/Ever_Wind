using UnityEngine;

public class OtherPlayer : MonoBehaviour
{
    public int UserDbId { get; private set; }
    private OtherPlayerNetworkSync _networkSync;

    private Vector3 _armPosition = new Vector3(0.1255408f, -0.1511414f, -0.107931f);
    private Quaternion _armRotation = Quaternion.Euler(124.652f, 61.85f, -47.065f);
    private Vector3 _backPosition = new Vector3(1.12291f, -0.7949995f, 0.7162999f);
    private Quaternion _backRotation = Quaternion.Euler(192.0f, -5.0f, 32.0f);

    public GameObject Weapon;
    public GameObject ArmObj;
    public GameObject BackObj;

    private uint _lastTimestamp;

    public void Init(int userDbId)
    {
        this.UserDbId = userDbId;
        _networkSync = GetComponent<OtherPlayerNetworkSync>();
    }

    public void OnMoveSync(Vector3 pos, float speed, uint timestamp)
    {
        if (timestamp <= _lastTimestamp)
            return;

        _lastTimestamp = timestamp;

        if (_networkSync != null)
        {
            _networkSync.OnReceiveMoveSync(pos, speed);
        }
    }

    public void OnWeapon()
    {
        Weapon.transform.parent = ArmObj.transform;
        Weapon.transform.localPosition = _armPosition;
        Weapon.transform.localRotation = _armRotation;
    }

    public void OffWeapon()
    {
        Weapon.transform.parent = BackObj.transform;
        Weapon.transform.localPosition = _backPosition;
        Weapon.transform.localRotation = _backRotation;
    }
}
