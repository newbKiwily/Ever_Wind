using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class QuestUI : MonoBehaviour
{
    [SerializeField]
    private ScrollRect _scrollRect;

    [SerializeField]
    private Scrollbar _verticalScrollbar;

    [SerializeField]
    private RectTransform _scrollArea;

    [SerializeField]
    private RectTransform _contentParent;

    [SerializeField]
    private GridLayoutGroup _gridLayoutGroup;

    [SerializeField]
    private float _scrollSensitivity = 0.08f;

    [SerializeField]
    private bool _hideVerticalScrollbar = true;

    [SerializeField]
    private GameObject _contentPrefab;

    private readonly List<QuestContentUI> _questContents = new List<QuestContentUI>();
    private bool _isInitialized;

    public RectTransform ContentParent => _contentParent;

    private Camera _eventCamera;

    public void Init()
    {
        if (_scrollRect == null)
            _scrollRect = GetComponentInChildren<ScrollRect>(true);

        if (_scrollRect != null)
        {
            if (_verticalScrollbar == null)
                _verticalScrollbar = _scrollRect.verticalScrollbar;

            if (_scrollArea == null)
                _scrollArea = _scrollRect.GetComponent<RectTransform>();

            if (_contentParent == null && _scrollRect.content != null)
                _contentParent = _scrollRect.content;
        }

        if (_gridLayoutGroup == null && _contentParent != null)
            _gridLayoutGroup = _contentParent.GetComponent<GridLayoutGroup>();

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            _eventCamera = null;
        }
        else
        {
            _eventCamera = canvas.worldCamera != null 
                ?canvas.worldCamera : Camera.main;
        }
        SetupVerticalScroll();
        _isInitialized = true;
        RefreshQuestContents();
    }

    private void OnEnable()
    {
        UIEvents.OnQuestProgressChanged += RefreshQuestContents;
    }

    private void Update()
    {
        if (_scrollRect == null || _scrollArea == null)
        {
            UIEvents.SetPointerOverQuestScroll(false);
            return;
        }

        bool isPointerOverScrollArea = RectTransformUtility.RectangleContainsScreenPoint(_scrollArea, Input.mousePosition, _eventCamera);
        UIEvents.SetPointerOverQuestScroll(isPointerOverScrollArea);

        float wheelDelta = Input.mouseScrollDelta.y;
        if (Mathf.Approximately(wheelDelta, 0f))
            return;

        if (!isPointerOverScrollArea)
            return;

        float nextPosition = _scrollRect.verticalNormalizedPosition + wheelDelta * _scrollSensitivity;
        _scrollRect.verticalNormalizedPosition = Mathf.Clamp01(nextPosition);
    }

    private void OnDisable()
    {
        UIEvents.OnQuestProgressChanged -= RefreshQuestContents;
        UIEvents.SetPointerOverQuestScroll(false);
    }

    

    private void SetupVerticalScroll()
    {
        if (_scrollRect != null)
        {
            _scrollRect.horizontal = false;
            _scrollRect.vertical = true;
            _scrollRect.horizontalScrollbar = null;
            _scrollRect.scrollSensitivity = 0f;

            if (_contentParent != null)
                _scrollRect.content = _contentParent;

            if (_verticalScrollbar != null)
                _scrollRect.verticalScrollbar = _verticalScrollbar;
        }

        if (_verticalScrollbar != null)
        {
            _verticalScrollbar.direction = Scrollbar.Direction.BottomToTop;
            _verticalScrollbar.interactable = false;
            SetVerticalScrollbarVisible(!_hideVerticalScrollbar);
        }

    }

    private void SetVerticalScrollbarVisible(bool isVisible)
    {
        if (_verticalScrollbar == null)
            return;

        CanvasGroup scrollbarCanvasGroup = _verticalScrollbar.GetComponent<CanvasGroup>();
        if (scrollbarCanvasGroup == null)
            scrollbarCanvasGroup = _verticalScrollbar.gameObject.AddComponent<CanvasGroup>();

        scrollbarCanvasGroup.alpha = isVisible ? 1f : 0f;
        scrollbarCanvasGroup.interactable = false;
        scrollbarCanvasGroup.blocksRaycasts = false;
    }

    public void RefreshQuestContents()
    {
        if (!_isInitialized || _contentParent == null || _contentPrefab == null)
            return;

        QuestManager questManager = SingletonManager.Instance.GetSingleton<QuestManager>();
        if (questManager == null)
            return;

        ClearQuestContents();

        IReadOnlyList<QuestManager.QuestProgressData> activeQuests = questManager.ActiveQuests;
        for (int i = 0; i < activeQuests.Count; i++)
        {
            if (activeQuests[i] == null || activeQuests[i].RewardClaimed)
                continue;

            GameObject questContentObject = Instantiate(_contentPrefab, _contentParent);
            QuestContentUI questContentUI = questContentObject.GetComponent<QuestContentUI>();
            if (questContentUI == null)
                questContentUI = questContentObject.AddComponent<QuestContentUI>();

            questContentUI.Init(activeQuests[i]);
            _questContents.Add(questContentUI);
        }
    }

    private void ClearQuestContents()
    {
        for (int i = 0; i < _questContents.Count; i++)
        {
            if (_questContents[i] != null)
                Destroy(_questContents[i].gameObject);
        }

        _questContents.Clear();
    }

}
