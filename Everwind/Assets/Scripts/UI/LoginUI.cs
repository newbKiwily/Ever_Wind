using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LoginUI : MonoBehaviour
{
    public TMP_InputField userIdField;
    public TMP_InputField passwordField;

    public Button loginButton;
    public Button signupButton;
    public TextMeshProUGUI resultLabel;
    public GameObject messageBox;

    private Coroutine _messageCoroutine;

    private void Awake()
    {
        loginButton.onClick.AddListener(OnLoginButtonClicked);
        signupButton.onClick.AddListener(OnSignupButtonClicked);

        // 메시지 박스 초기 상태
        messageBox.SetActive(false);
    }

    public void OnLoginButtonClicked()
    {
        string id = userIdField.text;
        string pw = passwordField.text;
        if (id == "" || pw == "")
        {
            SetResult("아이디 혹은 비밀번호를 입력해주세요");
            return;
        }

        byte[] packet = PacketMethod.BuildLoginRequest(id, pw);
        SingletonManager.Instance.GetSingleton<NetworkClient>().Send(packet);
    }

    public void OnSignupButtonClicked()
    {
        string id = userIdField.text;
        string pw = passwordField.text;
        if (id == "" || pw == "")
        {
            SetResult("아이디 혹은 비밀번호를 입력해주세요");
            return;
        }

        byte[] packet = PacketMethod.BuildSignupRequest(id, pw);
        SingletonManager.Instance.GetSingleton<NetworkClient>().Send(packet);
    }

    public void SetResult(string msg)
    {
        resultLabel.text = msg;

        if (_messageCoroutine != null)
            StopCoroutine(_messageCoroutine);

        _messageCoroutine = StartCoroutine(ShowMessageBox());
    }

    private System.Collections.IEnumerator ShowMessageBox()
    {
        messageBox.SetActive(true);
        yield return new WaitForSeconds(2.5f);
        messageBox.SetActive(false);
        _messageCoroutine = null;
    }

    public void FlushTest()
    {
        userIdField.text = "";
        passwordField.text = "";
    }
}