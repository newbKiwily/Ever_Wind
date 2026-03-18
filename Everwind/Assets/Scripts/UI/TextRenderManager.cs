using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using DG.Tweening; // ?꾩닔

public class TextRenderManager : SingletonBase<TextRenderManager>
{
    public override bool IsPersistent => false;

    public TextMeshProUGUI TextMeshProUGUI;
    public GameObject DialoguePanel;
    public CanvasGroup CanvasGroup; 
    public float FadeDuration = 0.5f;

    private Dictionary<string, string[]> _textData = new Dictionary<string, string[]>();
    private string _currentTextId = string.Empty;
    private int _currentIndex = -1;
    private Coroutine _activeTextCoroutine;

    protected override void Awake()
    {
        Priority = 20;
        base.Awake();

        if (CanvasGroup == null) CanvasGroup = DialoguePanel.GetComponent<CanvasGroup>();
    }

    private void Start()
    {
        CanvasGroup.alpha = 0f;
        TextMeshProUGUI.text = "";

        _textData.Add("CameraT", new string[]
        {
            "안녕하세요. 여긴 튜토리얼 필드입니다. ",
            "마우스를 우클릭을 누른 상태로 자유롭게 드래그 해보세요.",
            "잘했습니다.",
            "아무 키를 누르시면 다음 튜토리얼로 넘어갑니다."
        });

        _textData.Add("MoveT", new string[]
        {
            "이동 튜토리얼을 시작하겠습니다.",
            "마우스 우클릭으로 카메라를 제어하고,W,A,S,D를 이용하여 빨간색원으로 이동해보세요.",
            "잘했습니다.",
            "아무 키를 누르시면 다음 튜토리얼로 넘어갑니다."
        });

        _textData.Add("CombatT", new string[]
        {
            "전투 튜토리얼을 시작하겠습니다.",
            "마우스를 좌클릭하고 1번 또는 2번을 눌러보세요.",
            "우클릭을 하면 전투상태에 돌입하고 공격할 수 있습니다.",
            "전투 상태에서만 공격할 수 있다는 점을 기억하세요!",
            "이제 스켈레톤과 스파이더를 공격하여 쓰려트려 보세요!",
            "참고로 E키를 누르면 현재 타겟팅을 변경할 수 있습니다!",
            "잘했습니다",
            "아무 키를 누르시면 다음 튜토리얼로 넘어갑니다."
        });

        _textData.Add("TargetingT", new string[]
        {
            "타겟팅 튜토리얼을 시작하겠습니다.",
            "적들 아래의 빨간 원은 현재 타겟팅 상태인 적을 표시합니다.",
            "E키를 눌러 타겟팅을 변경하고 공격도 해보세요.",
            "잘했습니다",
            "아무 키를 누르시면 다음 튜토리얼로 넘어갑니다."
        });

        _textData.Add("InteractT", new string[]
        {
            "채집 튜토리얼을 시작하겠습니다.",
            "스페이스 바를 눌러 근처의 채집물을 자동으로 탐색하고 채집할 수 있습니다.",
            "채집물 근처에서 스페이스바를 눌러보세요",
            "잘했습니다.",
            "여러 채집물이 있을 시 가장 가까운 채집물부터 채집되는 점을 기억하세요!",
            "주변의 채집물도 채집해보세요!",
            "아무 키를 누르시면 다음 튜토리얼로 넘어갑니다"
        });

        _textData.Add("CraftT", new string[]
        {
            "제작 튜토리얼을 시작하겠습니다.",
            "C키(예시)를 눌러 제작 UI를 열어보세요.",
            "원하는 아이템을 선택하고 재료가 충분하다면 제작이 가능합니다.",
            "잘했습니다!",
            "부족한 재료는 빨간색으로 표시됩니다.",
            "필요한 만큼 제작해보세요!",
            "아무 키나 누르면 다음으로 넘어갑니다."
        });

        _textData.Add("EquipT", new string[]
        {
            "장비 튜토리얼을 시작하겠습니다.",
            "제작을 통하여 방어구를 하나 만들어보세요.",
            "그 후에 I를 눌러 장비아이템을 클릭하여 보세요.",
            "잘했습니다!",
            "장비 아이템을 장착하여 캐릭터를 성장시킬 수 있습니다.",
            "튜토리얼을 종료합니다."
        });
    }

    public void StartShow(string textId)
    {
        if (_activeTextCoroutine != null) StopCoroutine(_activeTextCoroutine);

        if (!_textData.ContainsKey(textId)) return;

        _currentTextId = textId;
        _currentIndex = 0;
        _activeTextCoroutine = StartCoroutine(ShowText(_textData[textId][_currentIndex]));
    }

    private IEnumerator ShowText(string textToDisplay)
    {
        if (CanvasGroup.alpha > 0)
        {
            yield return CanvasGroup.DOFade(0f, FadeDuration * 0.4f).WaitForCompletion();
        }

        TextMeshProUGUI.text = textToDisplay;

        // 3. ?섏씠????+ ?꾨즺 ??肄쒕갚 ?덉떆 (濡쒓렇 異쒕젰)
        yield return CanvasGroup.DOFade(1f, FadeDuration)
            .OnComplete(() => Debug.Log($"{textToDisplay} ?쒖떆 ?꾨즺!"))
            .WaitForCompletion();

        _activeTextCoroutine = null;
    }

    public void AutoShow(int fromIndex, int toIndex)
    {
        if (_activeTextCoroutine != null) StopCoroutine(_activeTextCoroutine);

        string[] texts = _textData[_currentTextId];
        _activeTextCoroutine = StartCoroutine(AutoShowSequence(texts, fromIndex, toIndex, 2.0f));
    }

    private IEnumerator AutoShowSequence(string[] texts, int start, int end, float displayTime)
    {
        for (int i = start; i <= end; i++)
        {
            _currentIndex = i;
            TextMeshProUGUI.text = texts[i];

            // ?섏씠????
            yield return CanvasGroup.DOFade(1f, FadeDuration).WaitForCompletion();

            // ?좎?
            yield return new WaitForSecondsRealtime(displayTime);

            // ?섏씠???꾩썐 (留덉?留?臾몄옣???꾨땺 ?뚮쭔 ?뱀? 痍⑦뼢猿?
            yield return CanvasGroup.DOFade(0f, FadeDuration)
                .OnComplete(() => {
                    // ?섏씠???꾩썐 ?꾨즺 ??肄쒕갚?쇰줈 ?ㅼ쓬 ?④퀎 以鍮?媛??
                    TextMeshProUGUI.text = "";
                })
                .WaitForCompletion();
        }
        _activeTextCoroutine = null;
    }

    public bool IsDoneShowingText()
    {
        return CanvasGroup.alpha <= 0.01f && _activeTextCoroutine == null;
    }
}
