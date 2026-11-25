using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
public sealed class SkillButton : UIControlButton, IPointerEnterHandler, IPointerExitHandler
{
 public Image CooldownImage; public TMP_Text Label; bool available; float endsAt,duration; string skillName; int rank;
 protected override void OnEnable(){base.OnEnable();PlayEvents.UIStateChanged+=Apply;}
 protected override void OnDisable(){base.OnDisable();PlayEvents.UIStateChanged-=Apply;PlayEvents.EvUITooltip(default);endsAt=duration=0;if(CooldownImage!=null)CooldownImage.enabled=false;}
 void Apply(InGameUIState state){available=false;skillName=null;endsAt=duration=0;Label.text=Value.ToString();foreach(var skill in state.Skills)if(skill.Id==Value){available=skill.Available;endsAt=Time.unscaledTime+skill.Remaining;duration=skill.Duration;skillName=skill.Name;rank=skill.Rank;break;}Update();}
 void Update(){float remaining=Mathf.Max(0,endsAt-Time.unscaledTime);if(CooldownImage!=null){CooldownImage.enabled=remaining>0&&duration>0;CooldownImage.fillAmount=duration>0?Mathf.Clamp01(remaining/duration):0;}if(_button!=null)_button.interactable=available&&remaining<=0;}
 protected override void OnClick(){PlayEvents.EvUICommand(UICommandKind.Skill,Value);}
 public void OnPointerEnter(PointerEventData e){if(!string.IsNullOrEmpty(skillName))PlayEvents.EvUITooltip(new UITooltipData{Visible=true,Title=skillName,Description="랭크 "+rank+"\n단축키 "+Value,ScreenPosition=e.position});}
 public void OnPointerExit(PointerEventData e){PlayEvents.EvUITooltip(default);}
}
