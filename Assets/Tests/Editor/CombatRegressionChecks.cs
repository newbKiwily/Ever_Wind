#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using EverWind.Gameplay;
public static class CombatRegressionChecks
{
 static int checks;
 static void Check(bool value,string name){if(!value)throw new Exception(name);checks++;}
 static void Set(object target,string property,object value)=>target.GetType().GetProperty(property).SetValue(target,value);
 static object Field(object target,string name)=>target.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(target);
 static void Panel(InputManager input,InGamePanel panel)=>typeof(InputManager).GetMethod("ApplyUIPanel",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(input,new object[]{panel});
 [MenuItem("Tools/EverWind/Verify combat regressions")]
 public static string Run()
 {
  if(Application.isPlaying)throw new Exception("Edit Mode only");
  checks=0;var scene=EditorSceneManager.NewPreviewScene();var previous=ContainerInMap.Current;var definitions=new List<ScriptableObject>();
  GameObject Make(string name){var go=new GameObject(name);SceneManager.MoveGameObjectToScene(go,scene);return go;}
  try
  {
   var session=Make("Session");var network=session.AddComponent<NetworkClient>();var container=session.AddComponent<ContainerInMap>();typeof(ContainerInMap).GetProperty("Current").SetValue(null,container);
   Set(network,"LocalEntityId",9);Set(network,"Ready",true);
   var catalog=ScriptableObject.CreateInstance<GameplayCatalog>();definitions.Add(catalog);Set(container,"Catalog",catalog);
   var registry=Make("Registry").AddComponent<ServerTransmitterManager>();registry.Transport=network;
   var input=Make("Input").AddComponent<InputManager>();Panel(input,InGamePanel.Quest);Check(!(bool)Field(input,"_uiRestricted"),"quest movement allowed");Panel(input,InGamePanel.Inventory);Check((bool)Field(input,"_uiRestricted"),"inventory remains modal");Panel(input,InGamePanel.Craft);Check((bool)Field(input,"_uiRestricted"),"craft remains modal");Panel(input,InGamePanel.None);Check(!(bool)Field(input,"_uiRestricted"),"closing restores input");
   var playerGo=Make("Player");var player=playerGo.AddComponent<PlayerServerTransmitter>();player.Bind(registry,new ServerMessage{Kind=EventKind.Spawn,Entity=9,Code=(ushort)EntityKind.Player,Hp=100,MaxHp=100,Radius=.4f});
   var motor=playerGo.AddComponent<MovementMotor>();motor.Actor=player;motor.Radius=.4f;motor.MoveSpeed=5;
   var control=playerGo.AddComponent<PlayerActionController>();control.Actor=player;control.Motor=motor;player.Controller=control;
   var monster=Make("Target").AddComponent<MonsterServerTransmitter>();monster.Bind(registry,new ServerMessage{Kind=EventKind.Spawn,Entity=10,Code=(ushort)EntityKind.Monster,Hp=100,MaxHp=100,Radius=.5f});
   var detector=Make("Detector").AddComponent<SkillRangeDetector>();
   var caster=ScriptableObject.CreateInstance<RangedSkill>();caster.SpawnPoint=ROSpawnPoint.Caster;
   var target=ScriptableObject.CreateInstance<RangedSkill>();target.SpawnPoint=ROSpawnPoint.Target;
   var single=ScriptableObject.CreateInstance<SingleTargetSkill>();
   var area=ScriptableObject.CreateInstance<SkillDefinition>();area.Kind=SkillKind.Area;area.RequiresTarget=false;
   SkillDefinition[] skills={caster,target,single,area};var entries=new List<GameplayCatalog.SkillEntry>();
   for(int i=0;i<skills.Length;i++){var s=skills[i];definitions.Add(s);s.Id=i+1;s.Reach=1.5f;s.RangeDetector=detector;entries.Add(new GameplayCatalog.SkillEntry{Id=s.Id,Definition=s,Reach=1.5f});}
   catalog.Skills=entries.ToArray();registry.Receive(new ServerMessage{Kind=EventKind.Growth,Entity=9,Code=1});
   for(int i=1;i<=4;i++)registry.Receive(new ServerMessage{Kind=EventKind.SkillRank,Entity=9,Definition=i,Code=1});
   player.Receive(new ServerMessage{Kind=EventKind.State,Mode=PlayerMode.Combat,Hp=100,MaxHp=100});
   foreach(var s in skills)
   {
    monster.transform.position=new Vector3(10,0,0);var action=new AttackAction(player,monster,s.Id,1.5f);
    Check(s.CheckRange(action,1.5f)==ActionResult.OutOfRange,s.name+" rejects far target");Check(action.TryRequest(null)==ActionResult.OutOfRange&&action.RequestId==0,"no out of range packet");
    control.SelectTarget(monster);control.AutoApproach=true;control.RequestAttack(s.Id);Check(Field(control,"current") is ApproachTargetAction,"all attacks approach");control.Interrupt();
    control.AutoApproach=false;control.RequestAttack(s.Id);Check(control.LastResult=="OutOfRange","manual range rejection");
    monster.transform.position=new Vector3(2.4f,0,0);Check(s.CheckRange(action,1.5f)==ActionResult.Running,"range boundary includes actor radii");monster.transform.position=new Vector3(2.41f,0,0);Check(s.CheckRange(action,1.5f)==ActionResult.OutOfRange,"outside boundary");Check(s.CheckRange(new AttackAction(player,null,s.Id,1.5f),1.5f)==ActionResult.TargetLost,"missing target rejected");
   }
   monster.transform.position=new Vector3(8,0,0);Check(caster.Center(player.transform,monster.transform)==player.transform.position,"caster RO center preserved");Check(target.Center(player.transform,monster.transform)==monster.transform.position,"target RO center preserved");
   var profile=AssetDatabase.LoadAssetAtPath<ActionVisualProfile>("Assets/Scripts/GamePlayEdit/Characters/Player/PlayerProfile.asset");
   var visual=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Scripts/GamePlayEdit/Characters/Player/PlayerVisual.prefab"));visual.transform.SetParent(playerGo.transform,false);var animator=visual.GetComponentInChildren<Animator>();animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
   caster.Animation=profile.ResolveClip("Idle"); // Item ID 1 intentionally overlaps skill ID 1.
   player.Controller=null;player.ConfigureVisual(profile,animator);animator.Rebind();animator.Update(0);
   double now=network.EstimatedServerTime;
   player.Receive(new ServerMessage{Kind=EventKind.ActionStarted,ActionId=100,Code=(ushort)ActionKind.Gather,Definition=1,Mode=PlayerMode.Looting,Value=(float)now,Duration=3});
   player.Presentation.Tick(.1f);animator.Update(.1f);animator.Update(.1f);
   Check(animator.runtimeAnimatorController==profile.Controller,"gather never overrides clip with matching skill ID");Check(animator.GetCurrentAnimatorStateInfo(0).IsName("Gather"),"Gather Animator state");
   player.Receive(new ServerMessage{Kind=EventKind.ActionEnded,ActionId=99,Mode=PlayerMode.Combat});Check(player.Mode==PlayerMode.Looting,"stale end cannot change mode");
   player.Receive(new ServerMessage{Kind=EventKind.ActionEnded,ActionId=100,Mode=PlayerMode.NonCombat});player.Presentation.Tick(.1f);animator.Update(.1f);animator.Update(.1f);
   Check(player.State.Current.Mode==PlayerMode.NonCombat,"end immediately leaves Looting");Check(animator.GetBool("Success")&&animator.GetCurrentAnimatorStateInfo(0).IsName("Success"),"successful gather plays Success");
   player.Receive(new ServerMessage{Kind=EventKind.ActionStarted,ActionId=101,Code=(ushort)ActionKind.Gather,Definition=1,Mode=PlayerMode.Looting,Value=(float)now,Duration=3});
   player.Receive(new ServerMessage{Kind=EventKind.ActionCancelled,ActionId=101,Mode=PlayerMode.NonCombat});player.Presentation.Tick(.1f);Check(!animator.GetBool("Success")&&player.Mode==PlayerMode.NonCombat,"cancel leaves Looting without Success");
   return "PASS: "+checks+" input, all-skill range/approach, RO center, real Animator Gather/Success and stale/cancel checks";
  }
  finally{typeof(ContainerInMap).GetProperty("Current").SetValue(null,previous);EditorSceneManager.ClosePreviewScene(scene);foreach(var d in definitions)if(d!=null)UnityEngine.Object.DestroyImmediate(d);}
 }
}
#endif
