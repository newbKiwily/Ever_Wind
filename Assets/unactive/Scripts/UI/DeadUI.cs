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
        if (EverWind.Gameplay.GameplayIntegration.Active != null) {
            _timer=EverWind.Gameplay.GameplayIntegration.Active.ReviveRemaining;
            introductionDead.text=_timer>0 ? $"사망했습니다.\n{Mathf.CeilToInt(_timer)}초 후 부활할 수 있습니다." : "지금 부활할 수 있습니다.";
            if(revive!=null)revive.interactable=_timer<=0;
            return;
        }
        if (_isCountingDown)
        {
            if (_timer > 0)
            {
                _timer -= Time.deltaTime;
                introductionDead.text = $"당신을 죽었습니다.\n{Mathf.CeilToInt(_timer)}초 후에 부활할 수 있습니다";
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
        introductionDead.text = "이제 부활할 수 있습니다";

        if (revive != null)
        {
            revive.interactable = true;
        }
    }

    public void OnRevive()
    {
        if (EverWind.Gameplay.GameplayIntegration.Active != null) { EverWind.Gameplay.GameplayIntegration.Active.Revive(); return; }
        var popUpUIManager = GetComponentInParent<PopUpUIManager>();
        if (popUpUIManager != null)
        {
            popUpUIManager.CloseDeadUI();
        }

        UIEvents.EvReviveRequested();
        OnRevived?.Invoke();
    }
    private void OnDisable(){if(!ReferenceEquals(_canvasGroup,null))DOTween.Kill(_canvasGroup);}
}

