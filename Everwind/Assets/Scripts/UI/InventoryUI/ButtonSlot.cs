using UnityEngine.UI;

public abstract class ButtonSlot : Slot
{
    protected Button _button;

    protected override void Awake()
    {
        _button = GetComponent<Button>();
        if (_button != null)
            _button.onClick.AddListener(OnClick);
    }

    protected virtual void OnDestroy()
    {
        if (_button != null)
            _button.onClick.RemoveListener(OnClick);
    }
    protected abstract void OnClick();
}