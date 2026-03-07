using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using DG.Tweening;

public class DeadUI : MonoBehaviour
{
    public Button revive;
    public Button quit;
    public TextMeshProUGUI introductionDead;

    private float _timer;
    private bool _isCountingDown = false;

    public event Action OnRevived;

    private CanvasGroup _canvasGroup;

    [SerializeField]
    private float _fadeDuration;

    public void Init()
    {
        _canvasGroup = this.GetComponent<CanvasGroup>();
        _canvasGroup.alpha = 0f;
        _fadeDuration = 1.0f;
    }

    public void ShowDeadUI()
    {
        gameObject.SetActive(true);
        _canvasGroup.DOKill();
        _canvasGroup.alpha = 0f;
        _canvasGroup.DOFade(1f, _fadeDuration).SetUpdate(true);

        _timer = 15f;
        _isCountingDown = true;

        if (revive != null)
        {
            revive.interactable = false;
        }
    }

    void Update()
    {
        if (_isCountingDown)
        {
            if (_timer > 0)
            {
                _timer -= Time.deltaTime;
                introductionDead.text = $"당신은 죽었습니다.\n{Mathf.CeilToInt(_timer)}초 후에 살아날 수 있습니다...";
            }
            else
            {
                FinishCountdown();
            }
        }
    }

    void FinishCountdown()
    {
        _isCountingDown = false;
        _timer = 0;
        introductionDead.text = "이제 부활할 수 있습니다!\n포기하지마세요.";

        if (revive != null)
        {
            revive.interactable = true;
        }
    }

    public void OnRevive()
    {
        var popUpUIManager = GetComponentInParent<PopUpUIManager>();
        if (popUpUIManager != null)
        {
            popUpUIManager.CloseDeadUI();
        }

        OnRevived?.Invoke();
    }
}