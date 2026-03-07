using UnityEngine;
using UnityEngine.EventSystems;

public abstract class Slot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private ItemTooltip ItemTooltip => SingletonManager.Instance.GetSingleton<PopUpUIManager>().ItemTooltip;
    protected InventoryItem _item;

    protected abstract void Awake();

    public virtual void OnPointerEnter(PointerEventData eventData)
    {
        if (_item != null)
        {
            ItemTooltip.ShowTooltip(_item.ItemName, _item.ItemInstruction);
        }
    }

    public virtual void OnPointerExit(PointerEventData eventData)
    {
        ItemTooltip.HideTooltip();
    }

    public virtual InventoryItem GetInventoryItem()
    {
        return _item;
    }
}