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
    public ServerTransmitterManager Registry;

    public InputManager Input;

    public QuestManager Quests;

    public Action<int, ushort> ApplyTutorialStep;

    public EffectManager Effects;

    public GameplayCatalog Catalog => Registry.Catalog;

    public PlayerServerTransmitter Player => Registry.LocalActor;

    public int TutorialPhase { get; private set; } = -1;
    public ushort TutorialEvidence { get; private set; }
    public bool CameraTutorialCompleted => cameraDone;

    public int EscapeConsumedFrame { get; private set; } = -1;

    public float ReviveRemaining { get; private set; }
    public string LastMessage { get; private set; } = "";

    public readonly Dictionary<int, int> Equipment = new Dictionary<int, int>();

    readonly Queue<ServerMessage> queued = new Queue<ServerMessage>();

    readonly Dictionary<int, double> cooldowns = new Dictionary<int, double>();

    readonly HashSet<string> hits = new HashSet<string>();

    bool initialized;
    ActionKind profileAction;
    ulong profileActionId;

    float cameraTime;

    bool cameraDone;

    public HealthChanged CurrentHealth => new HealthChanged { Entity = Player != null ? Player.EntityId : 0, Hp = Player != null ? Player.Hp : 0, MaxHp = Player != null ? Player.MaxHp : 0 };

    public SkillCooldownChanged GetSkillCooldown(int skill)
    {
        if (Registry.Progression.Skills.TryGetValue(skill, out PlayerProgression.SkillProgress progress))
        {
            double endsAt = cooldowns.TryGetValue(skill, out double localEnd) ? Math.Max(localEnd, progress.CooldownEndsAt) : progress.CooldownEndsAt;
            return new SkillCooldownChanged { Skill = skill, EndsAt = endsAt, Duration = progress.Cooldown };
        }

        return new SkillCooldownChanged { Skill = skill, EndsAt = cooldowns.TryGetValue(skill, out double at) ? at : 0, Duration = Catalog != null ? Catalog.Cooldown(skill) : 0 };
    }

    void Awake()
    {
        Active = this;

        var session = ContainerInMap.Current;

        if (session == null)
        {
            UnityEngine.SceneManagement.SceneManager.LoadSceneAsync("LogIn");

            return;
        }

        Registry.Transport = session.Transport;

    }

    IEnumerator Start()
    {
        if (Registry.Transport == null)
        {
            yield break;
        }

        yield return null;

        Input.Init();
        Quests.Init();

        initialized = true;

        while (queued.Count > 0)
            Handle(queued.Dequeue());
    }

    public void Receive(ServerMessage m)
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
        if (own && m.Kind == EventKind.Hit) PlayEvents.EvUIProfile(UIProfileState.Hit, .4f);
        if (own && m.Kind == EventKind.ActionStarted) { profileAction = (ActionKind)m.Code; profileActionId = m.ActionId; }
        if (own && (m.Kind == EventKind.ActionEnded || m.Kind == EventKind.ActionCancelled) && m.ActionId == profileActionId)
        {
            if (m.Kind == EventKind.ActionEnded && (profileAction == ActionKind.Gather || profileAction == ActionKind.Craft))
                PlayEvents.EvUIProfile(UIProfileState.Success, 1f);
            profileAction = ActionKind.None; profileActionId = 0;
        }

        if (m.Kind == EventKind.Welcome)
        {
            profileAction = ActionKind.None; profileActionId = 0;
            Equipment.Clear();
            cooldowns.Clear();
            hits.Clear();
        }

        if (m.Kind == EventKind.Equipment && own)
        {
            if (m.Definition == 0)
            {
                Equipment.Remove(m.Code);
            }
            else
                Equipment[m.Code] = m.Definition;
        }

        if (m.Kind == EventKind.Quest && own)
            Quests.ApplyServerProgress(m.Definition, m.Code, (int)m.Value, (m.Flags & 1) != 0, (m.Flags & 2) != 0);
        if (m.Kind == EventKind.Tutorial && own)
        {
            TutorialPhase = m.Definition;
            TutorialEvidence = m.Flags;

            ApplyTutorialStep?.Invoke(TutorialPhase, m.Flags);
        }

        if (m.Kind == EventKind.Stats && own)
        {
            ReviveRemaining = m.Duration;
        }

        if (m.Kind == EventKind.ActionStarted && own && (ActionKind)m.Code == ActionKind.Attack)
        {
            float cooldown = Registry.Progression.Skills.TryGetValue(m.Definition, out PlayerProgression.SkillProgress progress) ? progress.Cooldown : Catalog.Cooldown(m.Definition);
            cooldowns[m.Definition] = m.ServerTime + cooldown;

        }

    }

    void LateUpdate()
    {
        if (!initialized || Player == null)
        {
            return;
        }

        if (ReviveRemaining > 0)
        {
            ReviveRemaining = Mathf.Max(0, ReviveRemaining - Time.deltaTime);
        }
    }

    void Update()
    {
        if (!initialized)
        {
            return;
        }

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
        {
            return;
        }

        Player.Send(r, m =>
        {
            if (m.Error != ResultCode.None)
            {
                LastMessage = m.Error.ToString();
                PlayEvents.EvUINotice(LastMessage);

                Debug.Log("[Gameplay] " + r.Kind + ": " + m.Error);

            }
        });
    }

    public void Use(InventoryItem item)
    {
        int id = Catalog.ItemId(item);

        if (id == 0)
        {
            return;
        }

        Send(new Request { Kind = item is EquipmentItem ? RequestKind.Equip : RequestKind.UseItem, Definition = id, Position = Player.transform.position, Target = Mathf.RoundToInt(Player.transform.eulerAngles.y * 100) });
    }

    public void Unequip(EquipmentItem.EquipmentType slot) => Send(new Request { Kind = RequestKind.Unequip, Definition = (int)slot });

    public void Craft(CraftItemRecipe recipe)
    {
        int id = Catalog.RecipeId(recipe);

        if (id > 0)
        {
            Player?.GetComponent<PlayerActionController>().RequestCraft(id);
        }
    }

    public void Attack(int index)
    {
        if (Catalog.HasSkill(index + 1))
        {
            Player?.GetComponent<PlayerActionController>().RequestAttack(index + 1);
        }
    }

    public void Claim(int quest) => Send(new Request { Kind = RequestKind.ClaimQuest, Definition = quest });
    public void AdvanceTutorial(int phase) => Send(new Request { Kind = RequestKind.Tutorial, Definition = phase });
    public void Revive() => Send(new Request { Kind = RequestKind.Revive });

    public void Preview(bool visible)
    {
        var camera = Camera.main?.GetComponent<GameplayCameraController>();

        if (camera != null && camera.PreviewCamera != null)
        {
            camera.PreviewCamera.SetActive(visible);
        }
    }

    public void ShowDamage(Vector3 position, ServerMessage m)
    {
        string key = m.Epoch + ":" + m.ActionId + ":" + m.Entity;

        if (!hits.Add(key))
        {
            return;
        }

        if (hits.Count > 2048)
        {
            hits.Clear();
        }

        Effects.PlayEffect("Damaged", position);
        if (Camera.main != null) PlayEvents.EvUIDamage(new UIDamageData { ScreenPosition = Camera.main.WorldToScreenPoint(position + Vector3.up * 1.5f), Amount = Mathf.RoundToInt(m.Value) });
    }

    public void PlaySwing(int skill, Vector3 p)
    {
        var definition = Catalog.Skill(skill);
        if (definition == null || definition.SwingEffect == null) return;
        var effect = Instantiate(definition.SwingEffect, p, Quaternion.identity);
        Destroy(effect, 3);
    }

    void OnDestroy()
    {
        ApplyTutorialStep = null;
        if (Active == this)
        {
            Active = null;
        }
    }
}
}
