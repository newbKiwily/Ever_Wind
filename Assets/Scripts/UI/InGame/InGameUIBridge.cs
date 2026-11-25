using System;
using System.Collections.Generic;
using UnityEngine;
using EverWind.Gameplay;

// 게임 상태와 UI 이벤트의 경계. UI View는 이 컴포넌트나 게임 개체를 참조하지 않는다.
[DefaultExecutionOrder(-140)]
public sealed class InGameUIBridge : MonoBehaviour
{
    GameplayIntegration gameplay;
    ServerTransmitterManager registry;
    float nextRefresh;
    void Awake() { gameplay = GetComponent<GameplayIntegration>(); registry = GetComponent<ServerTransmitterManager>(); }
    void OnEnable() { PlayEvents.UIStateRequested += Publish; PlayEvents.UICommandRequested += Execute; }
    void OnDisable()
    {
        PlayEvents.UIStateRequested -= Publish; PlayEvents.UICommandRequested -= Execute;
        PlayEvents.EvUIStateChanged(new InGameUIState());
    }
    void Update()
    {
        if (!InputManager.IsTyping && registry != null && registry.Transport != null && registry.Transport.Ready)
        {
            var input = InputManager.Current;
            if (input.GetInventoryKeyDown()) PlayEvents.EvUIActivate("inventory");
            if (input.GetCraftUIKeyDown()) PlayEvents.EvUIActivate("craft");
            if (input.GetQuestUIKeyDown()) PlayEvents.EvUIActivate("quest");
            if (input.GetUICloseKeyDown()) PlayEvents.EvUIPanelRequested(InGamePanel.None);
            int skill = input.GetAttackKeyDown();
            if (skill > 0) PlayEvents.EvUIActivate("skill:" + skill);
        }
        if (Time.unscaledTime >= nextRefresh) { nextRefresh = Time.unscaledTime + .1f; Publish(); }
    }
    void Execute(UICommand command)
    {
        if (gameplay == null || registry == null || registry.Transport == null || !registry.Transport.Ready || gameplay.Player == null) return;
        var catalog = registry.Catalog;
        switch (command.Kind)
        {
        case UICommandKind.Skill: gameplay.Attack(command.Id - 1); break;
        case UICommandKind.UseItem:
            var item = catalog.Item(command.Id);
            if (item != null && registry.Inventory.TryGetValue(command.Id, out int count) && count > 0) gameplay.Use(item);
            break;
        case UICommandKind.Unequip: gameplay.Unequip((EquipmentItem.EquipmentType)command.Id); break;
        case UICommandKind.Craft:
            foreach (var recipe in catalog.Recipes) if (recipe.Id == command.Id) { gameplay.Craft(recipe.Definition); break; }
            break;
        case UICommandKind.ClaimQuest: gameplay.Quests.ClaimReward(command.Id); break;
        case UICommandKind.Revive: gameplay.Revive(); break;
        case UICommandKind.Logout: registry.Transport.Logout(); break;
        }
    }
    UIItemData Item(int id, int count, int slot = 0)
    {
        var definition = registry.Catalog.Item(id);
        return new UIItemData { Id = id, Count = count, Slot = slot, Name = definition != null ? definition.ItemName : id.ToString(), Description = definition != null ? definition.ItemInstruction : "", Icon = definition != null ? definition.ItemImage : null, Usable = definition is IUsableItem };
    }
    public void Publish()
    {
        var state = new InGameUIState();
        if (registry == null || registry.Transport == null || gameplay == null || registry.Catalog == null) { PlayEvents.EvUIStateChanged(state); return; }
        var player = registry.LocalActor;
        state.Ready = registry.Transport.Ready && player != null;
        state.Hp = player != null ? player.Hp : 0; state.MaxHp = player != null ? player.MaxHp : 0;
        state.Dead = player != null && player.Mode == PlayerMode.Dead;
        state.ReviveRemaining = gameplay.ReviveRemaining;
        state.Level = registry.Progression.Level; state.Experience = registry.Progression.Experience; state.RequiredExperience = registry.Progression.RequiredExperience;
        state.MapCenter = registry.MapCenter; state.MapRadius = registry.MapRadius;
        state.MapId = registry.CurrentMap;
        state.TutorialPhase = gameplay.TutorialPhase;
        state.TutorialCleared = state.TutorialPhase == 0 ? gameplay.CameraTutorialCompleted : state.TutorialPhase > 0 && state.TutorialPhase < 6 && (gameplay.TutorialEvidence & (1 << state.TutorialPhase)) != 0;
        state.PlayerPosition = player != null ? player.transform.position : Vector3.zero;
        state.SelectedTarget = player != null && player.Controller != null && player.Controller.SelectedTarget != null ? player.Controller.SelectedTarget.EntityId : 0;
        var items = new List<UIItemData>();
        foreach (var entry in registry.Inventory) if (entry.Value > 0) items.Add(Item(entry.Key, entry.Value));
        items.Sort((a, b) => a.Id.CompareTo(b.Id)); state.Items = items.ToArray();
        var equipment = new List<UIItemData>();
        foreach (var entry in gameplay.Equipment) equipment.Add(Item(entry.Value, 1, entry.Key));
        state.Equipment = equipment.ToArray();
        var skills = new List<UISkillData>();
        foreach (var entry in registry.Catalog.Skills)
        {
            var cooldown = gameplay.GetSkillCooldown(entry.Id);
            registry.Progression.Skills.TryGetValue(entry.Id, out var growth);
            skills.Add(new UISkillData { Id = entry.Id, Name = entry.Definition != null ? entry.Definition.name : entry.Id.ToString(), Rank = growth != null ? growth.Rank : 0, Available = state.Ready && !state.Dead && entry.Definition != null && entry.Definition.CanExecute && registry.Progression.CanUseSkill(entry.Id), Remaining = (float)Math.Max(0, cooldown.EndsAt - registry.Transport.EstimatedServerTime), Duration = cooldown.Duration });
        }
        state.Skills = skills.ToArray();
        var recipes = new List<UIRecipeData>();
        foreach (var entry in registry.Catalog.Recipes)
        {
            var definition = entry.Definition; if (definition == null) continue;
            var ingredients = new List<UIItemData>(); bool available = state.Ready && !state.Dead;
            foreach (var ingredient in definition.Ingredients)
            {
                int id = registry.Catalog.ItemId(ingredient.IngredientItem);
                registry.Inventory.TryGetValue(id, out int owned);
                var data = Item(id, ingredient.Amount); data.Description = "보유 " + owned + " / 필요 " + ingredient.Amount;
                ingredients.Add(data); available &= id > 0 && owned >= ingredient.Amount;
            }
            Sprite icon = null;
            foreach (var item in registry.Catalog.Items) if (item.Definition != null && (item.Definition.ItemName == definition.ResultItemName || item.Definition.name == definition.ResultItemName)) { icon = item.Definition.ItemImage; break; }
            recipes.Add(new UIRecipeData { Id = entry.Id, Name = definition.ResultItemName, Icon = icon, Available = available, Ingredients = ingredients.ToArray() });
        }
        state.Recipes = recipes.ToArray();
        var quests = new List<UIQuestData>();
        if (gameplay.Quests != null) foreach (var progress in gameplay.Quests.ActiveQuests)
        {
            if (progress.Quest == null) continue;
            quests.Add(new UIQuestData { Id = progress.Quest.QuestId, Name = progress.Quest.QuestName, Description = progress.Quest.QuestDescription, Complete = progress.IsCompleted, Claimed = progress.RewardClaimed });
        }
        state.Quests = quests.ToArray();
        var enemies = new List<UIEnemyData>(); var camera = Camera.main;
        foreach (var entity in registry.Entities) if (entity is MonsterServerTransmitter monster && monster.Hp > 0 && monster.gameObject.activeInHierarchy)
            enemies.Add(new UIEnemyData { Id = monster.EntityId, Name = monster.gameObject.name, Hp = monster.Hp, MaxHp = monster.MaxHp, Position = monster.transform.position, ScreenPosition = camera != null ? camera.WorldToScreenPoint(monster.transform.position + Vector3.up * 2.3f) : new Vector3(0, 0, -1) });
        state.Enemies = enemies.ToArray();
        PlayEvents.EvUIStateChanged(state);
    }
}
