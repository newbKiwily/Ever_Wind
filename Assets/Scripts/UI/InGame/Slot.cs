using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
public abstract class Slot : UIControlButton, IPointerEnterHandler, IPointerExitHandler
{
 public UISlotKind Kind; public int Key;
 public UnityEngine.UI.Image ItemImage; public TMP_Text CountText, NameText;
 protected UISlotData data;
 protected override void OnEnable(){base.OnEnable();PlayEvents.UISlotChanged+=Apply;}
 protected override void OnDisable(){base.OnDisable();PlayEvents.UISlotChanged-=Apply;PlayEvents.EvUITooltip(default);}
 void Apply(UISlotData value){if(value.Kind!=Kind||value.Key!=Key)return;data=value;Value=value.CommandId;if(ItemImage!=null){ItemImage.sprite=value.Icon;ItemImage.enabled=value.Icon!=null;}if(CountText!=null)CountText.text=value.Count;if(NameText!=null)NameText.text=value.Name;_button.interactable=value.Interactable;}
 public void OnPointerEnter(PointerEventData e){if(!string.IsNullOrEmpty(data.Name))PlayEvents.EvUITooltip(new UITooltipData{Visible=true,Title=data.Name,Description=data.Description,ScreenPosition=e.position});}
 public void OnPointerExit(PointerEventData e){PlayEvents.EvUITooltip(default);}
}
