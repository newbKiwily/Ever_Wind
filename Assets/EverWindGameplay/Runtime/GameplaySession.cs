using UnityEngine;
using UnityEngine.SceneManagement;
namespace EverWind.Gameplay
{
[DefaultExecutionOrder(-300)]
public sealed class GameplaySession : MonoBehaviour
{
    public static GameplaySession Current { get; private set; }
    public NetworkClient Transport;
    public GameplayCatalog Catalog;
    public GameplayWorldCatalog WorldCatalog;
    public string Host = "127.0.0.1";
    public int Port = 4000;
    public string Status => Transport.Status;
    bool loading, logout;
    ulong logoutRequest;
    void Awake()
    {
        if (Current != null && Current != this)
        {
            Destroy(gameObject);
            return;
        }
        Current = this;
        DontDestroyOnLoad(gameObject);
        Transport.Message += Message;
        Transport.SignedUp += SignupResult;
        Transport.Disconnected += Disconnected;
        SceneManager.sceneLoaded += Loaded;
    }
    public void Login(string user, string password)
    {
        Transport.Host = Host;
        Transport.Port = Port;
        Transport.Login(user, password);
    }
    public void Signup(string user, string password)
    {
        Transport.Host = Host;
        Transport.Port = Port;
        Transport.Signup(user, password);
    }
    void SignupResult(int code)
    {
        FindFirstObjectByType<LoginUI>()?.SetResult(code == 0   ? "가입되었습니다. 로그인해 주세요."
                                                    : code == 1 ? "이미 사용 중인 계정입니다."
                                                                : "가입에 실패했습니다.");
    }
    void Message(ServerMessage m)
    {
        if (m.Kind == EventKind.SnapshotEnd && SceneManager.GetActiveScene().name == "LogIn" && !loading)
        {
            loading = true;
            SceneManager.LoadSceneAsync("InGame");
        }
        if (logout && m.Kind == EventKind.Saved && m.RequestId == logoutRequest)
        {
            logout = false;
            if (m.Error == ResultCode.None)
                ReturnToLogin();
        }
    }
    void Loaded(Scene scene, LoadSceneMode mode)
    {
        loading = false;
        if (scene.name == "InGame")
        {
            if (Transport.Ready)
                Transport.Send(new Request { Kind = RequestKind.Join });
            else
                ReturnToLogin();
        }
    }
    public void Logout()
    {
        if (!Transport.Ready)
        {
            ReturnToLogin();
            return;
        }
        logout = true;
        logoutRequest = Transport.Send(new Request { Kind = RequestKind.Logout });
    }
    public void ReturnToLogin()
    {
        Transport.CloseConnection();
        if (SceneManager.GetActiveScene().name != "LogIn" && !loading)
        {
            loading = true;
            SceneManager.LoadSceneAsync("LogIn");
        }
    }
    void Disconnected()
    {
        if (SceneManager.GetActiveScene().name == "InGame" && !loading)
        {
            loading = true;
            SceneManager.LoadSceneAsync("LogIn");
        }
    }
    void OnDestroy()
    {
        if (Current != this)
            return;
        Transport.Message -= Message;
        Transport.SignedUp -= SignupResult;
        Transport.Disconnected -= Disconnected;
        SceneManager.sceneLoaded -= Loaded;
        Current = null;
    }
}
}
