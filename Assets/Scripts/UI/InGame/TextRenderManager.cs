using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using DG.Tweening;

public sealed class TextRenderManager : MonoBehaviour
{
    public TextMeshProUGUI TextMeshProUGUI;
    public GameObject DialoguePanel;
    public CanvasGroup CanvasGroup;
    public float FadeDuration = .5f;

    private readonly Dictionary<string, string[]> _textData = new Dictionary<string, string[]>();
    private string _currentTextId = string.Empty;
    private int _currentIndex = -1;
    private Coroutine _activeTextCoroutine;
    private bool _playing;
    private int _phase = -1;
    private bool _cleared;
    private static readonly string[] StepIds = { "CameraT", "MoveT", "CombatT", "InteractT", "CraftT", "EquipT" };
    private static readonly int[] ClearIndices = { 2, 2, 6, 3, 3, 3 };

    private void Awake()
    {
        if (CanvasGroup == null && DialoguePanel != null) CanvasGroup = DialoguePanel.GetComponent<CanvasGroup>();
        BuildTextData();
        ResetPresentation();
    }
    private void BuildTextData()
    {
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
            "마우스 우클릭으로 카메라를 제어하고,W,A,S,D를 이용하여 이동 목표 지점으로 이동해보세요.",
            "잘했습니다.",
            "아무 키를 누르시면 다음 튜토리얼로 넘어갑니다."
        });

        _textData.Add("CombatT", new string[]
        {
           "전투 튜토리얼을 시작하겠습니다.",
            "마우스를 좌클릭하고 습득한 스킬의 숫자 키를 눌러보세요.",
            "좌클릭을 하면 전투상태에 돌입하고 공격할 수 있습니다.",
            "전투 상태에서만 공격할 수 있다는 점을 기억하세요!",
            "이제 스켈레톤과 스파이더를 공격하여 쓰러트려 보세요!",
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
            "C키를 눌러 제작 UI를 열어보세요.",
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

    private void OnEnable() { PlayEvents.UIStateChanged += Apply; PlayEvents.EvUIStateRequested(); }
    private void OnDisable() { PlayEvents.UIStateChanged -= Apply; ResetPresentation(); }

    private void Apply(InGameUIState state)
    {
        int phase = state.TutorialPhase;
        if (!state.Ready || state.Dead || phase < 0 || phase >= StepIds.Length)
        {
            if (_phase >= 0)
            {
                _phase = -1;
                CancelPlayback();
                _playing = true;
                _activeTextCoroutine = StartCoroutine(FadeOut());
            }
            return;
        }
        if (_phase == phase && _cleared == state.TutorialCleared) return;
        _phase = phase; _cleared = state.TutorialCleared;
        StartShow(StepIds[phase]);
        AutoShow(_cleared ? ClearIndices[phase] : 0, _cleared ? _textData[_currentTextId].Length - 1 : ClearIndices[phase] - 1);
    }

    public void StartShow(string textId)
    {
        if (_textData.Count == 0) BuildTextData();
        if (!_textData.ContainsKey(textId)) return;
        CancelPlayback();
        _currentTextId = textId; _currentIndex = 0;
        DialoguePanel.SetActive(true); _playing = true;
        _activeTextCoroutine = StartCoroutine(ShowText(_textData[textId][0]));
    }
    public void AutoShow(int fromIndex, int toIndex)
    {
        if (!_textData.TryGetValue(_currentTextId, out var texts)) return;
        if (fromIndex < 0 || toIndex < fromIndex || toIndex >= texts.Length) throw new ArgumentOutOfRangeException(nameof(fromIndex));
        CancelPlayback(); DialoguePanel.SetActive(true); _playing = true;
        _activeTextCoroutine = StartCoroutine(AutoShowSequence(texts, fromIndex, toIndex, 2f));
    }
    private IEnumerator ShowText(string text)
    {
        if (CanvasGroup.alpha > 0) yield return Fade(0, FadeDuration * .4f).WaitForCompletion();
        TextMeshProUGUI.text = text;
        yield return Fade(1, FadeDuration).WaitForCompletion();
        _playing = false; _activeTextCoroutine = null;
    }
    private Tween Fade(float alpha, float duration) => CanvasGroup.DOFade(alpha, Mathf.Max(.001f, duration)).SetUpdate(true);
    private IEnumerator AutoShowSequence(string[] texts, int start, int end, float displayTime)
    {
        if (CanvasGroup.alpha > 0) yield return Fade(0, FadeDuration * .4f).WaitForCompletion();
        for (int i = start; i <= end; i++)
        {
            _currentIndex = i; TextMeshProUGUI.text = texts[i];
            yield return Fade(1, FadeDuration).WaitForCompletion();
            yield return new WaitForSecondsRealtime(displayTime);
            yield return Fade(0, FadeDuration).WaitForCompletion();
            TextMeshProUGUI.text = "";
        }
        _playing = false; _activeTextCoroutine = null;
    }
    private IEnumerator FadeOut()
    {
        yield return Fade(0, FadeDuration).WaitForCompletion();
        TextMeshProUGUI.text = ""; DialoguePanel.SetActive(false);
        _playing = false; _activeTextCoroutine = null;
    }
    public bool IsDoneShowingText() => !_playing && (CanvasGroup == null || CanvasGroup.alpha <= .01f);
    public bool HasFinishedStage(int phase, bool cleared) => _phase == phase && _cleared == cleared && IsDoneShowingText();
    private void CancelPlayback()
    {
        if (_activeTextCoroutine != null) StopCoroutine(_activeTextCoroutine);
        _activeTextCoroutine = null; _playing = false;
        if (CanvasGroup != null) DOTween.Kill(CanvasGroup);
    }
    private void ResetPresentation()
    {
        CancelPlayback(); _phase = -1; _cleared = false;
        if (CanvasGroup != null) CanvasGroup.alpha = 0;
        if (TextMeshProUGUI != null) TextMeshProUGUI.text = "";
        if (DialoguePanel != null) DialoguePanel.SetActive(false);
    }
}

