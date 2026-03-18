using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : SingletonBase<SceneLoader>
{
    public override bool IsPersistent => true;

    public LoginUI loginUI; // 타입명과 필드명이 같으므로 소문자 시작

    protected override void Awake()
    {
        Priority = -70;
        base.Awake();
    }

    public void LoadGame(string sceneName)
    {
        StartCoroutine(LoadSequence(sceneName));
    }

    private IEnumerator LoadSequence(string sceneName)
    {
        var data = SingletonManager.Instance.GetSingleton<DataCenter>();
        loginUI.SetResult("인게임 환경을 불러오는 중...");

        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        while (!op.isDone) yield return null;

        WorldLoader.Instance.InitializeWorld(
            data.loginData.MapId,
            data.loginData.Position,
            data.OtherPlayers,
            data.LoadEnemies
        );

        SingletonManager.Instance.InitializeIngame();

        yield return new WaitForSeconds(0.5f);
    }
}
