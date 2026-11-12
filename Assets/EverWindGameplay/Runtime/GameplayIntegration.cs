using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace EverWind.Gameplay
{
[DefaultExecutionOrder(-150)]
public sealed class GameplayIntegration : MonoBehaviour
{
    public static GameplayIntegration Active { get; private set; }
    public ServerTransceiverManager Registry;
    public InputManager Input;
    public PopUpUIManager Popups;
    public QuestManager Quests;
    public TutorialGuide Tutorial;
    public DisplayUIManager Display;
    public EnemyHpUIManager EnemyUI;
    public EffectManager Effects;
    public GameplayCatalog Catalog => Registry.Catalog;
    public ServerTransceiver Player => Registry.LocalActor;
    public int TutorialPhase { get; private set; } = -1;
    public int EscapeConsumedFrame { get; private set; } = -1;
    public float ReviveRemaining { get; private set; }
    public string LastMessage { get; private set; } = "";
    public readonly Dictionary<int, int> Equipment = new Dictionary<int, int>();
    readonly Queue<ServerMessage> queued = new Queue<ServerMessage>();
    readonly Dictionary<int, double> cooldowns = new Dictionary<int, double>();
    readonly HashSet<string> hits = new HashSet<string>();
    bool initialized, inventoryDirty;
    float enemiesTimer, cameraTime;
    bool cameraDone;
    void Awake()
    {
        Active = this;
        var session = GameplaySession.Current;
        if (session == null)
        {
            UnityEngine.SceneManagement.SceneManager.LoadSceneAsync("LogIn");
            return;
        }
        Registry.Transport = session.Transport;
        Registry.Catalog = session.Catalog;
        Registry.WorldCatalog = session.WorldCatalog;
        Registry.Transport.Message += Receive;
        GameplayInput.Legacy = Input;
    }
    IEnumerator Start()
    {
        if (Registry.Transport == null)
            yield break;
        yield return null;
        Input.Init();
        Quests.Init();
        Popups.Init();
        initialized = true;
        while (queued.Count > 0)
            Handle(queued.Dequeue());
    }
    void Receive(ServerMessage m)
    {
        if (!initialized)
        {
            queued.Enqueue(m);
            return;
        }
        Handle(m);
    }
    void Handle(ServerMessage m)
    {
        bool own = m.Entity == Registry.Transport.LocalEntityId;
        if (m.Kind == EventKind.Welcome)
        {
            Equipment.Clear();
            cooldowns.Clear();
            hits.Clear();
            inventoryDirty = true;
            EnemyUI.ClearForMapChange();
            var data = SingletonManager.Instance.GetSingleton<DataCenter>();
            if (data != null)
                data.loginData.MapId = (int)m.Value;
        }
        if (m.Kind == EventKind.Inventory && own)
            inventoryDirty = true;
        if (m.Kind == EventKind.Equipment && own)
        {
            if (m.Definition == 0)
                Equipment.Remove(m.Code);
            else
                Equipment[m.Code] = m.Definition;
            inventoryDirty = true;
        }
        if (m.Kind == EventKind.Quest && own)
            Quests.ApplyServerProgress(m.Definition, m.Code, (int)m.Value, (m.Flags & 1) != 0,
                                       (m.Flags & 2) != 0);
        if (m.Kind == EventKind.Tutorial && own)
        {
            TutorialPhase = m.Definition;
            Tutorial.ApplyServerStep(TutorialPhase);
        }
        if (m.Kind == EventKind.Stats && own)
        {
            ReviveRemaining = m.Duration;
            Display.ApplyServerStats(m.Hp, m.MaxHp);
        }
        if (m.Kind == EventKind.Hit && own)
        {
            Display.ApplyServerStats(m.Hp, m.MaxHp);
            UIEvents.EvProfileChangeRequested(DisplayUIManager.ProfileState.Hit, .4f);
        }
        if (own && (m.Kind == EventKind.State || m.Kind == EventKind.Hit))
        {
            if (m.Mode == PlayerMode.Dead && !Popups.DeadUI.gameObject.activeSelf)
                UIEvents.EvDeadUiOpenRequested();
            else if (m.Mode != PlayerMode.Dead && Popups.DeadUI.gameObject.activeSelf)
                UIEvents.EvDeadUiCloseRequested();
        }
        if (m.Kind == EventKind.ActionStarted && own && (ActionKind)m.Code == ActionKind.Attack)
            cooldowns[m.Definition] = m.ServerTime + Catalog.Cooldown(m.Definition);
        if (m.Kind == EventKind.ActionEnded && own && (ActionKind)m.Code == ActionKind.Craft)
            UIEvents.EvProfileChangeRequested(DisplayUIManager.ProfileState.Success, 1);
        if (m.Kind == EventKind.Activity && own)
        {
            if (m.Code == 0)
                PlayEvents.EvCombatEnemyKilled(m.Target);
            if (m.Code == 1)
            {
                PlayEvents.EvGatherCompleted(m.Target);
                PlayEvents.EvInteractionCompleted();
            }
            if (m.Code == 2)
                PlayEvents.EvCraftCompleted(m.Target);
            if (m.Code == 3)
                PlayEvents.EvEquipCompleted();
            if (m.Code == 4)
                PlayEvents.EvMoveCompleted();
        }
        if (m.Kind == EventKind.SnapshotEnd)
        {
            inventoryDirty = true;
            Display.BindServerPlayer(Player);
            enemiesTimer = 1;
        }
    }
    void LateUpdate()
    {
        if (!initialized || Player == null)
            return;
        if (inventoryDirty)
        {
            inventoryDirty = false;
            Popups.Inventory.ApplyServerInventory(Registry.Inventory, Catalog);
            Popups.EquipmentUI.ApplyServerEquipment(Equipment, Catalog);
        }
        foreach (var skill in Catalog.Skills)
        {
            double remain = cooldowns.TryGetValue(skill.Id, out double until)
                                ? Math.Max(0, until - Registry.Transport.EstimatedServerTime)
                                : 0;
            UIEvents.EvSkillCooldownChanged(
                skill.Id - 1, skill.Cooldown > 0 ? Mathf.Clamp01(1 - (float)remain / skill.Cooldown) : 1,
                remain <= 0);
        }
        enemiesTimer += Time.deltaTime;
        if (enemiesTimer > .25f)
        {
            enemiesTimer = 0;
            UIEvents.EvEnemyListUpdated(
                Registry.Entities
                    .Where(a => a.Kind == EntityKind.Monster && a.Hp > 0 &&
                                (a.transform.position - Player.transform.position).sqrMagnitude <= 2500)
                    .Select(a => a.gameObject)
                    .ToList());
        }
        if (ReviveRemaining > 0)
            ReviveRemaining = Mathf.Max(0, ReviveRemaining - Time.deltaTime);
    }
    void Update()
    {
        if (!initialized)
            return;
        if (UnityEngine.Input.GetKeyDown(KeyCode.Escape) && Popups.IsPopUpOn)
            EscapeConsumedFrame = Time.frameCount;
        if (TutorialPhase == 0 && !cameraDone && UnityEngine.Input.GetMouseButton(1))
        {
            cameraTime += Time.deltaTime;
            if (cameraTime >= 5)
            {
                cameraDone = true;
                PlayEvents.EvCameraCompleted();
            }
        }
    }
    public void Send(Request r)
    {
        if (Player == null)
            return;
        Player.Send(r, m =>
                       {
                           if (m.Error != ResultCode.None)
                           {
                               LastMessage = m.Error.ToString();
                               Debug.Log("[Gameplay] " + r.Kind + ": " + m.Error);
                           }
                       });
    }
    public void Use(InventoryItem item)
    {
        int id = Catalog.ItemId(item);
        if (id == 0)
            return;
        Send(new Request { Kind = item is EquipmentItem ? RequestKind.Equip : RequestKind.UseItem,
                           Definition = id, Position = Player.transform.position,
                           Target = Mathf.RoundToInt(Player.transform.eulerAngles.y * 100) });
    }
    public void Unequip(EquipmentItem.EquipmentType slot) => Send(new Request { Kind = RequestKind.Unequip,
                                                                                Definition = (int)slot });
    public void Craft(CraftItemRecipe recipe)
    {
        int id = Catalog.RecipeId(recipe);
        if (id > 0)
            Player?.GetComponent<PlayerActionController>().RequestCraft(id);
    }
    public void Attack(int index)
    {
        if (Catalog.HasSkill(index + 1))
            Player?.GetComponent<PlayerActionController>().RequestAttack(index + 1);
    }
    public void Claim(int quest) => Send(new Request { Kind = RequestKind.ClaimQuest, Definition = quest });
    public void AdvanceTutorial(int phase) => Send(new Request { Kind = RequestKind.Tutorial,
                                                                 Definition = phase });
    public void Revive() => Send(new Request { Kind = RequestKind.Revive });
    public void Preview(bool visible)
    {
        var camera = Camera.main?.GetComponent<GameplayCameraController>();
        if (camera != null && camera.PreviewCamera != null)
            camera.PreviewCamera.SetActive(visible);
    }
    public void ShowDamage(ServerTransceiver actor, ServerMessage m)
    {
        string key = m.Epoch + ":" + m.ActionId + ":" + m.Entity;
        if (!hits.Add(key))
            return;
        if (hits.Count > 2048)
            hits.Clear();
        UIEvents.EvDamageTextRequested(actor.transform.position, Mathf.RoundToInt(m.Value));
        Effects.PlayEffect("Damaged", actor.transform.position);
    }
    public void PlaySwing(int skill, Vector3 p)
    {
        Effects.PlayEffect(skill == 1 ? "Skill_normal" : skill == 2 ? "Skill_smash" : "Skill_windmill", p);
    }
    void OnDestroy()
    {
        if (Registry.Transport != null)
            Registry.Transport.Message -= Receive;
        if (Active == this)
        {
            Active = null;
            GameplayInput.Legacy = null;
        }
    }
}
}
