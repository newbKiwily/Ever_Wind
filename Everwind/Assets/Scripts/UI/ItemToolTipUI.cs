using UnityEngine;
using TMPro;
using DG.Tweening; // DOTween 추가

public class ItemTooltip : MonoBehaviour
{
    public GameObject tooltipWindow;
    private CanvasGroup _canvasGroup; // Alpha 제어용
    [SerializeField]
    private TextMeshProUGUI _titleText;
    [SerializeField]
    private TextMeshProUGUI _descriptionText;
    [SerializeField]
    private float _fadeDuration;

    public void Init()
    {
        _canvasGroup = tooltipWindow.GetComponent<CanvasGroup>();
        _canvasGroup.alpha = 0f;
        _fadeDuration = 0.4f;
    }

    public void ShowTooltip(string title, string description)
    {
        _canvasGroup.DOKill();

        _titleText.text = title;
        _descriptionText.text = description;

        tooltipWindow.transform.position = Input.mousePosition;
        tooltipWindow.SetActive(true);

        _canvasGroup.DOFade(1f, _fadeDuration).SetUpdate(true);
    }

    public void HideTooltip()
    {
        _canvasGroup.DOKill();
        _canvasGroup.alpha = 0f;
        tooltipWindow.SetActive(false);
    }

    private void Update()
    {
        if (tooltipWindow.activeSelf)
        {
            tooltipWindow.transform.position = Input.mousePosition;
        }
    }
}
