#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using EverWind.Gameplay;

public sealed class InGameUITestHarness : MonoBehaviour
{
    public InGameUIState Sample;
    public bool Finished;
    public string Result;
    public int Checks;
    int commands, effects;
    UICommand lastCommand;
    EffectManager manager;
    void Check(bool condition, string name) { if (!condition) throw new Exception(name); ++Checks; }
    void Publish() { if (Sample != null) PlayEvents.EvUIStateChanged(Sample); }
    void Command(UICommand value) { ++commands; lastCommand = value; }
    void EffectRequested(UIEffectRequest value) { ++effects; }
    void OnEnable() { PlayEvents.UIStateRequested += Publish; PlayEvents.UICommandRequested += Command; PlayEvents.UIEffectRequested += EffectRequested; }
    void OnDisable() { PlayEvents.UIStateRequested -= Publish; PlayEvents.UICommandRequested -= Command; PlayEvents.UIEffectRequested -= EffectRequested; }
    IEnumerator Start()
    {
        var icon = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UIImage/slot.png");
        Sample = new InGameUIState
        {
            Ready = true, Hp = 75, MaxHp = 100, Level = 3, Experience = 18, RequiredExperience = 50, MapRadius = 80,
            Items = new[] { new UIItemData { Id = 1, Name = "나뭇가지", Description = "제작에 사용하는 재료입니다.", Count = 12, Icon = icon }, new UIItemData { Id = 2, Name = "회복 물약", Description = "생명력을 회복합니다.", Count = 3, Icon = icon, Usable = true } },
            Equipment = new[] { new UIItemData { Id = 3, Slot = 1003, Name = "철검", Icon = icon } },
            Skills = new[] { new UISkillData { Id = 1, Name = "일반 공격", Available = true, Rank = 1, Duration = 1 }, new UISkillData { Id = 2, Name = "스매시", Available = true, Rank = 1 }, new UISkillData { Id = 3, Name = "범위 공격", Available = true, Rank = 1 } },
            Recipes = new[] { new UIRecipeData { Id = 10, Name = "모닥불", Available = true, Icon = icon, Ingredients = new[] { new UIItemData { Id = 1, Name = "나뭇가지", Count = 3, Description = "보유 12 / 필요 3", Icon = icon } } } },
            Quests = new[] { new UIQuestData { Id = 20, Name = "첫 번째 전투", Description = "주변의 몬스터를 처치하세요." }, new UIQuestData { Id = 21, Name = "재료 모으기", Description = "제작 재료를 모았습니다.", Complete = true } }
        };
        yield return null;
        Publish();
        yield return null;
        try
        {
            Check(FindObjectsByType<UnityEngine.EventSystems.EventSystem>(FindObjectsSortMode.None).Length == 1, "one EventSystem");
            Check(FindObjectsByType<Canvas>(FindObjectsSortMode.None).All(canvas => canvas.renderMode == RenderMode.ScreenSpaceCamera), "camera canvas");
            Check(FindObjectsByType<TMPro.TMP_Text>(FindObjectsSortMode.None).All(text => text.font != null && text.font.name.Contains("NotoSansKR")), "NotoSansKR");
            manager = new GameObject("Test Effect Manager").AddComponent<EffectManager>();
            var source = new GameObject("Test Click Effect"); source.SetActive(false);
            var particles = source.AddComponent<ParticleSystem>(); var main = particles.main; main.loop = false; main.duration = .1f; main.startLifetime = .05f; main.startSize = .3f;
            var effect = source.AddComponent<OneShotEffect>();
            var skill = FindObjectsByType<SkillButton>(FindObjectsSortMode.None).First(button => button.Value == 1);
            skill.InteractionEffect = effect;
            PlayEvents.EvUIActivate("skill:1");
            Check(commands == 1 && lastCommand.Kind == UICommandKind.Skill && lastCommand.Id == 1, "keyboard routes once");
            Check(effects == 1 && manager.ActiveEffectCount == 1, "keyboard requests pooled effect once");
            Check(manager.GetEffectPoolSize(effect) == effect.InitialPoolSize, "prewarm limit");
            var playing = FindObjectsByType<OneShotEffect>(FindObjectsSortMode.None).First(value => value != effect && value.IsPlaying);
            Check(playing.gameObject.layer == 5 && playing.UseUnscaledTime, "UI layer and unscaled playback");
            skill.GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            Check(commands == 2 && effects == 2, "mouse uses same activation path");
            Sample.Skills[0].Remaining = 1; Publish(); skill.Activate();
            Check(commands == 2 && effects == 2, "disabled button rejects both command and effect");
            PlayEvents.EvUIActivate("inventory"); Publish();
            Check(FindObjectsByType<InventorySlot>(FindObjectsSortMode.None).Length == 2, "inventory populated");
            var item = FindObjectsByType<InventorySlot>(FindObjectsSortMode.None).First(slot => slot.Key == 2); item.Activate();
            Check(lastCommand.Kind == UICommandKind.UseItem && lastCommand.Id == 2, "item command event");
            PlayEvents.EvUIActivate("craft"); Publish();
            Check(FindObjectsByType<CraftSlot>(FindObjectsSortMode.None).Length == 1 && FindObjectsByType<IngredientSlot>(FindObjectsSortMode.None).Length == 1, "recipe and ingredient views");
            PlayEvents.EvUIActivate("craft:execute");
            Check(lastCommand.Kind == UICommandKind.Craft && lastCommand.Id == 10, "craft request event");
            PlayEvents.EvUIActivate("quest"); Publish();
            Check(FindObjectsByType<QuestContentUI>(FindObjectsSortMode.None).Length == 2, "quest view populated");
            FindObjectsByType<QuestContentUI>(FindObjectsSortMode.None).First(slot => slot.Key == 21).Activate();
            Check(lastCommand.Kind == UICommandKind.ClaimQuest && lastCommand.Id == 21, "claim request event");
            int before = commands; FindObjectsByType<QuestContentUI>(FindObjectsSortMode.None).First(slot => slot.Key == 20).Activate();
            Check(commands == before, "incomplete quest blocked");
            Sample.Dead = true; Sample.ReviveRemaining = 3; Publish();
            var revive = FindObjectsByType<UIControlButton>(FindObjectsSortMode.None).First(button => button.ControlId == "revive");
            Publish(); revive.Activate(); Check(commands == before, "revive delay blocked");
            Sample.ReviveRemaining = 0; Publish(); revive.Activate(); Check(lastCommand.Kind == UICommandKind.Revive, "revive request event");
            Sample.Dead = false; Publish();
            Sample.Items = new[] { Sample.Items[1] }; PlayEvents.EvUIActivate("inventory"); Publish();
            Check(FindObjectsByType<InventorySlot>(FindObjectsSortMode.None).Length == 1, "removed items hidden");
            PlayEvents.EvUIActivate("inventory"); PlayEvents.EvUIActivate("inventory"); Publish();
            Check(FindObjectsByType<InventorySlot>(FindObjectsSortMode.None).Length == 1, "reopen initial snapshot");
            Result = "PASS: " + Checks + " interaction/event checks";
        }
        catch (Exception e) { Result = "FAIL: " + e; Debug.LogError(Result); Finished = true; yield break; }
        yield return new WaitForSecondsRealtime(1.2f);
        try
        {
            Check(manager.ActiveEffectCount == 0, "effect returned to pool");
            var skill = FindObjectsByType<SkillButton>(FindObjectsSortMode.None).First(button => button.Value == 1);
            var effect = skill.InteractionEffect;
            int size = manager.GetEffectPoolSize(effect);
            var world = manager.PlayEffect(effect, new EffectPlayInfo(Vector3.zero, Quaternion.identity));
            Check(manager.ActiveEffectCount == 1 && manager.GetEffectPoolSize(effect) == size, "reuse without allocating");
            var playing = FindObjectsByType<OneShotEffect>(FindObjectsSortMode.None).First(value => value.IsPlaying);
            Check(playing.gameObject.layer == 0, "world playback restores original layer");
            world.Release();
            Check(manager.ActiveEffectCount == 0, "explicit release");
            int before = commands;
            var canvas = FindFirstObjectByType<InGameUIController>();
            canvas.gameObject.SetActive(false);
            PlayEvents.EvUIActivate("skill:1");
            Check(commands == before, "disabled UI unsubscribes");
            Sample.Skills[0].Remaining = 0;
            canvas.gameObject.SetActive(true); Publish();
            PlayEvents.EvUIPanelRequested(InGamePanel.None);
            PlayEvents.EvUIActivate("skill:1");
            Check(commands == before + 1, "re-enabled UI subscribes once");
            PlayEvents.EvUIStateChanged(new InGameUIState());
            Check(!skill.GetComponent<UnityEngine.UI.Button>().interactable, "map reset disables stale controls");
            Publish();
            PlayEvents.EvUIActivate("inventory");
            Result = "PASS: " + Checks + " interaction/event/pooling checks";
        }
        catch (Exception e) { Result = "FAIL: " + e; Debug.LogError(Result); }
        Finished = true;
    }
}
#endif
