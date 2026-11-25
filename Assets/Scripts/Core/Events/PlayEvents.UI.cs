using System;
using UnityEngine;
using EverWind.Gameplay;

public enum InGamePanel { None, Inventory, Craft, Quest, Dead }
public enum UICommandKind { Skill, UseItem, Unequip, Craft, ClaimQuest, Revive, Logout }
public enum UISlotKind { Inventory, Equipment, Recipe, Ingredient, Quest }
public enum UIProfileState { Normal, Hit, Success, Hurt }

public struct UICommand { public UICommandKind Kind; public int Id; }
public struct UIItemData
{
    public int Id, Count, Slot;
    public string Name, Description;
    public Sprite Icon;
    public bool Usable;
}
public struct UISkillData
{
    public int Id, Rank;
    public string Name;
    public float Remaining, Duration;
    public bool Available;
}
public struct UIRecipeData
{
    public int Id;
    public string Name;
    public Sprite Icon;
    public bool Available;
    public UIItemData[] Ingredients;
}
public struct UIQuestData
{
    public int Id;
    public string Name, Description;
    public bool Complete, Claimed;
}
public struct UIEnemyData
{
    public int Id;
    public string Name;
    public float Hp, MaxHp;
    public Vector3 ScreenPosition, Position;
}
// UI에는 표시값의 복사본만 전달한다. 개체/게임 매니저 참조는 전달하지 않는다.
public sealed class InGameUIState
{
    public bool Ready, Dead;
    public float Hp, MaxHp, ReviveRemaining;
    public int Level, Experience, RequiredExperience, SelectedTarget, MapId;
    public Vector3 PlayerPosition, MapCenter;
    public float MapRadius;
    public int TutorialPhase = -1;
    public bool TutorialCleared;
    public UIItemData[] Items = Array.Empty<UIItemData>(), Equipment = Array.Empty<UIItemData>();
    public UISkillData[] Skills = Array.Empty<UISkillData>();
    public UIRecipeData[] Recipes = Array.Empty<UIRecipeData>();
    public UIQuestData[] Quests = Array.Empty<UIQuestData>();
    public UIEnemyData[] Enemies = Array.Empty<UIEnemyData>();
}
public struct UISlotData
{
    public UISlotKind Kind;
    public int Key, CommandId;
    public string Name, Description, Count;
    public Sprite Icon;
    public bool Interactable;
}
public struct UIControlState { public string Control; public int Value; public bool Enabled; }
public struct UITooltipData { public string Title, Description; public Vector2 ScreenPosition; public bool Visible; }
public struct UIEffectRequest { public Effect Prefab; public Vector2 ScreenPosition; public float Scale; }
public struct UIDamageData { public Vector3 ScreenPosition; public int Amount; }

public static partial class PlayEvents
{
    public static event Action<UIProfileState, float> UIProfileChanged;
    public static void EvUIProfile(UIProfileState state, float duration) => UIProfileChanged?.Invoke(state, duration);
    public static event Action UIStateRequested;
    public static event Action<InGameUIState> UIStateChanged;
    public static event Action<UICommand> UICommandRequested;
    public static event Action<string> UIControlActivationRequested;
    public static event Action<UIControlState> UIControlStateChanged;
    public static event Action<InGamePanel> UIPanelRequested;
    public static event Action<InGamePanel> UIPanelChanged;
    public static event Action<UISlotData> UISlotChanged;
    public static event Action<int> UIRecipeSelected;
    public static event Action<UITooltipData> UITooltipChanged;
    public static event Action<UIEffectRequest> UIEffectRequested;
    public static event Action<Effect, EffectPlayInfo> UIEffectPlayRequested;
    public static event Action<UIDamageData> UIDamageShown;
    public static event Action<string> UINotice;
    public static void EvUIStateRequested() => UIStateRequested?.Invoke();
    public static void EvUIStateChanged(InGameUIState state) => UIStateChanged?.Invoke(state ?? new InGameUIState());
    public static void EvUICommand(UICommandKind kind, int id = 0) => UICommandRequested?.Invoke(new UICommand { Kind = kind, Id = id });
    public static void EvUIActivate(string control) => UIControlActivationRequested?.Invoke(control);
    public static void EvUIControlState(UIControlState state) => UIControlStateChanged?.Invoke(state);
    public static void EvUIPanelRequested(InGamePanel panel) => UIPanelRequested?.Invoke(panel);
    public static void EvUIPanelChanged(InGamePanel panel) => UIPanelChanged?.Invoke(panel);
    public static void EvUISlot(UISlotData data) => UISlotChanged?.Invoke(data);
    public static void EvUIRecipeSelected(int id) => UIRecipeSelected?.Invoke(id);
    public static void EvUITooltip(UITooltipData data) => UITooltipChanged?.Invoke(data);
    public static void EvUIEffect(UIEffectRequest request) => UIEffectRequested?.Invoke(request);
    public static void EvUIEffectPlay(Effect prefab, EffectPlayInfo info) => UIEffectPlayRequested?.Invoke(prefab, info);
    public static void EvUIDamage(UIDamageData data) => UIDamageShown?.Invoke(data);
    public static void EvUINotice(string text) => UINotice?.Invoke(text);
}
